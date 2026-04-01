using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;
using LoopMeet.Infrastructure.Supabase.Models;
using Supabase;
using Operator = Supabase.Postgrest.Constants.Operator;

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

    public async Task<IReadOnlyList<Meetup>> ListUpcomingByGroupAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow.ToString("o");
        var response = await _client
            .From<MeetupRecord>()
            .Filter("group_id", Operator.Equals, groupId.ToString())
            .Filter("scheduled_at", Operator.GreaterThan, now)
            .Order("scheduled_at", global::Supabase.Postgrest.Constants.Ordering.Ascending)
            .Get();

        return response.Models
            .Select(Map)
            .ToList();
    }

    public async Task<IReadOnlyList<(Meetup Meetup, string GroupName)>> ListUpcomingByUserAsync(Guid userId, CancellationToken cancellationToken = default)
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
            return Array.Empty<(Meetup, string)>();
        }

        var now = DateTimeOffset.UtcNow.ToString("o");
        var allMeetups = new List<MeetupRecord>();
        foreach (var groupId in groupIds)
        {
            var meetupResponse = await _client
                .From<MeetupRecord>()
                .Filter("group_id", Operator.Equals, groupId.ToString())
                .Filter("scheduled_at", Operator.GreaterThan, now)
                .Order("scheduled_at", global::Supabase.Postgrest.Constants.Ordering.Ascending)
                .Get();

            allMeetups.AddRange(meetupResponse.Models);
        }

        var groupsResponse = await _client.From<GroupRecord>().Get();
        var groupLookup = groupsResponse.Models
            .Where(g => groupIds.Contains(g.Id))
            .ToDictionary(g => g.Id, g => g.Name);

        return allMeetups
            .OrderBy(m => m.ScheduledAt)
            .Select(m =>
            {
                groupLookup.TryGetValue(m.GroupId, out var groupName);
                return (Map(m), groupName ?? string.Empty);
            })
            .ToList();
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
            CreatedAt = meetup.CreatedAt,
            UpdatedAt = meetup.UpdatedAt
        };
    }
}
