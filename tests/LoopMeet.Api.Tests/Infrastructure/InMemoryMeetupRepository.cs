using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Meetups;
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

    public Task<IReadOnlyList<Meetup>> ListUpcomingByGroupAsync(Guid groupId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var results = _store.Meetups
                .Where(meetup => meetup.GroupId == groupId && meetup.ScheduledAt > now)
                .OrderBy(meetup => meetup.ScheduledAt)
                .ToList();
            return Task.FromResult<IReadOnlyList<Meetup>>(results);
        }
    }

    public Task<IReadOnlyList<Meetup>> ListUpcomingByUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var memberGroupIds = _store.Memberships
                .Where(membership => membership.UserId == userId)
                .Select(membership => membership.GroupId)
                .Distinct()
                .ToHashSet();

            var results = _store.Meetups
                .Where(meetup => memberGroupIds.Contains(meetup.GroupId) && meetup.ScheduledAt > now)
                .OrderBy(meetup => meetup.ScheduledAt)
                .ToList();

            return Task.FromResult<IReadOnlyList<Meetup>>(results);
        }
    }

    /// <summary>
    /// Same batch-and-skip algorithm as the Supabase repository: read up to
    /// <paramref name="limit"/> raw rows after the cursor, keep the past ones, and stop
    /// once the page is full or a raw batch comes back short.
    /// </summary>
    public Task<MeetupPage> ListPastByGroupAsync(Guid groupId, DateTimeOffset now, MeetupPageCursor? before, int limit, CancellationToken cancellationToken = default)
    {
        lock (_store.SyncRoot)
        {
            var items = new List<Meetup>(limit);
            var cursor = before;
            while (true)
            {
                var batch = _store.Meetups
                    .Where(meetup => meetup.GroupId == groupId && meetup.ScheduledAt <= now)
                    .Where(meetup => cursor is not { } position
                        || meetup.ScheduledAt < position.ScheduledAt
                        || (meetup.ScheduledAt == position.ScheduledAt && meetup.Id.CompareTo(position.Id) < 0))
                    .OrderByDescending(meetup => meetup.ScheduledAt)
                    .ThenByDescending(meetup => meetup.Id)
                    .Take(limit)
                    .ToList();

                foreach (var meetup in batch)
                {
                    cursor = new MeetupPageCursor(meetup.ScheduledAt, meetup.Id);
                    if (MeetupLifecycle.IsPast(meetup.ScheduledAt, meetup.Timezone, now))
                    {
                        items.Add(meetup);
                        if (items.Count == limit)
                        {
                            return Task.FromResult(new MeetupPage(items, cursor));
                        }
                    }
                }

                if (batch.Count < limit)
                {
                    return Task.FromResult(new MeetupPage(items, null));
                }
            }
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
