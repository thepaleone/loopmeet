namespace LoopMeet.Core.Models;

public enum ReportReason
{
    InappropriatePhotoOrName,
    HarassmentOrThreats,
    ChildSafety,
    Spam,
    Other
}

/// <summary>
/// The wire and database codes for <see cref="ReportReason"/>. They match the check constraint on
/// abuse_reports.reason and the App's ReportReasons.
/// </summary>
public static class ReportReasonCodes
{
    private static readonly IReadOnlyDictionary<ReportReason, string> Codes = new Dictionary<ReportReason, string>
    {
        [ReportReason.InappropriatePhotoOrName] = "inappropriate_photo_or_name",
        [ReportReason.HarassmentOrThreats] = "harassment_or_threats",
        [ReportReason.ChildSafety] = "child_safety",
        [ReportReason.Spam] = "spam",
        [ReportReason.Other] = "other"
    };

    public static string ToCode(ReportReason reason) => Codes[reason];

    public static bool TryParse(string? code, out ReportReason reason)
    {
        foreach (var (candidate, candidateCode) in Codes)
        {
            if (string.Equals(candidateCode, code, StringComparison.Ordinal))
            {
                reason = candidate;
                return true;
            }
        }

        reason = default;
        return false;
    }
}
