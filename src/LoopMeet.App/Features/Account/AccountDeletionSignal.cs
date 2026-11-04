namespace LoopMeet.App.Features.Account;

/// <summary>
/// Raised when the Api reports that the signed-in account is scheduled for deletion. MAUI-free so
/// AccountStatusHandler can be unit tested; AccountDeletionGate turns it into navigation.
/// </summary>
public sealed class AccountDeletionSignal
{
    public event Action<DateTimeOffset>? PendingDeletion;

    public void Raise(DateTimeOffset deletesAt) => PendingDeletion?.Invoke(deletesAt);
}
