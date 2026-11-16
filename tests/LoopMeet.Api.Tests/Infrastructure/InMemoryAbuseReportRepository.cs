using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;

namespace LoopMeet.Api.Tests.Infrastructure;

/// <summary>
/// Mirrors submit_abuse_report(): the caller must be a member of the group, the reported person a
/// different, active member, the details 1–1,000 characters after trimming, and no more than ten
/// reports in the last 24 hours.
/// </summary>
public sealed class InMemoryAbuseReportRepository : IAbuseReportRepository
{
    private const int DailyLimit = 10;

    private readonly InMemoryStore _store;
    private readonly TimeProvider _timeProvider;

    public InMemoryAbuseReportRepository(InMemoryStore store, TimeProvider timeProvider)
    {
        _store = store;
        _timeProvider = timeProvider;
    }

    public Task<ReportSubmission> SubmitAsync(Guid reporterId, NewAbuseReport report, CancellationToken cancellationToken = default)
    {
        var details = report.Details.Trim();
        var now = _timeProvider.GetUtcNow();

        lock (_store.SyncRoot)
        {
            if (!Enum.IsDefined(report.Reason) || details.Length is < 1 or > 1000)
            {
                return Task.FromResult(new ReportSubmission(ReportSubmissionStatus.Invalid, null));
            }

            if (!IsActiveMember(report.GroupId, reporterId))
            {
                return Task.FromResult(new ReportSubmission(ReportSubmissionStatus.NotGroupMember, null));
            }

            if (report.ReportedUserId is { } reportedId
                && (reportedId == reporterId || !IsActiveMember(report.GroupId, reportedId)))
            {
                return Task.FromResult(new ReportSubmission(ReportSubmissionStatus.InvalidReportedUser, null));
            }

            var recent = _store.AbuseReports.Count(existing =>
                existing.ReporterUserId == reporterId && existing.ReceivedAt > now.AddHours(-24));
            if (recent >= DailyLimit)
            {
                return Task.FromResult(new ReportSubmission(ReportSubmissionStatus.LimitReached, null));
            }

            var stored = new AbuseReport(Guid.NewGuid(), reporterId, report.GroupId, report.ReportedUserId, report.Reason, details, now);
            _store.AbuseReports.Add(stored);
            return Task.FromResult(new ReportSubmission(ReportSubmissionStatus.Submitted, stored));
        }
    }

    private bool IsActiveMember(Guid groupId, Guid userId) =>
        !_store.AccountDeletions.ContainsKey(userId)
        && _store.Memberships.Any(membership => membership.GroupId == groupId && membership.UserId == userId);
}
