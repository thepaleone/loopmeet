using LoopMeet.Core.Models;

namespace LoopMeet.Core.Interfaces;

public interface IMembershipRepository
{
    Task<Membership?> GetByUserAndGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Membership>> ListMembersAsync(Guid groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Memberships across several groups in one round trip. Used by meetup reads
    /// to resolve organizer names without a per-group query.
    /// </summary>
    Task<IReadOnlyList<Membership>> ListMembersByGroupsAsync(IReadOnlyList<Guid> groupIds, CancellationToken cancellationToken = default);
    Task AddAsync(Membership membership, CancellationToken cancellationToken = default);
    Task AddFromInvitationAsync(Membership membership, string invitedEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes <paramref name="userId"/>'s membership of the group (023 US2). The Supabase client
    /// acts as that user, so leave_group() reads them from the token; the id names whose
    /// membership the call removes.
    /// </summary>
    Task<MembershipExitResult> LeaveAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default);
}
