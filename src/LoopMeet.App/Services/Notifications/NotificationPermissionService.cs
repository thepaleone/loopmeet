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

    public NotificationPermissionState CurrentState
    {
        get
        {
            var raw = Preferences.Default.Get(PermissionStateKey, NotificationPermissionState.Unknown.ToString());
            return Enum.TryParse<NotificationPermissionState>(raw, true, out var parsed)
                ? parsed
                : NotificationPermissionState.Unknown;
        }
    }

    public Task SetStateAsync(NotificationPermissionState state)
    {
        Preferences.Default.Set(PermissionStateKey, state.ToString());
        return Task.CompletedTask;
    }

    public bool ShouldPromptAfterSignIn() => CurrentState == NotificationPermissionState.Unknown;
}
