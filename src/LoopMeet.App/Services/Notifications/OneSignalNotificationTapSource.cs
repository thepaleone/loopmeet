using OneSignalSDK.DotNet;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Text.Json;

namespace LoopMeet.App.Services.Notifications;

public sealed class OneSignalNotificationTapSource : INotificationTapSource
{
    private bool _started;
    private readonly ILogger<OneSignalNotificationTapSource> _logger;

    public OneSignalNotificationTapSource(ILogger<OneSignalNotificationTapSource> logger)
    {
        _logger = logger;
    }

    public event Func<IDictionary<string, object?>, Task>? NotificationOpened;

    public Task StartAsync()
    {
        if (_started)
        {
            return Task.CompletedTask;
        }

        OneSignal.Notifications.Clicked += (_, args) =>
        {
            var additionalData = ExtractAdditionalData(args);
            _logger.LogInformation("OneSignal click received. Keys={Keys}", string.Join(",", additionalData.Keys));
            NotificationTapBridge.Publish(additionalData);
            _ = NotificationOpened?.Invoke(additionalData);
        };

        foreach (var pendingTap in NotificationTapBridge.Drain())
        {
            _logger.LogInformation("Replaying buffered notification tap. Keys={Keys}", string.Join(",", pendingTap.Keys));
            _ = NotificationOpened?.Invoke(pendingTap);
        }

        _started = true;
        return Task.CompletedTask;
    }

    private static IDictionary<string, object?> ExtractAdditionalData(object args)
    {
        var extracted = new Dictionary<string, object?>();
        var notification = args.GetType().GetProperty("Notification")?.GetValue(args);
        var raw = notification?.GetType().GetProperty("AdditionalData")?.GetValue(notification);
        var root = ToDictionary(raw);

        if (root.Count == 0)
        {
            var result = args.GetType().GetProperty("Result")?.GetValue(args);
            var resultNotification = result?.GetType().GetProperty("Notification")?.GetValue(result);
            raw = resultNotification?.GetType().GetProperty("AdditionalData")?.GetValue(resultNotification);
            root = ToDictionary(raw);
        }

        if (root.Count == 0)
        {
            var result = args.GetType().GetProperty("Result")?.GetValue(args);
            var resultJson = result?.ToString();
            if (!string.IsNullOrWhiteSpace(resultJson))
            {
                root = ToDictionary(resultJson);
            }
        }

        CopyKnownKeys(extracted, root);

        if (extracted.Count == 0 && root.Count > 0)
        {
            foreach (var pair in root)
            {
                extracted[pair.Key] = pair.Value?.ToString();
            }
        }

        if (root.TryGetValue("custom", out var custom))
        {
            var customMap = ToDictionary(custom);
            if (customMap.TryGetValue("a", out var additional))
            {
                CopyKnownKeys(extracted, ToDictionary(additional));
            }
        }

        return extracted;
    }

    private static Dictionary<string, object?> ToDictionary(object? value)
    {
        if (value is null)
        {
            return new Dictionary<string, object?>();
        }

        if (value is Dictionary<string, object?> dict)
        {
            return dict;
        }

        if (value is IDictionary<string, object> map)
        {
            return map.ToDictionary(pair => pair.Key, pair => (object?)pair.Value);
        }

        if (value is IDictionary dictionary)
        {
            var result = new Dictionary<string, object?>();
            foreach (DictionaryEntry entry in dictionary)
            {
                var key = entry.Key?.ToString();
                if (!string.IsNullOrWhiteSpace(key))
                {
                    result[key] = entry.Value;
                }
            }

            return result;
        }

        if (value is IEnumerable enumerable)
        {
            var result = new Dictionary<string, object?>();
            foreach (var item in enumerable)
            {
                if (item is null)
                {
                    continue;
                }

                var itemType = item.GetType();
                var keyProperty = itemType.GetProperty("Key");
                var valueProperty = itemType.GetProperty("Value");
                if (keyProperty is null || valueProperty is null)
                {
                    continue;
                }

                var key = keyProperty.GetValue(item)?.ToString();
                if (string.IsNullOrWhiteSpace(key))
                {
                    continue;
                }

                result[key] = valueProperty.GetValue(item);
            }

            if (result.Count > 0)
            {
                return result;
            }
        }

        if (value is JsonElement jsonElement)
        {
            return JsonElementToDictionary(jsonElement);
        }

        if (value is string json && json.TrimStart().StartsWith("{", StringComparison.Ordinal))
        {
            try
            {
                using var document = JsonDocument.Parse(json);
                return JsonElementToDictionary(document.RootElement);
            }
            catch (JsonException)
            {
                return new Dictionary<string, object?>();
            }
        }

        return new Dictionary<string, object?>();
    }

    private static Dictionary<string, object?> JsonElementToDictionary(JsonElement element)
    {
        var result = new Dictionary<string, object?>();
        if (element.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (var property in element.EnumerateObject())
        {
            result[property.Name] = property.Value.ValueKind == JsonValueKind.String
                ? property.Value.GetString()
                : property.Value.ToString();
        }

        return result;
    }

    private static void CopyKnownKeys(Dictionary<string, object?> target, Dictionary<string, object?> source)
    {
        foreach (var key in KnownKeys)
        {
            if (!target.ContainsKey(key)
                && source.TryGetValue(key, out var value)
                && value is not null)
            {
                target[key] = value.ToString();
            }
        }
    }

    private static readonly string[] KnownKeys =
    [
        "notification_type",
        "target_kind",
        "target_id",
        "fallback_route",
        "event_id",
        "sent_at",
    ];
}
