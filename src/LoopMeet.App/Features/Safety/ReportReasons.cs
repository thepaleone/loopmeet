namespace LoopMeet.App.Features.Safety;

/// <summary>A reason the report form offers. The picker shows <see cref="Label"/>.</summary>
public sealed record ReportReasonOption(string Code, string Label)
{
    public override string ToString() => Label;
}

/// <summary>
/// The five report reasons (feature 023), in the order the form lists them. The codes match the
/// Api contract and the abuse_reports.reason check. MAUI-free so it can be unit tested.
/// </summary>
public static class ReportReasons
{
    public static IReadOnlyList<ReportReasonOption> All { get; } =
    [
        new("inappropriate_photo_or_name", "Inappropriate photo or name"),
        new("harassment_or_threats", "Harassment or threats"),
        new("child_safety", "Child safety"),
        new("spam", "Spam"),
        new("other", "Other")
    ];
}
