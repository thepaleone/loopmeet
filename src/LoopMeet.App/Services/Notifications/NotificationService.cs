using Microsoft.Extensions.Logging;

namespace LoopMeet.App.Services.Notifications;

public sealed class NotificationService
{
    private readonly PendingNotificationIntentStore _intentStore;
    private readonly NotificationNavigator _navigator;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        PendingNotificationIntentStore intentStore,
        NotificationNavigator navigator,
        ILogger<NotificationService> logger)
    {
        _intentStore = intentStore;
        _navigator = navigator;
        _logger = logger;
    }

    public async Task HandleNotificationOpenedAsync(IDictionary<string, object?> additionalData, bool isSignedIn)
    {
        var intent = NotificationIntentFrom(additionalData);
        if (intent is null)
        {
            _logger.LogWarning("Notification open did not include required routing payload. Keys={Keys}", string.Join(",", additionalData.Keys));
            await MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync("//home"));
            return;
        }

        if (!isSignedIn)
        {
            _logger.LogInformation("Notification tap queued until sign-in. Type={Type} EventId={EventId}", intent.NotificationType, intent.EventId);
            await _intentStore.SaveAsync(intent);
            await MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync("//login"));
            return;
        }

        try
        {
            await _navigator.NavigateAsync(intent);
            _logger.LogInformation("Notification tap navigation completed. Type={Type} EventId={EventId}", intent.NotificationType, intent.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Notification tap navigation failed; queuing for retry. Type={Type} EventId={EventId}", intent.NotificationType, intent.EventId);
            await _intentStore.SaveAsync(intent);
        }
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
