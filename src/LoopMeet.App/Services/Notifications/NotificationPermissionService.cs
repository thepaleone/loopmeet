using OneSignalSDK.DotNet;

namespace LoopMeet.App.Services.Notifications;

public enum NotificationPermissionState
{
    Unknown,
    Granted,
    Denied,
}

public sealed class NotificationPermissionService
{
    private const string PermissionStateKey = "notification_permission_state";

    public NotificationPermissionState CachedState
    {
        get
        {
            var raw = Preferences.Default.Get(PermissionStateKey, NotificationPermissionState.Unknown.ToString());
            return Enum.TryParse<NotificationPermissionState>(raw, true, out var parsed)
                ? parsed
                : NotificationPermissionState.Unknown;
        }
    }

    public NotificationPermissionState CurrentState =>
        TryReadOsPermission(out var granted) && granted
            ? NotificationPermissionState.Granted
            : CachedState == NotificationPermissionState.Granted
                ? NotificationPermissionState.Unknown
                : CachedState;

    public Task SetStateAsync(NotificationPermissionState state)
    {
        Preferences.Default.Set(PermissionStateKey, state.ToString());
        return Task.CompletedTask;
    }

    public bool ShouldPromptAfterSignIn()
    {
        if (TryReadOsPermission(out var osGranted) && osGranted)
        {
            return false;
        }

        // OS does not currently consider us granted. Re-engage in two cases:
        //  - Cached state is Unknown (fresh install or never asked).
        //  - Cached state is Granted (user revoked at the OS level since our
        //    last sign-in). OneSignal's fallbackToSettings routes them to the
        //    system Settings page if the OS dialog cannot show again.
        // Cached Denied is left alone here to avoid prompting on every sign-in;
        // the in-app Settings CTA covers FR-015 in that case.
        return CachedState != NotificationPermissionState.Denied;
    }

    private static bool TryReadOsPermission(out bool granted)
    {
        try
        {
            granted = OneSignal.Notifications.Permission;
            return true;
        }
        catch
        {
            granted = false;
            return false;
        }
    }
}
