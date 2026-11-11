// Inside the ...Auth.Session namespace the plain name "Session" binds to the
// namespace itself, so the Gotrue type needs an alias.
using GotrueSession = Supabase.Gotrue.Session;

namespace LoopMeet.App.Features.Auth.Session;

/// <summary>
/// When a session's access token expires. Supabase.Gotrue 8 dropped
/// <c>Session.ExpiresAt()</c> and <c>Expired()</c>; this is the same rule:
/// the client's issue time plus the server's <c>expires_in</c>.
/// </summary>
public static class SessionExpiry
{
    public static DateTimeOffset? ExpiresAtUtc(GotrueSession session)
    {
        if (session.ExpiresIn <= 0 || session.CreatedAt == default)
        {
            return null;
        }

        var issuedUtc = session.CreatedAt.Kind == DateTimeKind.Local
            ? session.CreatedAt.ToUniversalTime()
            : DateTime.SpecifyKind(session.CreatedAt, DateTimeKind.Utc);
        return new DateTimeOffset(issuedUtc).AddSeconds(session.ExpiresIn);
    }

    /// <summary>An unknown expiry counts as expired, so the session is renewed rather than trusted.</summary>
    public static bool IsExpired(GotrueSession session, DateTimeOffset nowUtc) =>
        ExpiresAtUtc(session) is not { } expiresAt || expiresAt <= nowUtc;
}
