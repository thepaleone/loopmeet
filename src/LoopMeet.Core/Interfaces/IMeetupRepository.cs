using LoopMeet.Core.Models;

namespace LoopMeet.Core.Interfaces;

public interface IMeetupRepository
{
    Task<Meetup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Meetup>> ListUpcomingByGroupAsync(Guid groupId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(Meetup Meetup, string GroupName)>> ListUpcomingByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Meetup> AddAsync(Meetup meetup, CancellationToken cancellationToken = default);
    Task UpdateAsync(Meetup meetup, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
