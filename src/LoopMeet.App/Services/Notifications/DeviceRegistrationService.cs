namespace LoopMeet.App.Services.Notifications;

public sealed class DeviceRegistrationService
{
    private readonly AppConfig _config;

    public DeviceRegistrationService(AppConfig config)
    {
        _config = config;
    }

    public Task SyncPermissionStateAsync(NotificationPermissionState state)
    {
        _ = _config;
        _ = state;
        return Task.CompletedTask;
    }
}
