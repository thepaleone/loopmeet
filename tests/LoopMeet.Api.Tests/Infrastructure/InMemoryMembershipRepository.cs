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

    public Task<IReadOnlyList<Membership>> ListMembersByGroupsAsync(IReadOnlyList<Guid> groupIds, CancellationToken cancellationToken = default)
    {
        if (groupIds.Count == 0)
        {
            return Task.FromResult<IReadOnlyList<Membership>>(Array.Empty<Membership>());
        }

        lock (_store.SyncRoot)
        {
            var results = _store.Memberships
                .Where(membership => groupIds.Contains(membership.GroupId))
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

    public Task AddFromInvitationAsync(Membership membership, string invitedEmail, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            _store.Memberships.Add(membership);
        }

        return Task.CompletedTask;
    }

    /// <summary>Mirrors leave_group(): only a member can leave, and never the owner.</summary>
    public Task<MembershipExitResult> LeaveAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var membership = _store.Memberships.FirstOrDefault(m => m.GroupId == groupId && m.UserId == userId);
            if (membership is null)
            {
                return Task.FromResult(MembershipExitResult.NotGroupMember);
            }

            var group = _store.Groups.FirstOrDefault(g => g.Id == groupId);
            if (membership.Role == "owner" || group?.OwnerUserId == userId)
            {
                return Task.FromResult(MembershipExitResult.OwnerCannotLeave);
            }

            _store.Memberships.Remove(membership);
            return Task.FromResult(MembershipExitResult.Done);
        }
    }
}
