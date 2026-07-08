namespace LoopMeet.App.Features.Auth.Session;

/// <summary>
/// Decides whether a renewal attempt is worthwhile right now. Deterministic —
/// all time is injected. Attempt when the token is expired or inside its final
/// fifth of lifetime, or when no successful check happened within the debounce
/// window; skip otherwise so rapid foreground/background cycling cannot stack
/// refresh calls (Supabase refresh tokens are single-use — a raced second
/// refresh reads as a false definitive rejection).
/// </summary>
public sealed class SessionRenewalPolicy
{
    public static readonly TimeSpan DebounceWindow = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan DefaultExpiryMargin = TimeSpan.FromMinutes(5);

    public bool ShouldAttempt(DateTimeOffset nowUtc, DateTimeOffset? lastSuccessUtc, DateTimeOffset? tokenExpiryUtc)
    {
        if (tokenExpiryUtc is null)
        {
            return true;
        }

        if (nowUtc >= tokenExpiryUtc.Value - ExpiryMargin(lastSuccessUtc, tokenExpiryUtc.Value))
        {
            return true;
        }

        return lastSuccessUtc is null || nowUtc - lastSuccessUtc.Value >= DebounceWindow;
    }

    /// <summary>
    /// The "final fifth" of the token lifetime, matching Gotrue's own refresh
    /// margin. Lifetime is approximated from the last successful check (the
    /// token was issued then); without one, fall back to a fixed margin.
    /// </summary>
    private static TimeSpan ExpiryMargin(DateTimeOffset? lastSuccessUtc, DateTimeOffset tokenExpiryUtc)
    {
        if (lastSuccessUtc is null || lastSuccessUtc.Value >= tokenExpiryUtc)
        {
            return DefaultExpiryMargin;
        }

        return (tokenExpiryUtc - lastSuccessUtc.Value) / 5;
    }
}
