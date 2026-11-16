using LoopMeet.Core.Models;

namespace LoopMeet.Core.Interfaces;

/// <summary>
/// Files reports as the calling user, <paramref name="reporterUserId"/> in SubmitAsync. The database
/// enforces every rule (023 research D1).
/// </summary>
public interface IAbuseReportRepository
{
    Task<ReportSubmission> SubmitAsync(Guid reporterUserId, NewAbuseReport report, CancellationToken cancellationToken = default);
}
