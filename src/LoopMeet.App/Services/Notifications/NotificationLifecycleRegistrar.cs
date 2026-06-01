namespace LoopMeet.App.Services.Notifications;

using Microsoft.Extensions.Logging;

public interface INotificationTapSource
{
    event Func<IDictionary<string, object?>, Task>? NotificationOpened;
    Task StartAsync();
}

public sealed class NoOpNotificationTapSource : INotificationTapSource
{
    public event Func<IDictionary<string, object?>, Task>? NotificationOpened;

    public Task StartAsync()
    {
        _ = NotificationOpened;
        return Task.CompletedTask;
    }
}

public sealed class NotificationLifecycleRegistrar
{
    private readonly INotificationTapSource _tapSource;
    private readonly NotificationService _notificationService;
    private readonly ILogger<NotificationLifecycleRegistrar> _logger;

    public NotificationLifecycleRegistrar(
        INotificationTapSource tapSource,
        NotificationService notificationService,
        ILogger<NotificationLifecycleRegistrar> logger)
    {
        _tapSource = tapSource;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task RegisterAsync()
    {
        _tapSource.NotificationOpened += (additionalData) =>
        {
            _logger.LogInformation("Notification tap dispatched to NotificationService. Keys={Keys}", string.Join(",", additionalData.Keys));
            return _notificationService.HandleNotificationOpenedAsync(additionalData, isSignedIn: true);
        };
        await _tapSource.StartAsync();
    }
}
