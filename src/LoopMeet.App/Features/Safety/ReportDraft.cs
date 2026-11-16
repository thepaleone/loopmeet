using System.Globalization;

namespace LoopMeet.App.Features.Safety;

/// <summary>
/// The report form's rules (feature 023). The server enforces the same limits; these give
/// feedback before sending. MAUI-free so it can be unit tested.
/// </summary>
public static class ReportDraft
{
    public const int MaxDetailsLength = 1000;

    public static bool CanSubmit(Guid? groupId, string? reasonCode, string? details)
    {
        var length = Trimmed(details).Length;
        return groupId is not null
            && !string.IsNullOrEmpty(reasonCode)
            && length is >= 1 and <= MaxDetailsLength;
    }

    /// <summary>"995 characters left". Counts what's typed, as the editor's length limit does.</summary>
    public static string Remaining(string? details)
    {
        var left = Math.Max(0, MaxDetailsLength - (details?.Length ?? 0));
        return left == 1
            ? "1 character left"
            : left.ToString("N0", CultureInfo.InvariantCulture) + " characters left";
    }

    public static string Trimmed(string? details) => details?.Trim() ?? string.Empty;
}
