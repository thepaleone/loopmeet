using System.Text.Json;

namespace LoopMeet.App.Services.Notifications;

public sealed class PendingNotificationIntentStore
{
    private const string StorageKey = "pending_notification_intent";

    public Task SaveAsync(NotificationIntent intent)
    {
        Preferences.Default.Set(StorageKey, JsonSerializer.Serialize(intent));
        return Task.CompletedTask;
    }

    public Task<NotificationIntent?> ConsumeAsync()
    {
        if (!Preferences.Default.ContainsKey(StorageKey))
        {
            return Task.FromResult<NotificationIntent?>(null);
        }

        var raw = Preferences.Default.Get(StorageKey, string.Empty);
        Preferences.Default.Remove(StorageKey);

        if (string.IsNullOrWhiteSpace(raw))
        {
            return Task.FromResult<NotificationIntent?>(null);
        }

        return Task.FromResult(JsonSerializer.Deserialize<NotificationIntent>(raw));
    }
}

public sealed record NotificationIntent(
    string NotificationType,
    string TargetKind,
    string? TargetId,
    string FallbackRoute,
    string EventId,
    string SentAt);
