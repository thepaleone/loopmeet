namespace LoopMeet.App.Services.Notifications;

public sealed class NotificationService
{
    private readonly PendingNotificationIntentStore _intentStore;
    private readonly NotificationNavigator _navigator;

    public NotificationService(PendingNotificationIntentStore intentStore, NotificationNavigator navigator)
    {
        _intentStore = intentStore;
        _navigator = navigator;
    }

    public async Task HandleNotificationOpenedAsync(IDictionary<string, object?> additionalData, bool isSignedIn)
    {
        var intent = NotificationIntentFrom(additionalData);
        if (intent is null)
        {
            await MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync("//Home"));
            return;
        }

        if (!isSignedIn)
        {
            await _intentStore.SaveAsync(intent);
            await MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync("//Login"));
            return;
        }

        await _navigator.NavigateAsync(intent);
    }

    private static NotificationIntent? NotificationIntentFrom(IDictionary<string, object?> additionalData)
    {
        string? Read(string key) => additionalData.TryGetValue(key, out var value) ? value?.ToString() : null;

        var type = Read("notification_type");
        var targetKind = Read("target_kind");
        var fallbackRoute = Read("fallback_route");
        var eventId = Read("event_id");
        var sentAt = Read("sent_at");

        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(targetKind) ||
            string.IsNullOrWhiteSpace(fallbackRoute) || string.IsNullOrWhiteSpace(eventId) || string.IsNullOrWhiteSpace(sentAt))
        {
            return null;
        }

        return new NotificationIntent(type, targetKind, Read("target_id"), fallbackRoute, eventId, sentAt);
    }
}
