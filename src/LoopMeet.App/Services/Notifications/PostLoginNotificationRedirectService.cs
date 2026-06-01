namespace LoopMeet.App.Services.Notifications;

public sealed class PostLoginNotificationRedirectService
{
    private readonly PendingNotificationIntentStore _intentStore;
    private readonly NotificationNavigator _navigator;

    public PostLoginNotificationRedirectService(
        PendingNotificationIntentStore intentStore,
        NotificationNavigator navigator)
    {
        _intentStore = intentStore;
        _navigator = navigator;
    }

    public async Task ResumeAsync()
    {
        var intent = await _intentStore.ConsumeAsync();
        if (intent is null)
        {
            return;
        }

        await _navigator.NavigateAsync(intent);
    }
}
