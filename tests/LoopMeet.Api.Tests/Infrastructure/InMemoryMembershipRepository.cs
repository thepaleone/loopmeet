using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;

namespace LoopMeet.Api.Tests.Infrastructure;

public sealed class InMemoryMembershipRepository : IMembershipRepository
{
    private readonly InMemoryStore _store;

    public InMemoryMembershipRepository(InMemoryStore store)
    {
        _store = store;
    }

    public Task<Membership?> GetByUserAndGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult(_store.Memberships.FirstOrDefault(
                membership => membership.UserId == userId && membership.GroupId == groupId));
        }
    }

    public Task<IReadOnlyList<Membership>> ListMembersAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var results = _store.Memberships
                .Where(membership => membership.GroupId == groupId)
                .ToList();
            return Task.FromResult<IReadOnlyList<Membership>>(results);
        }
    }

    public Task AddAsync(Membership membership, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            _store.Memberships.Add(membership);
        }

        return Task.CompletedTask;
    }
}
