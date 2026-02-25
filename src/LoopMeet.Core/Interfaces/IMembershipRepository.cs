using LoopMeet.Core.Models;

namespace LoopMeet.Core.Interfaces;

public interface IMembershipRepository
{
    Task<Membership?> GetByUserAndGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Membership>> ListMembersAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task AddAsync(Membership membership, CancellationToken cancellationToken = default);
}
