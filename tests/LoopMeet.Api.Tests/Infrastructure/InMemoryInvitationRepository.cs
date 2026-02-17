using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;

namespace LoopMeet.Api.Tests.Infrastructure;

public sealed class InMemoryInvitationRepository : IInvitationRepository
{
    private readonly InMemoryStore _store;

    public InMemoryInvitationRepository(InMemoryStore store)
    {
        _store = store;
    }

    public Task<Invitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult(_store.Invitations.FirstOrDefault(invitation => invitation.Id == id));
        }
    }

    public Task<IReadOnlyList<Invitation>> ListPendingByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var results = _store.Invitations
                .Where(invitation => invitation.InvitedEmail == email && invitation.Status == "pending")
                .OrderBy(invitation => invitation.CreatedAt)
                .ToList();
            return Task.FromResult<IReadOnlyList<Invitation>>(results);
        }
    }

    public Task<bool> ExistsPendingForEmailAsync(Guid groupId, string email, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var exists = _store.Invitations.Any(invitation =>
                invitation.GroupId == groupId
                && invitation.InvitedEmail == email
                && invitation.Status == "pending");
            return Task.FromResult(exists);
        }
    }

    public Task AddAsync(Invitation invitation, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            _store.Invitations.Add(invitation);
        }

        return Task.CompletedTask;
    }

    public Task UpdateAsync(Invitation invitation, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var index = _store.Invitations.FindIndex(candidate => candidate.Id == invitation.Id);
            if (index >= 0)
            {
                _store.Invitations[index] = invitation;
            }
        }

        return Task.CompletedTask;
    }
}
