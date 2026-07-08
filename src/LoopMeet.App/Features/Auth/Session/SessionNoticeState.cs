namespace LoopMeet.App.Features.Auth.Session;

/// <summary>
/// The only hand-off channel for session-ended notices (contract §6a). The
/// coordinator sets Pending before navigating to //login; the login screen
/// consumes it exactly once so the banner never resurfaces on later visits.
/// </summary>
public sealed class SessionNoticeState
{
    private readonly object _gate = new();
    private SignOutReason? _pending;

    public SignOutReason? Pending
    {
        set
        {
            lock (_gate)
            {
                _pending = value;
            }
        }
    }

    public SignOutReason? TakePending()
    {
        lock (_gate)
        {
            var taken = _pending;
            _pending = null;
            return taken;
        }
    }
}
