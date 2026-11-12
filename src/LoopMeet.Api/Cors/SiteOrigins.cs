namespace LoopMeet.Api.Cors;

/// <summary>
/// Which browser origins may call the Api: the website and its previews (017 research D6).
/// An entry is an exact origin, or holds one <c>*</c> in its first host label that matches one or
/// more letters, digits or dashes and never a dot, so <c>https://*-loopmeet-account.joelgray.workers.dev</c>
/// admits <c>https://main-loopmeet-account.joelgray.workers.dev</c> and nothing outside that host.
/// supabase/functions/apple-authorization/cors.ts holds the same rule; both test suites read
/// site-origins.cases.json beside it.
/// </summary>
public static class SiteOrigins
{
    public const string PolicyName = "Site";

    /// <summary>Reads a comma-separated list, as <c>Cors:AllowedOrigins</c> and <c>SITE_ORIGINS</c> hold it.</summary>
    public static string[] Parse(string? csv) =>
        (csv ?? string.Empty).Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    public static bool IsAllowed(string? origin, IEnumerable<string> patterns) =>
        !string.IsNullOrEmpty(origin) && patterns.Any(pattern => Matches(origin, pattern.Trim()));

    private static bool Matches(string origin, string pattern)
    {
        var star = pattern.IndexOf('*');
        if (star < 0)
        {
            return string.Equals(origin, pattern, StringComparison.OrdinalIgnoreCase);
        }

        if (!WildcardIsInFirstHostLabel(pattern, star))
        {
            return false;
        }

        var prefix = pattern[..star];
        var suffix = pattern[(star + 1)..];
        if (origin.Length <= prefix.Length + suffix.Length
            || !origin.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            || !origin.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        foreach (var c in origin.AsSpan(prefix.Length, origin.Length - prefix.Length - suffix.Length))
        {
            if (!char.IsAsciiLetterOrDigit(c) && c != '-')
            {
                return false;
            }
        }

        return true;
    }

    private static bool WildcardIsInFirstHostLabel(string pattern, int star)
    {
        if (pattern.IndexOf('*', star + 1) >= 0)
        {
            return false;
        }

        var schemeEnd = pattern.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd < 0 || star < schemeEnd + 3)
        {
            return false;
        }

        var hostStart = schemeEnd + 3;
        return pattern.AsSpan(hostStart, star - hostStart).IndexOfAny('.', ':', '/') < 0;
    }
}
