namespace LoopMeet.Core.Accounts;

public static class AccountDeletionPolicy
{
    /// <summary>
    /// How long a deletion request waits before permanent deletion. The authoritative value is
    /// in SQL (<c>request_account_deletion()</c>); this copy only projects the date shown before
    /// a request exists. Tests pin both.
    /// </summary>
    public static readonly TimeSpan GracePeriod = TimeSpan.FromDays(30);
}
