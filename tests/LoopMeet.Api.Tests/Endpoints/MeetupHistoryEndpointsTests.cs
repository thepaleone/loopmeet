using System.Net;
using System.Net.Http.Json;
using LoopMeet.Api.Contracts;
using LoopMeet.Api.Tests.Infrastructure;
using LoopMeet.Core.Models;
using Xunit;

namespace LoopMeet.Api.Tests.Endpoints;

/// <summary>
/// Past meetups, the single-meetup read, and the end-of-day cutoff (feature 012).
/// The clock is fixed per test; the 30 s response caches run on the real clock,
/// so a test never re-reads one cache key across a clock change.
/// </summary>
public sealed class MeetupHistoryEndpointsTests
{
    private const string LosAngeles = "America/Los_Angeles";

    // 2026-10-30 13:00 in Los Angeles (PDT, UTC-7).
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 20, 0, 0, TimeSpan.Zero);

    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly InMemoryStore _store;
    private readonly Guid _ownerId = Guid.NewGuid();

    public MeetupHistoryEndpointsTests()
    {
        _store = new InMemoryStore();
        _factory = new TestWebApplicationFactory(_store);
        _factory.Clock.UtcNow = Now;
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", _ownerId.ToString());
        SeedUser(_ownerId, "Olivia Owner");
    }

    // ── US1: GET /groups/{groupId}/meetups/past ──────────────────────────

    [Fact]
    public async Task PastList_ReturnsOnlyPastMeetups_NewestFirst_WithReadModelFields()
    {
        var groupId = SeedGroupWithMembership(_ownerId, "Thursday Crew");
        SeedMeetup(groupId, "Two days ago", Now.AddDays(-2));
        SeedMeetup(groupId, "Ten days ago", Now.AddDays(-10));
        SeedMeetup(groupId, "Next week", Now.AddDays(7));

        var page = await GetPastAsync(groupId);

        Assert.Equal(new[] { "Two days ago", "Ten days ago" }, page.Meetups.Select(m => m.Title));
        Assert.All(page.Meetups, m => Assert.True(m.IsPast));
        Assert.All(page.Meetups, m => Assert.Equal("Thursday Crew", m.GroupName));
        Assert.All(page.Meetups, m => Assert.Equal("Olivia Owner", m.CreatedByDisplayName));
        Assert.All(page.Meetups, m => Assert.Equal(_ownerId, m.GroupOwnerUserId));
        Assert.Null(page.NextCursor);
    }

    [Fact]
    public async Task PastList_DefaultsToTwentyItems()
    {
        var groupId = SeedGroupWithMembership(_ownerId);
        SeedPastSeries(groupId, 25);

        var page = await GetPastAsync(groupId);

        Assert.Equal(20, page.Meetups.Count);
        Assert.NotNull(page.NextCursor);
    }

    [Fact]
    public async Task PastList_FollowingCursorsReturnsEveryMeetupExactlyOnce()
    {
        var groupId = SeedGroupWithMembership(_ownerId);
        var seeded = SeedPastSeries(groupId, 45);

        var seen = await ReadAllPastAsync(groupId);

        Assert.Equal(45, seen.Count);
        Assert.Equal(seeded, seen.Select(m => m.Id).ToList());
    }

    [Fact]
    public async Task PastList_TiedScheduleTimesAcrossPageBoundaryAppearOnce()
    {
        var groupId = SeedGroupWithMembership(_ownerId);
        SeedPastSeries(groupId, 19);
        var tiedAt = Now.AddDays(-30);
        var tieA = SeedMeetup(groupId, "Tie A", tiedAt);
        var tieB = SeedMeetup(groupId, "Tie B", tiedAt);

        var seen = await ReadAllPastAsync(groupId);

        Assert.Equal(21, seen.Count);
        Assert.Single(seen, m => m.Id == tieA);
        Assert.Single(seen, m => m.Id == tieB);
    }

    [Fact]
    public async Task PastList_DeletingASeenMeetupBetweenPagesSkipsNothing()
    {
        var groupId = SeedGroupWithMembership(_ownerId);
        var seeded = SeedPastSeries(groupId, 30);

        var first = await GetPastAsync(groupId);
        var deleteResponse = await _client.DeleteAsync($"/groups/{groupId}/meetups/{first.Meetups[5].Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        var second = await GetPastAsync(groupId, first.NextCursor);

        Assert.Equal(seeded.Skip(20), second.Meetups.Select(m => m.Id));
        Assert.Null(second.NextCursor);
    }

    [Fact]
    public async Task PastList_ExcludesMeetupThatStartedEarlierToday_AndStillPagesOnward()
    {
        var groupId = SeedGroupWithMembership(_ownerId);
        // 09:00 today in Los Angeles: started, but its day has not ended.
        SeedMeetup(groupId, "Started this morning", Now.AddHours(-4));
        var older = SeedPastSeries(groupId, 2);

        var first = await GetPastAsync(groupId, limit: 1);

        Assert.Equal(older[0], Assert.Single(first.Meetups).Id);
        Assert.NotNull(first.NextCursor);
        var rest = await GetPastAsync(groupId, first.NextCursor, limit: 1);
        Assert.Equal(older[1], Assert.Single(rest.Meetups).Id);
    }

    [Theory]
    [InlineData("?cursor=garbage", "invalid_cursor")]
    [InlineData("?limit=0", "invalid_limit")]
    [InlineData("?limit=51", "invalid_limit")]
    public async Task PastList_RejectsInvalidPagingParameters(string query, string expectedCode)
    {
        var groupId = SeedGroupWithMembership(_ownerId);

        var response = await _client.GetAsync($"/groups/{groupId}/meetups/past{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal(expectedCode, error!.Code);
    }

    [Fact]
    public async Task PastList_FirstPageCacheIsInvalidatedByDelete()
    {
        var groupId = SeedGroupWithMembership(_ownerId);
        var ids = SeedPastSeries(groupId, 3);

        await GetPastAsync(groupId);
        var deleteResponse = await _client.DeleteAsync($"/groups/{groupId}/meetups/{ids[0]}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        var after = await GetPastAsync(groupId);

        Assert.DoesNotContain(after.Meetups, m => m.Id == ids[0]);
    }

    // ── US1: GET /groups/{groupId}/meetups/{meetupId} ────────────────────

    [Fact]
    public async Task SingleMeetup_ReturnsPastMeetupWithIsPast()
    {
        var groupId = SeedGroupWithMembership(_ownerId, "Thursday Crew");
        var id = SeedMeetup(groupId, "Last week", Now.AddDays(-7));

        var response = await _client.GetAsync($"/groups/{groupId}/meetups/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var meetup = await response.Content.ReadFromJsonAsync<MeetupListItemResponse>();
        Assert.Equal(id, meetup!.Id);
        Assert.True(meetup.IsPast);
        Assert.Equal("Thursday Crew", meetup.GroupName);
        Assert.Equal(_ownerId, meetup.GroupOwnerUserId);
    }

    [Fact]
    public async Task SingleMeetup_IsNotFoundForAnotherGroupOrUnknownId()
    {
        var groupId = SeedGroupWithMembership(_ownerId);
        var otherGroupId = SeedGroupWithMembership(_ownerId);
        var otherMeetup = SeedMeetup(otherGroupId, "Elsewhere", Now.AddDays(-1));

        var wrongGroup = await _client.GetAsync($"/groups/{groupId}/meetups/{otherMeetup}");
        var unknown = await _client.GetAsync($"/groups/{groupId}/meetups/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, wrongGroup.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }

    // ── US2: PATCH / DELETE of past meetups ──────────────────────────────

    [Fact]
    public async Task Patch_PastMeetupLocationOnly_Succeeds()
    {
        var groupId = SeedGroupWithMembership(_ownerId);
        var id = SeedMeetup(groupId, "Taco night", Now.AddDays(-3));

        var response = await PatchAsync(groupId, id, new UpdateMeetupRequest
        {
            Title = "Taco night",
            PlaceName = "La Taqueria",
            Latitude = 37.75,
            Longitude = -122.41
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        lock (_store.SyncRoot)
        {
            Assert.Equal("La Taqueria", _store.Meetups.Single(m => m.Id == id).PlaceName);
        }
    }

    [Fact]
    public async Task Patch_PastMeetupWithUnchangedTimeResent_Succeeds()
    {
        var groupId = SeedGroupWithMembership(_ownerId);
        var scheduledAt = Now.AddDays(-3);
        var id = SeedMeetup(groupId, "Taco night", scheduledAt);

        var response = await PatchAsync(groupId, id, new UpdateMeetupRequest { Title = "Taco night!", ScheduledAt = scheduledAt });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Patch_PastMeetupMovedToAnotherPastTime_SucceedsAndReorders()
    {
        var groupId = SeedGroupWithMembership(_ownerId);
        var moved = SeedMeetup(groupId, "Moved", Now.AddDays(-2));
        var anchor = SeedMeetup(groupId, "Anchor", Now.AddDays(-5));

        var response = await PatchAsync(groupId, moved, new UpdateMeetupRequest { ScheduledAt = Now.AddDays(-9) });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await GetPastAsync(groupId);
        Assert.Equal(new[] { anchor, moved }, page.Meetups.Select(m => m.Id));
    }

    [Fact]
    public async Task Patch_UpcomingMeetupMovedLater_Succeeds()
    {
        var groupId = SeedGroupWithMembership(_ownerId);
        var id = SeedMeetup(groupId, "Ride", Now.AddDays(3));

        var response = await PatchAsync(groupId, id, new UpdateMeetupRequest { ScheduledAt = Now.AddDays(4) });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Patch_UpcomingMeetupMovedIntoThePast_IsRejected()
    {
        var groupId = SeedGroupWithMembership(_ownerId);
        var id = SeedMeetup(groupId, "Ride", Now.AddDays(3));

        var response = await PatchAsync(groupId, id, new UpdateMeetupRequest { ScheduledAt = Now.AddDays(-1) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("invalid_meetup_schedule", error!.Code);
    }

    [Fact]
    public async Task Delete_PastMeetup_RemovesItFromThePastList()
    {
        var groupId = SeedGroupWithMembership(_ownerId);
        var id = SeedMeetup(groupId, "Old", Now.AddDays(-4));

        var response = await _client.DeleteAsync($"/groups/{groupId}/meetups/{id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var page = await GetPastAsync(groupId);
        Assert.DoesNotContain(page.Meetups, m => m.Id == id);
    }

    // ── US3: moving a past meetup back into the future ───────────────────

    [Fact]
    public async Task Patch_PastMeetupMovedIntoTheFuture_BecomesUpcomingEverywhere()
    {
        var groupId = SeedGroupWithMembership(_ownerId);
        var id = SeedMeetup(groupId, "Repeat it", Now.AddDays(-3));

        var response = await PatchAsync(groupId, id, new UpdateMeetupRequest { ScheduledAt = Now.AddDays(1) });

        // The PATCH invalidated both group caches; the Home cache has not been read
        // in this test, so every list below is computed fresh.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var past = await GetPastAsync(groupId);
        Assert.DoesNotContain(past.Meetups, m => m.Id == id);
        var upcoming = await _client.GetFromJsonAsync<MeetupsResponse>($"/groups/{groupId}/meetups");
        var moved = Assert.Single(upcoming!.Meetups, m => m.Id == id);
        Assert.False(moved.IsPast);
        var home = await _client.GetFromJsonAsync<UpcomingMeetupsResponse>("/meetups/upcoming");
        Assert.Contains(home!.Meetups, m => m.Id == id);
    }

    // ── US4: a meetup stays upcoming until local midnight ─────────────────

    [Theory]
    [InlineData("America/New_York", "2026-10-30T19:00:00-04:00", "2026-10-30T23:59:00-04:00", "2026-10-31T00:01:00-04:00")]
    [InlineData(null, "2026-10-30T19:00:00-07:00", "2026-10-30T23:59:00-07:00", "2026-10-31T00:01:00-07:00")]
    public async Task Cutoff_MeetupIsInExactlyOneListOnEachSideOfLocalMidnight(
        string? timezone, string scheduledAt, string beforeMidnight, string afterMidnight)
    {
        var scheduled = DateTimeOffset.Parse(scheduledAt);

        // A fresh member and group per clock setting, so no 30 s cache spans both.
        _factory.Clock.UtcNow = DateTimeOffset.Parse(beforeMidnight);
        var (beforeClient, beforeGroup, beforeId) = SeedMemberWithMeetup(scheduled, timezone);
        await AssertListedAsUpcomingAsync(beforeClient, beforeGroup, beforeId);

        _factory.Clock.UtcNow = DateTimeOffset.Parse(afterMidnight);
        var (afterClient, afterGroup, afterId) = SeedMemberWithMeetup(scheduled, timezone);
        await AssertListedAsPastAsync(afterClient, afterGroup, afterId);
    }

    [Fact]
    public async Task Patch_StartedTodayWithUnchangedTime_Succeeds()
    {
        var groupId = SeedGroupWithMembership(_ownerId);
        var scheduledAt = Now.AddHours(-4);
        var id = SeedMeetup(groupId, "Started this morning", scheduledAt);

        var response = await PatchAsync(groupId, id, new UpdateMeetupRequest { Title = "Renamed", ScheduledAt = scheduledAt });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Patch_StartedTodayMovedToAnEarlierTime_IsRejected()
    {
        var groupId = SeedGroupWithMembership(_ownerId);
        var id = SeedMeetup(groupId, "Started this morning", Now.AddHours(-4));

        var response = await PatchAsync(groupId, id, new UpdateMeetupRequest { ScheduledAt = Now.AddMinutes(-30) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("invalid_meetup_schedule", error!.Code);
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private (HttpClient Client, Guid GroupId, Guid MeetupId) SeedMemberWithMeetup(DateTimeOffset scheduledAt, string? timezone)
    {
        var memberId = Guid.NewGuid();
        SeedUser(memberId, "Mia Member");
        var groupId = SeedGroupWithMembership(memberId);
        var meetupId = SeedMeetup(groupId, "Dinner", scheduledAt, timezone);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", memberId.ToString());
        return (client, groupId, meetupId);
    }

    private static async Task AssertListedAsUpcomingAsync(HttpClient client, Guid groupId, Guid meetupId)
    {
        var upcoming = await client.GetFromJsonAsync<MeetupsResponse>($"/groups/{groupId}/meetups");
        var home = await client.GetFromJsonAsync<UpcomingMeetupsResponse>("/meetups/upcoming");
        var past = await client.GetFromJsonAsync<PastMeetupsResponse>($"/groups/{groupId}/meetups/past");
        Assert.Contains(upcoming!.Meetups, m => m.Id == meetupId);
        Assert.Contains(home!.Meetups, m => m.Id == meetupId);
        Assert.DoesNotContain(past!.Meetups, m => m.Id == meetupId);
    }

    private static async Task AssertListedAsPastAsync(HttpClient client, Guid groupId, Guid meetupId)
    {
        var upcoming = await client.GetFromJsonAsync<MeetupsResponse>($"/groups/{groupId}/meetups");
        var home = await client.GetFromJsonAsync<UpcomingMeetupsResponse>("/meetups/upcoming");
        var past = await client.GetFromJsonAsync<PastMeetupsResponse>($"/groups/{groupId}/meetups/past");
        Assert.DoesNotContain(upcoming!.Meetups, m => m.Id == meetupId);
        Assert.DoesNotContain(home!.Meetups, m => m.Id == meetupId);
        Assert.Contains(past!.Meetups, m => m.Id == meetupId);
    }


    private Task<HttpResponseMessage> PatchAsync(Guid groupId, Guid meetupId, UpdateMeetupRequest request) =>
        _client.PatchAsJsonAsync($"/groups/{groupId}/meetups/{meetupId}", request);


    private async Task<PastMeetupsResponse> GetPastAsync(Guid groupId, string? cursor = null, int? limit = null)
    {
        var query = new List<string>();
        if (cursor is not null) query.Add($"cursor={Uri.EscapeDataString(cursor)}");
        if (limit is not null) query.Add($"limit={limit}");
        var suffix = query.Count == 0 ? string.Empty : "?" + string.Join("&", query);

        var response = await _client.GetAsync($"/groups/{groupId}/meetups/past{suffix}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<PastMeetupsResponse>())!;
    }

    private async Task<List<MeetupListItemResponse>> ReadAllPastAsync(Guid groupId)
    {
        var all = new List<MeetupListItemResponse>();
        string? cursor = null;
        do
        {
            var page = await GetPastAsync(groupId, cursor);
            all.AddRange(page.Meetups);
            cursor = page.NextCursor;
        }
        while (cursor is not null);
        return all;
    }

    /// <summary>Seeds <paramref name="count"/> past meetups one day apart, newest first; returns their ids in that order.</summary>
    private List<Guid> SeedPastSeries(Guid groupId, int count)
    {
        return Enumerable.Range(1, count)
            .Select(day => SeedMeetup(groupId, $"Past #{day}", Now.AddDays(-day)))
            .ToList();
    }

    private void SeedUser(Guid userId, string displayName)
    {
        lock (_store.SyncRoot)
        {
            _store.Users.Add(new User { Id = userId, DisplayName = displayName, Email = $"{userId:N}@test.com" });
        }
    }

    private Guid SeedGroupWithMembership(Guid userId, string? groupName = null)
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId,
            Name = groupName ?? $"Test Group {Guid.NewGuid():N}",
            CreatedAt = Now,
            UpdatedAt = Now
        };

        lock (_store.SyncRoot)
        {
            _store.Groups.Add(group);
            _store.Memberships.Add(new Membership
            {
                Id = Guid.NewGuid(),
                GroupId = group.Id,
                UserId = userId,
                Role = "owner",
                CreatedAt = Now
            });
        }

        return group.Id;
    }

    private Guid SeedMeetup(Guid groupId, string title, DateTimeOffset scheduledAt, string? timezone = LosAngeles)
    {
        var meetup = new Meetup
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            CreatedByUserId = _ownerId,
            Title = title,
            ScheduledAt = scheduledAt,
            Timezone = timezone,
            CreatedAt = Now,
            UpdatedAt = Now
        };

        lock (_store.SyncRoot)
        {
            _store.Meetups.Add(meetup);
        }

        return meetup.Id;
    }
}
