using System.Text.Json;

namespace LoopMeet.App.Features.Safety;

/// <summary>
/// Maps the Api's safety error codes to what the user sees (specs/023-safety-controls/contracts/api.md).
/// MAUI-free so it can be unit tested.
/// </summary>
public static class SafetyErrorText
{
    private static readonly IReadOnlyDictionary<string, string> Messages = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["invalid_report"] = "Choose a reason and describe what happened (up to 1,000 characters).",
        ["not_group_member"] = "You can only report groups you belong to.",
        ["invalid_reported_user"] = "Choose someone in this group other than yourself.",
        ["report_limit_reached"] = "You've sent 10 reports today. You can send more later, or email abuse@loopmeet.io.",
        ["owner_cannot_leave"] = "Group owners can't leave their own group."
    };

    public static string Message(string? code) =>
        code is not null && Messages.TryGetValue(code, out var message) ? message : "Something went wrong. Please try again.";

    /// <summary>
    /// Leaving a group the caller is no longer in means they already left, or were removed, from
    /// another device: the App returns to Groups with no error (spec Edge Cases, racing actions).
    /// </summary>
    public static bool MeansAlreadyLeft(string? code) => code == "not_group_member";

    /// <summary>
    /// A network failure (no status) or a server error offers the email route. A refusal (4xx)
    /// wouldn't change if the same report went by email, so it shows its message instead.
    /// </summary>
    public static bool OffersEmailFallback(int? statusCode) => statusCode is null or >= 500;

    /// <summary>The <c>code</c> of an Api ErrorResponse body, or null when the body has none.</summary>
    public static string? CodeFrom(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            return document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("code", out var code)
                && code.ValueKind == JsonValueKind.String
                    ? code.GetString()
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
