using Microsoft.Extensions.Logging;

namespace LoopMeet.Api.Services.Notifications;

public sealed class NotificationAuditService
{
    private readonly ILogger<NotificationAuditService> _logger;

    public NotificationAuditService(ILogger<NotificationAuditService> logger)
    {
        _logger = logger;
    }

    public Task RecordSendAttemptAsync(string eventId, string notificationType, string userId, string status)
    {
        _logger.LogInformation(
            "notification_send_attempt event_id={EventId} type={NotificationType} user_id={UserId} status={Status}",
            eventId,
            notificationType,
            userId,
            status);
        return Task.CompletedTask;
    }

    public Task RecordOpenAsync(string eventId, string? userId, string navigationResult, string? resolvedRoute)
    {
        _logger.LogInformation(
            "notification_open event_id={EventId} user_id={UserId} result={NavigationResult} route={ResolvedRoute}",
            eventId,
            userId,
            navigationResult,
            resolvedRoute);
        return Task.CompletedTask;
    }
}
