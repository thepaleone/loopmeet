using Microsoft.Extensions.Logging;
using OneSignalSDK.DotNet;

namespace LoopMeet.App.Services.Notifications;

public sealed class OneSignalBootstrapService
{
    private readonly AppConfig _config;
    private readonly ILogger<OneSignalBootstrapService> _logger;

    public OneSignalBootstrapService(AppConfig config, ILogger<OneSignalBootstrapService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public Task InitializeAsync()
    {
        if (string.IsNullOrWhiteSpace(_config.OneSignalAppId))
        {
            _logger.LogWarning("OneSignal app id is not configured; skipping OneSignal initialization.");
            return Task.CompletedTask;
        }

        OneSignal.Initialize(_config.OneSignalAppId);
        var appIdPreview = _config.OneSignalAppId.Length >= 8
            ? _config.OneSignalAppId[..8]
            : _config.OneSignalAppId;
        _logger.LogInformation("OneSignal initialized. AppIdPrefix={AppIdPrefix}", appIdPreview);
        return Task.CompletedTask;
    }
}
