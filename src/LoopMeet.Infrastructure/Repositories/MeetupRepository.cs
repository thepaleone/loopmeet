using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Meetups;
using LoopMeet.Core.Models;
using LoopMeet.Infrastructure.Supabase.Models;
using Supabase;
using Supabase.Postgrest.Interfaces;
using Operator = Supabase.Postgrest.Constants.Operator;
using QueryFilter = Supabase.Postgrest.QueryFilter;

namespace LoopMeet.Infrastructure.Repositories;

public sealed class MeetupRepository : IMeetupRepository
{
    private readonly Client _client;

    public MeetupRepository(Client client)
    {
        _client = client;
    }

    public async Task<Meetup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _client
            .From<MeetupRecord>()
            .Filter("id", Operator.Equals, id.ToString())
            .Get();

        var record = response.Models.FirstOrDefault();
        return record is null ? null : Map(record);
    }

    public async Task<IReadOnlyList<Meetup>> ListUpcomingByGroupAsync(Guid groupId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var response = await _client
            .From<MeetupRecord>()
            .Filter("group_id", Operator.Equals, groupId.ToString())
            .Filter("scheduled_at", Operator.GreaterThan, (now - MeetupLifecycle.ClassificationBand).ToString("o"))
            .Order("scheduled_at", global::Supabase.Postgrest.Constants.Ordering.Ascending)
            .Get();

        return response.Models
            .Select(Map)
            .Where(meetup => !MeetupLifecycle.IsPast(meetup.ScheduledAt, meetup.Timezone, now))
            .ToList();
    }

    public async Task<IReadOnlyList<Meetup>> ListUpcomingByUserAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default)
    {
        var membershipResponse = await _client
            .From<MembershipRecord>()
            .Filter("member_user_id", Operator.Equals, userId.ToString())
            .Get();

        var groupIds = membershipResponse.Models
            .Select(m => m.GroupId)
            .Distinct()
            .ToList();

        if (groupIds.Count == 0)
        {
            return Array.Empty<Meetup>();
        }

        var response = await _client
            .From<MeetupRecord>()
            .Filter("group_id", Operator.In, groupIds.Select(id => id.ToString()).ToList())
            .Filter("scheduled_at", Operator.GreaterThan, (now - MeetupLifecycle.ClassificationBand).ToString("o"))
            .Order("scheduled_at", global::Supabase.Postgrest.Constants.Ordering.Ascending)
            .Get();

        return response.Models
            .Select(Map)
            .Where(meetup => !MeetupLifecycle.IsPast(meetup.ScheduledAt, meetup.Timezone, now))
            .ToList();
    }

    /// <summary>
    /// Newest-first keyset page of past meetups. Rows inside the classification band
    /// that are still upcoming are skipped (not counted), so batches are read until
    /// <paramref name="limit"/> past rows are found or the table is exhausted.
    /// </summary>
    public async Task<MeetupPage> ListPastByGroupAsync(Guid groupId, DateTimeOffset now, MeetupPageCursor? before, int limit, CancellationToken cancellationToken = default)
    {
        var items = new List<Meetup>(limit);
        var cursor = before;
        while (true)
        {
            var query = _client
                .From<MeetupRecord>()
                .Filter("group_id", Operator.Equals, groupId.ToString())
                .Filter("scheduled_at", Operator.LessThanOrEqual, now.UtcDateTime.ToString("o"));

            if (cursor is { } position)
            {
                var at = position.ScheduledAt.UtcDateTime.ToString("o");
                query = query.Or(new List<IPostgrestQueryFilter>
                {
                    new QueryFilter("scheduled_at", Operator.LessThan, at),
                    new QueryFilter(Operator.And, new List<IPostgrestQueryFilter>
                    {
                        new QueryFilter("scheduled_at", Operator.Equals, at),
                        new QueryFilter("id", Operator.LessThan, position.Id.ToString())
                    })
                });
            }

            var response = await query
                .Order("scheduled_at", global::Supabase.Postgrest.Constants.Ordering.Descending)
                .Order("id", global::Supabase.Postgrest.Constants.Ordering.Descending)
                .Limit(limit)
                .Get();

            var batch = response.Models.Select(Map).ToList();
            foreach (var meetup in batch)
            {
                cursor = new MeetupPageCursor(meetup.ScheduledAt, meetup.Id);
                if (MeetupLifecycle.IsPast(meetup.ScheduledAt, meetup.Timezone, now))
                {
                    items.Add(meetup);
                    if (items.Count == limit)
                    {
                        return new MeetupPage(items, cursor);
                    }
                }
            }

            if (batch.Count < limit)
            {
                return new MeetupPage(items, null);
            }
        }
    }

    public async Task<Meetup> AddAsync(Meetup meetup, CancellationToken cancellationToken = default)
    {
        var record = Map(meetup);
        var response = await _client.From<MeetupRecord>().Insert(record);
        return Map(response.Models.First());
    }

    public async Task UpdateAsync(Meetup meetup, CancellationToken cancellationToken = default)
    {
        var record = Map(meetup);
        await _client.From<MeetupRecord>().Update(record);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _client
            .From<MeetupRecord>()
            .Filter("id", Operator.Equals, id.ToString())
            .Delete();
    }

    private static Meetup Map(MeetupRecord record)
    {
        return new Meetup
        {
            Id = record.Id,
            GroupId = record.GroupId,
            CreatedByUserId = record.CreatedByUserId,
            Title = record.Title,
            ScheduledAt = record.ScheduledAt,
            PlaceName = record.PlaceName,
            PlaceAddress = record.PlaceAddress,
            Latitude = record.Latitude,
            Longitude = record.Longitude,
            PlaceId = record.PlaceId,
            Timezone = record.Timezone,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }

    private static MeetupRecord Map(Meetup meetup)
    {
        return new MeetupRecord
        {
            Id = meetup.Id,
            GroupId = meetup.GroupId,
            CreatedByUserId = meetup.CreatedByUserId,
            Title = meetup.Title,
            ScheduledAt = meetup.ScheduledAt,
            PlaceName = meetup.PlaceName,
            PlaceAddress = meetup.PlaceAddress,
            Latitude = meetup.Latitude,
            Longitude = meetup.Longitude,
            PlaceId = meetup.PlaceId,
            Timezone = meetup.Timezone,
            CreatedAt = meetup.CreatedAt,
            UpdatedAt = meetup.UpdatedAt
        };
    }
}
