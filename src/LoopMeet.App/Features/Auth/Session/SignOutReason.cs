namespace LoopMeet.App.Features.Auth.Session;

public enum SignOutReason
{
    UserInitiated,
    SessionRejected,
    SessionRejectedWithUnsavedInput
}

public static class SignOutNotices
{
    public static string? For(SignOutReason? reason) => reason switch
    {
        SignOutReason.SessionRejected => "Your session ended. Please sign in again.",
        SignOutReason.SessionRejectedWithUnsavedInput => "Your session ended and unsaved changes were lost. Please sign in again.",
        _ => null
    };
}
