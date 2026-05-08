using OneSignalSDK.DotNet;

namespace LoopMeet.App.Services.Notifications;

public sealed class OneSignalNotificationTapSource : INotificationTapSource
{
    private bool _started;

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
            _ = NotificationOpened?.Invoke(additionalData);
        };

        _started = true;
        return Task.CompletedTask;
    }

    private static IDictionary<string, object?> ExtractAdditionalData(object args)
    {
        var result = args.GetType().GetProperty("Result")?.GetValue(args);
        var notification = result?.GetType().GetProperty("Notification")?.GetValue(result);
        var additionalData = notification?.GetType().GetProperty("AdditionalData")?.GetValue(notification);

        return additionalData as IDictionary<string, object?> ?? new Dictionary<string, object?>();
    }
}
