namespace LoopMeet.Core.Models;

/// <summary>A recorded report (023 US1). Kept for a year, through either account's deletion.</summary>
public sealed record AbuseReport(
    Guid Id,
    Guid ReporterUserId,
    Guid GroupId,
    Guid? ReportedUserId,
    ReportReason Reason,
    string Details,
    DateTimeOffset ReceivedAt);

/// <summary>A report as the caller submits it. <c>ReportedUserId</c> null means the group itself.</summary>
public sealed record NewAbuseReport(Guid GroupId, Guid? ReportedUserId, ReportReason Reason, string Details);

public enum ReportSubmissionStatus
{
    Submitted,
    Invalid,
    NotGroupMember,
    InvalidReportedUser,
    LimitReached
}

public sealed record ReportSubmission(ReportSubmissionStatus Status, AbuseReport? Report);
