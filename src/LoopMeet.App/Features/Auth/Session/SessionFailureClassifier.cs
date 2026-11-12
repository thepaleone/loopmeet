using Supabase.Gotrue.Exceptions;

namespace LoopMeet.App.Features.Auth.Session;

public enum SessionFailureKind
{
    Definitive,
    Transient
}

/// <summary>
/// Classifies session restore/renewal failures per FR-004a: only a definitive
/// rejection by the auth server may end a session. Anything ambiguous is
/// Transient — never force a sign-out on uncertainty.
/// </summary>
public static class SessionFailureClassifier
{
    public static SessionFailureKind Classify(Exception exception)
    {
        if (exception is GotrueException gotrue)
        {
            switch (gotrue.Reason)
            {
                case FailureHint.Reason.ExpiredRefreshToken:
                case FailureHint.Reason.InvalidRefreshToken:
                case FailureHint.Reason.NoSessionFound:
                    return SessionFailureKind.Definitive;
                // Gotrue 8 classifies by error_code, so a network reason may carry any status.
                case FailureHint.Reason.Offline:
                case FailureHint.Reason.NetworkError:
                case FailureHint.Reason.CloudflareNetworkError:
                    return SessionFailureKind.Transient;
            }

            return gotrue.StatusCode is 400 or 401 or 403
                ? SessionFailureKind.Definitive
                : SessionFailureKind.Transient;
        }

        return SessionFailureKind.Transient;
    }
}
