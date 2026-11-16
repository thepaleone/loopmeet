using LoopMeet.Api.Contracts;
using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;
using LoopMeet.Core.Validators;

namespace LoopMeet.Api.Services.Safety;

/// <summary>
/// Files a report (023 US1). Logs by id and reason code only: the details and the reporter's
/// email reach the safety inbox, never a log (FR-021).
/// </summary>
public sealed class ReportCommandService
{
    private readonly IAbuseReportRepository _reports;
    private readonly AbuseReportValidator _validator = new();
    private readonly ILogger<ReportCommandService> _logger;

    public ReportCommandService(IAbuseReportRepository reports, ILogger<ReportCommandService> logger)
    {
        _reports = reports;
        _logger = logger;
    }

    public async Task<ReportSubmission> SubmitAsync(Guid reporterUserId, ReportRequest request, CancellationToken cancellationToken = default)
    {
        if (!ReportReasonCodes.TryParse(request.Reason, out var reason))
        {
            _logger.LogWarning("ReportRefused for {UserId} in {GroupId}: {Status}", reporterUserId, request.GroupId, ReportSubmissionStatus.Invalid);
            return new ReportSubmission(ReportSubmissionStatus.Invalid, null);
        }

        var report = new NewAbuseReport(request.GroupId, request.ReportedUserId, reason, request.Details ?? string.Empty);
        var result = _validator.Validate(report).IsValid
            ? await _reports.SubmitAsync(reporterUserId, report, cancellationToken)
            : new ReportSubmission(ReportSubmissionStatus.Invalid, null);

        if (result.Status == ReportSubmissionStatus.Submitted)
        {
            _logger.LogInformation("ReportSubmitted {ReportId} by {UserId} in {GroupId}, reason {Reason}",
                result.Report!.Id, reporterUserId, request.GroupId, ReportReasonCodes.ToCode(reason));
        }
        else
        {
            _logger.LogWarning("ReportRefused for {UserId} in {GroupId}: {Status}", reporterUserId, request.GroupId, result.Status);
        }

        return result;
    }
}
