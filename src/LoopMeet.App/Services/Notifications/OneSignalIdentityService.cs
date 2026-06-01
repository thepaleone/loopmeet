using Microsoft.Extensions.Logging;
using OneSignalSDK.DotNet;

namespace LoopMeet.App.Services.Notifications;

public sealed class OneSignalIdentityService
{
    private readonly ILogger<OneSignalIdentityService> _logger;
    private bool _diagnosticsAttached;

    public OneSignalIdentityService(ILogger<OneSignalIdentityService> logger)
    {
        _logger = logger;
    }

    public Task LoginAsync(Guid userId)
    {
        OneSignal.Login(userId.ToString());
        _logger.LogInformation("OneSignal login completed for user {UserId}", userId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Ensures the device is opted in to push delivery. In OneSignalSDK.DotNet v5+
    /// the OS permission grant is tracked separately from the push subscription
    /// opt-in state, so a freshly-granted permission still leaves the device as
    /// "Never Subscribed" on the dashboard unless we explicitly opt in.
    /// </summary>
    public Task EnsureOptedInAsync()
    {
        try
        {
            AttachDiagnosticsOnce();

            if (!OneSignal.User.PushSubscription.OptedIn)
            {
                OneSignal.User.PushSubscription.OptIn();
                _logger.LogInformation("OneSignal push subscription OptIn invoked.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OneSignal push subscription OptIn threw.");
        }

        return Task.CompletedTask;
    }

    private void AttachDiagnosticsOnce()
    {
        if (_diagnosticsAttached) return;
        _diagnosticsAttached = true;

        try
        {
            OneSignal.User.PushSubscription.Changed += (_, args) =>
            {
                var state = args.State;
                _logger.LogInformation(
                    "OneSignal push subscription changed. OptedIn={OptedIn} Id={Id} HasToken={HasToken}",
                    state.Current.OptedIn,
                    state.Current.Id,
                    !string.IsNullOrWhiteSpace(state.Current.Token));
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to attach OneSignal push subscription diagnostics.");
        }
    }
}
