using Microsoft.Extensions.Logging;
using OneSignalSDK.DotNet;

namespace LoopMeet.App.Services.Notifications;

public sealed class OneSignalIdentityService
{
    private readonly ILogger<OneSignalIdentityService> _logger;

    public OneSignalIdentityService(ILogger<OneSignalIdentityService> logger)
    {
        _logger = logger;
    }

    public Task LoginAsync(Guid userId)
    {
        OneSignal.Login(userId.ToString());
        _logger.LogInformation("OneSignal login completed for user {UserId}", userId);
        return Task.CompletedTask;
    }
}
