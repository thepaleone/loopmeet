namespace LoopMeet.App.Features.Auth.Session;

/// <summary>
/// The ApiAuthHandler-facing surface of the SessionCoordinator (contract §2).
/// </summary>
public interface ISessionTokenSource
{
    /// <summary>Current session access token only — no fallback stores.</summary>
    string? GetAccessToken();

    /// <summary>
    /// Forces one renewal attempt in response to an API 401. The server signal
    /// is authoritative, so this bypasses the debounce (but stays single-flight).
    /// </summary>
    Task<RenewalOutcome> RefreshForRetryAsync();
}

public enum RenewalTrigger
{
    AppForegrounded,
    ApiUnauthorized,
    StartupRevalidation
}

public enum RenewalOutcome
{
    /// <summary>Token fresh enough; no attempt made.</summary>
    StillValid,
    Renewed,
    /// <summary>Renewal failed transiently (offline, timeout, 5xx); session kept per FR-004a.</summary>
    TransientFailureKeptSession,
    /// <summary>Auth server definitively rejected the session; full sign-out performed.</summary>
    DefinitivelyRejectedSignedOut,
    /// <summary>No session/refresh token to renew.</summary>
    Skipped
}

/// <summary>Destination of the startup session check (contract §1).</summary>
public sealed record StartupResolution(string Route, SignOutReason? Notice);
