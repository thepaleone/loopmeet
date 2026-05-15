using Microsoft.Extensions.Logging;
using OneSignalSDK.DotNet;

namespace LoopMeet.App.Services.Notifications;

public sealed class OneSignalBootstrapService
{
    private readonly AppConfig _config;
    private readonly ILogger<OneSignalBootstrapService> _logger;
    private bool _initialized;

    public OneSignalBootstrapService(AppConfig config, ILogger<OneSignalBootstrapService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public bool IsInitialized => _initialized;

    public Task InitializeAsync()
    {
        if (_initialized)
        {
            return Task.CompletedTask;
        }

        if (string.IsNullOrWhiteSpace(_config.OneSignalAppId))
        {
            _logger.LogError(
                "OneSignal app id is not configured. Push notifications, permission prompts, and device registration via OneSignal will all be skipped. Set OneSignalAppId in AppConfig.");
            return Task.CompletedTask;
        }

        try
        {
            OneSignal.Initialize(_config.OneSignalAppId);
            _initialized = true;
            var appIdPreview = _config.OneSignalAppId.Length >= 8
                ? _config.OneSignalAppId[..8]
                : _config.OneSignalAppId;
            _logger.LogInformation("OneSignal initialized. AppIdPrefix={AppIdPrefix}", appIdPreview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OneSignal.Initialize threw; notifications will not work this session.");
        }

        return Task.CompletedTask;
    }
}
