using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;

namespace LoopMeet.Api.Tests.Infrastructure;

public sealed class InMemoryGroupRepository : IGroupRepository
{
    private readonly InMemoryStore _store;

    public InMemoryGroupRepository(InMemoryStore store)
    {
        _store = store;
    }

    public Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult(_store.Groups.FirstOrDefault(group => group.Id == id));
        }
    }

    public Task<IReadOnlyList<Group>> ListOwnedAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var results = _store.Groups
                .Where(group => group.OwnerUserId == ownerUserId)
                .OrderBy(group => group.Name)
                .ToList();
            return Task.FromResult<IReadOnlyList<Group>>(results);
        }
    }

    public Task<IReadOnlyList<Group>> ListMemberAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var groupIds = _store.Memberships
                .Where(membership => membership.UserId == userId)
                .Select(membership => membership.GroupId)
                .Distinct()
                .ToList();

            var results = _store.Groups
                .Where(group => groupIds.Contains(group.Id))
                .OrderBy(group => group.Name)
                .ToList();
            return Task.FromResult<IReadOnlyList<Group>>(results);
        }
    }

    public Task<bool> ExistsNameForOwnerAsync(Guid ownerUserId, string name, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var exists = _store.Groups.Any(group => group.OwnerUserId == ownerUserId && group.Name == name);
            return Task.FromResult(exists);
        }
    }

    public Task<IReadOnlyList<Group>> ListByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var lookup = ids.ToHashSet();
            var results = _store.Groups
                .Where(group => lookup.Contains(group.Id))
                .ToList();
            return Task.FromResult<IReadOnlyList<Group>>(results);
        }
    }

    public Task<Group> AddAsync(Group group, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            _store.Groups.Add(group);
        }

        return Task.FromResult(group);
    }

    public Task UpdateAsync(Group group, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var index = _store.Groups.FindIndex(candidate => candidate.Id == group.Id);
            if (index >= 0)
            {
                _store.Groups[index] = group;
            }
        }

        return Task.CompletedTask;
    }
}
