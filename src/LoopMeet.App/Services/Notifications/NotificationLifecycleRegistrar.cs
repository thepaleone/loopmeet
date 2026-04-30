namespace LoopMeet.App.Services.Notifications;

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

    public NotificationLifecycleRegistrar(INotificationTapSource tapSource, NotificationService notificationService)
    {
        _tapSource = tapSource;
        _notificationService = notificationService;
    }

    public async Task RegisterAsync()
    {
        _tapSource.NotificationOpened += (additionalData) => _notificationService.HandleNotificationOpenedAsync(additionalData, isSignedIn: true);
        await _tapSource.StartAsync();
    }
}
