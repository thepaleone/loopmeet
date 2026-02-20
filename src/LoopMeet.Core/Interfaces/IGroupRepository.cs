using LoopMeet.Core.Models;

namespace LoopMeet.Core.Interfaces;

public interface IGroupRepository
{
    Task<Group?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Group>> ListByIdsAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Group>> ListOwnedAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Group>> ListMemberAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> ExistsNameForOwnerAsync(Guid ownerUserId, string name, CancellationToken cancellationToken = default);
    Task<Group> AddAsync(Group group, CancellationToken cancellationToken = default);
    Task UpdateAsync(Group group, CancellationToken cancellationToken = default);
}
