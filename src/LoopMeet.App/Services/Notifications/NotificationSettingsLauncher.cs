namespace LoopMeet.App.Services.Notifications;

public sealed class NotificationSettingsLauncher
{
    public Task OpenAppNotificationSettingsAsync()
    {
        return MainThread.InvokeOnMainThreadAsync(() => AppInfo.Current.ShowSettingsUI());
    }
}
