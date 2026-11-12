using LoopMeet.Core.Models;

namespace LoopMeet.Core.Interfaces;

public enum AccountRestoreResult
{
    Restored,
    NotPending,
    DeletionInProgress
}

/// <summary>Acts on the calling user's own deletion record.</summary>
public interface IAccountDeletionRepository
{
    Task<AccountDeletion?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AccountDeletion> RequestAsync(CancellationToken cancellationToken = default);
    Task<AccountRestoreResult> RestoreAsync(CancellationToken cancellationToken = default);
}
