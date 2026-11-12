using LoopMeet.Api.Services.Auth;
using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;

namespace LoopMeet.Api.Tests.Infrastructure;

/// <summary>
/// Mirrors request_account_deletion() and restore_account_deletion(): both act on the caller,
/// a repeat request keeps the original row, and restore is refused once the deadline has
/// passed or purging has started.
/// </summary>
public sealed class InMemoryAccountDeletionRepository : IAccountDeletionRepository
{
    private readonly InMemoryStore _store;
    private readonly CurrentUserService _currentUser;
    private readonly TimeProvider _timeProvider;

    public InMemoryAccountDeletionRepository(InMemoryStore store, CurrentUserService currentUser, TimeProvider timeProvider)
    {
        _store = store;
        _currentUser = currentUser;
        _timeProvider = timeProvider;
    }

    public Task<AccountDeletion?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult(_store.AccountDeletions.GetValueOrDefault(userId));
        }
    }

    public Task<AccountDeletion> RequestAsync(CancellationToken cancellationToken = default)
    {
        var userId = CallerId();
        lock (_store.SyncRoot)
        {
            if (!_store.AccountDeletions.TryGetValue(userId, out var deletion))
            {
                var now = _timeProvider.GetUtcNow();
                deletion = new AccountDeletion(userId, now, now.AddDays(30), AccountDeletionStatus.Pending);
                _store.AccountDeletions[userId] = deletion;
            }

            return Task.FromResult(deletion);
        }
    }

    public Task<AccountRestoreResult> RestoreAsync(CancellationToken cancellationToken = default)
    {
        var userId = CallerId();
        lock (_store.SyncRoot)
        {
            if (!_store.AccountDeletions.TryGetValue(userId, out var deletion))
            {
                return Task.FromResult(AccountRestoreResult.NotPending);
            }

            if (deletion.Status != AccountDeletionStatus.Pending || deletion.DeletesAt <= _timeProvider.GetUtcNow())
            {
                return Task.FromResult(AccountRestoreResult.DeletionInProgress);
            }

            _store.AccountDeletions.Remove(userId);
            return Task.FromResult(AccountRestoreResult.Restored);
        }
    }

    private Guid CallerId() =>
        _currentUser.UserId ?? throw new InvalidOperationException("not_authenticated");
}
