using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using LoopMeet.App.Services.Notifications;
using System.Text.Json;

namespace LoopMeet.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        PublishTapFromIntent(Intent);
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        if (intent is not null)
        {
            Intent = intent;
        }

        PublishTapFromIntent(intent);
    }

    private static void PublishTapFromIntent(Intent? intent)
    {
        var additionalData = ExtractAdditionalData(intent);
        Log.Debug("LoopMeet.Notifications", $"MainActivity intent inspected. Action={intent?.Action} Keys={string.Join(",", additionalData.Keys)}");
        if (additionalData.Count > 0)
        {
            NotificationTapBridge.Publish(additionalData);
        }
    }

    private static Dictionary<string, object?> ExtractAdditionalData(Intent? intent)
    {
        var result = new Dictionary<string, object?>();
        if (intent is null)
        {
            return result;
        }

        AddKnownKeys(result, intent.Extras);
        AddRawExtras(result, intent.Extras);

        var oneSignalData = intent.GetStringExtra("onesignalData");
        if (!string.IsNullOrWhiteSpace(oneSignalData))
        {
            AddKnownKeysFromJson(result, oneSignalData);
        }

        return result;
    }

    private static void AddKnownKeys(Dictionary<string, object?> target, Bundle? extras)
    {
        if (extras is null)
        {
            return;
        }

        foreach (var key in KnownKeys)
        {
            if (!target.ContainsKey(key) && extras.ContainsKey(key))
            {
                target[key] = extras.Get(key)?.ToString();
            }
        }
    }

    private static void AddRawExtras(Dictionary<string, object?> target, Bundle? extras)
    {
        if (extras is null)
        {
            return;
        }

        foreach (var key in extras.KeySet())
        {
            var rawKey = $"raw:{key}";
            if (!target.ContainsKey(rawKey))
            {
                target[rawKey] = extras.Get(key)?.ToString();
            }
        }
    }

    private static void AddKnownKeysFromJson(Dictionary<string, object?> target, string rawJson)
    {
        try
        {
            using var document = JsonDocument.Parse(rawJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            AddKnownKeysFromElement(target, root);

            if (root.TryGetProperty("custom", out var custom)
                && custom.ValueKind == JsonValueKind.Object
                && custom.TryGetProperty("a", out var additional)
                && additional.ValueKind == JsonValueKind.Object)
            {
                AddKnownKeysFromElement(target, additional);
            }
        }
        catch (JsonException)
        {
        }
    }

    private static void AddKnownKeysFromElement(Dictionary<string, object?> target, JsonElement element)
    {
        foreach (var key in KnownKeys)
        {
            if (!target.ContainsKey(key)
                && element.TryGetProperty(key, out var value)
                && value.ValueKind != JsonValueKind.Null)
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
