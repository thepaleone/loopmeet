namespace LoopMeet.Core.Models;

/// <summary>A pending or in-progress account deletion. No record means the account is active.</summary>
public sealed record AccountDeletion(Guid UserId, DateTimeOffset RequestedAt, DateTimeOffset DeletesAt, AccountDeletionStatus Status);

public enum AccountDeletionStatus
{
    Pending,
    Purging
}
