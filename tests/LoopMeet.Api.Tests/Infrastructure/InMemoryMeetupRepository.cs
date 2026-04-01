using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;

namespace LoopMeet.Api.Tests.Infrastructure;

public sealed class InMemoryMeetupRepository : IMeetupRepository
{
    private readonly InMemoryStore _store;

    public InMemoryMeetupRepository(InMemoryStore store)
    {
        _store = store;
    }

    public Task<Meetup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            return Task.FromResult(_store.Meetups.FirstOrDefault(meetup => meetup.Id == id));
        }
    }

    public Task<IReadOnlyList<Meetup>> ListUpcomingByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var results = _store.Meetups
                .Where(meetup => meetup.GroupId == groupId && meetup.ScheduledAt > DateTimeOffset.UtcNow)
                .OrderBy(meetup => meetup.ScheduledAt)
                .ToList();
            return Task.FromResult<IReadOnlyList<Meetup>>(results);
        }
    }

    public Task<IReadOnlyList<(Meetup Meetup, string GroupName)>> ListUpcomingByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var memberGroupIds = _store.Memberships
                .Where(membership => membership.UserId == userId)
                .Select(membership => membership.GroupId)
                .Distinct()
                .ToHashSet();

            var results = _store.Meetups
                .Where(meetup => memberGroupIds.Contains(meetup.GroupId) && meetup.ScheduledAt > DateTimeOffset.UtcNow)
                .OrderBy(meetup => meetup.ScheduledAt)
                .Select(meetup =>
                {
                    var groupName = _store.Groups
                        .Where(group => group.Id == meetup.GroupId)
                        .Select(group => group.Name)
                        .FirstOrDefault() ?? string.Empty;
                    return (meetup, groupName);
                })
                .ToList();

            return Task.FromResult<IReadOnlyList<(Meetup Meetup, string GroupName)>>(results);
        }
    }

    public Task<Meetup> AddAsync(Meetup meetup, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            _store.Meetups.Add(meetup);
        }

        return Task.FromResult(meetup);
    }

    public Task UpdateAsync(Meetup meetup, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var index = _store.Meetups.FindIndex(candidate => candidate.Id == meetup.Id);
            if (index >= 0)
            {
                _store.Meetups[index] = meetup;
            }
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            _store.Meetups.RemoveAll(meetup => meetup.Id == id);
        }

        return Task.CompletedTask;
    }
}
