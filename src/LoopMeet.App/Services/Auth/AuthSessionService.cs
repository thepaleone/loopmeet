using LoopMeet.App.Services.Notifications;
using LoopMeet.App.Features.Auth;
using OneSignalSDK.DotNet;

namespace LoopMeet.App.Services.Auth;

public sealed class AuthSessionService
{
    private readonly AuthService _authService;
    private readonly NotificationPermissionService _permissionService;
    private readonly DeviceRegistrationService _deviceRegistrationService;
    private readonly OneSignalIdentityService _oneSignalIdentityService;

    public AuthSessionService(
        AuthService authService,
        NotificationPermissionService permissionService,
        DeviceRegistrationService deviceRegistrationService,
        OneSignalIdentityService oneSignalIdentityService)
    {
        _authService = authService;
        _permissionService = permissionService;
        _deviceRegistrationService = deviceRegistrationService;
        _oneSignalIdentityService = oneSignalIdentityService;
    }

    public async Task HandleSuccessfulSignInAsync()
    {
        var permissionState = _permissionService.CurrentState;

        if (_permissionService.ShouldPromptAfterSignIn())
        {
            var granted = await RequestNotificationPermissionAsync();
            permissionState = granted
                ? NotificationPermissionState.Granted
                : NotificationPermissionState.Denied;
        }

        await _permissionService.SetStateAsync(permissionState);

        var userId = _authService.GetCurrentUserId();
        if (userId.HasValue)
        {
            await _oneSignalIdentityService.LoginAsync(userId.Value);
            try
            {
                await _deviceRegistrationService.SyncPermissionStateAsync(userId.Value, permissionState);
            }
            catch
            {
                // Intentionally non-fatal to avoid blocking sign-in flow.
            }

            try
            {
                await _deviceRegistrationService.SyncUserProfileTimezoneAsync(userId.Value);
            }
            catch
            {
                // Non-fatal; reminders fall back to America/Los_Angeles.
            }
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
