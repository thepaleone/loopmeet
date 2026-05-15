using LoopMeet.App.Services.Notifications;
using LoopMeet.App.Features.Auth;
using Microsoft.Extensions.Logging;
using OneSignalSDK.DotNet;

namespace LoopMeet.App.Services.Auth;

public sealed class AuthSessionService
{
    private readonly AuthService _authService;
    private readonly NotificationPermissionService _permissionService;
    private readonly DeviceRegistrationService _deviceRegistrationService;
    private readonly OneSignalIdentityService _oneSignalIdentityService;
    private readonly OneSignalBootstrapService _oneSignalBootstrap;
    private readonly ILogger<AuthSessionService> _logger;

    public AuthSessionService(
        AuthService authService,
        NotificationPermissionService permissionService,
        DeviceRegistrationService deviceRegistrationService,
        OneSignalIdentityService oneSignalIdentityService,
        OneSignalBootstrapService oneSignalBootstrap,
        ILogger<AuthSessionService> logger)
    {
        _authService = authService;
        _permissionService = permissionService;
        _deviceRegistrationService = deviceRegistrationService;
        _oneSignalIdentityService = oneSignalIdentityService;
        _oneSignalBootstrap = oneSignalBootstrap;
        _logger = logger;
    }

    public async Task HandleSuccessfulSignInAsync()
    {
        // Make sure OneSignal is initialized before we touch its APIs.
        // App.xaml.cs fires this on startup but we re-await here defensively
        // so a cold sign-in race doesn't leave us querying an uninitialized SDK.
        await _oneSignalBootstrap.InitializeAsync();

        var permissionState = await ResolvePermissionStateAsync();
        await PersistPermissionStateAsync(permissionState);

        var userId = _authService.GetCurrentUserId();
        if (!userId.HasValue)
        {
            _logger.LogWarning("Post-sign-in setup skipped: no current user id available.");
            return;
        }

        await TryLinkOneSignalIdentityAsync(userId.Value);
        await TrySyncDevicePermissionAsync(userId.Value, permissionState);
        await TrySyncUserProfileTimezoneAsync(userId.Value);
    }

    private async Task<NotificationPermissionState> ResolvePermissionStateAsync()
    {
        var state = _permissionService.CurrentState;

        if (!_oneSignalBootstrap.IsInitialized)
        {
            _logger.LogWarning(
                "Notification permission flow skipped: OneSignal is not initialized. Cached state retained as {State}.",
                state);
            return state;
        }

        if (!_permissionService.ShouldPromptAfterSignIn())
        {
            return state;
        }

        try
        {
            var granted = await RequestNotificationPermissionAsync();
            return granted ? NotificationPermissionState.Granted : NotificationPermissionState.Denied;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OneSignal.RequestPermissionAsync threw; treating as Denied for this session.");
            return NotificationPermissionState.Denied;
        }
    }

    private async Task PersistPermissionStateAsync(NotificationPermissionState state)
    {
        try
        {
            await _permissionService.SetStateAsync(state);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to persist notification permission state {State} to local preferences.", state);
        }
    }

    private async Task TryLinkOneSignalIdentityAsync(Guid userId)
    {
        if (!_oneSignalBootstrap.IsInitialized)
        {
            return;
        }

        try
        {
            await _oneSignalIdentityService.LoginAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OneSignal identity login failed for {UserId}.", userId);
        }
    }

    private async Task TrySyncDevicePermissionAsync(Guid userId, NotificationPermissionState state)
    {
        try
        {
            await _deviceRegistrationService.SyncPermissionStateAsync(userId, state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Device registration sync failed for {UserId} with state {State}; user_devices row was not written.",
                userId, state);
        }
    }

    private async Task TrySyncUserProfileTimezoneAsync(Guid userId)
    {
        try
        {
            await _deviceRegistrationService.SyncUserProfileTimezoneAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "User profile timezone sync failed for {UserId}; reminders will fall back to America/Los_Angeles.",
                userId);
        }
    }

    private static async Task<bool> RequestNotificationPermissionAsync()
    {
#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            return await OneSignal.Notifications.RequestPermissionAsync(fallbackToSettings: true);
        }

        return true;
#else
        return await OneSignal.Notifications.RequestPermissionAsync(fallbackToSettings: true);
#endif
    }
}
