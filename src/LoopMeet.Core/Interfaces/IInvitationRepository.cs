using LoopMeet.Core.Models;

namespace LoopMeet.Core.Interfaces;

public interface IInvitationRepository
{
    Task<Invitation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Invitation>> ListPendingByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> ExistsPendingForEmailAsync(Guid groupId, string email, CancellationToken cancellationToken = default);
    Task AddAsync(Invitation invitation, CancellationToken cancellationToken = default);
    Task UpdateAsync(Invitation invitation, CancellationToken cancellationToken = default);
}
