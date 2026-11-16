namespace LoopMeet.App.Features.Safety;

public sealed record FallbackMail(string To, string Subject, string Body);

/// <summary>
/// The email the report form offers when the report can't be sent (feature 023, FR-007). It holds
/// only what the reporter chose and saw, never ids. MAUI-free so it can be unit tested.
/// </summary>
public static class ReportFallbackMail
{
    public const string SafetyAddress = "abuse@loopmeet.io";

    public static FallbackMail Compose(string groupName, string? personName, string reasonLabel, string details, string appVersion)
    {
        var body = string.Join('\n',
            $"Group: {groupName}",
            $"Reported: {personName ?? "The group itself"}",
            $"Reason: {reasonLabel}",
            $"App version: {appVersion}",
            string.Empty,
            "Details:",
            ReportDraft.Trimmed(details));

        return new FallbackMail(SafetyAddress, $"LoopMeet report: {reasonLabel}", body);
    }
}
