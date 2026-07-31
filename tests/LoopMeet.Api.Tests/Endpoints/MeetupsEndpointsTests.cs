using System.Net;
using System.Net.Http.Json;
using LoopMeet.Api.Contracts;
using LoopMeet.Api.Tests.Infrastructure;
using LoopMeet.Core.Models;
using Xunit;

namespace LoopMeet.Api.Tests.Endpoints;

public sealed class MeetupsEndpointsTests
{
    private readonly HttpClient _client;
    private readonly InMemoryStore _store;

    public MeetupsEndpointsTests()
    {
        _store = new InMemoryStore();
        var factory = new TestWebApplicationFactory(_store);
        _client = factory.CreateClient();
    }

    // ── T014: POST /groups/{groupId}/meetups ────────────────────────────

    [Fact]
    public async Task CreatesMeetupSuccessfully()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        var groupId = SeedGroupWithMembership(userId);

        var scheduledAt = DateTimeOffset.UtcNow.AddDays(7);
        var response = await _client.PostAsJsonAsync($"/groups/{groupId}/meetups", new CreateMeetupRequest
        {
            Title = "Saturday Ride",
            ScheduledAt = scheduledAt
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<MeetupResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Saturday Ride", payload!.Title);
        Assert.Equal(groupId, payload.GroupId);
        Assert.Equal(scheduledAt, payload.ScheduledAt);
        Assert.Equal(userId, payload.CreatedByUserId);

        lock (_store.SyncRoot)
        {
            var meetup = _store.Meetups.FirstOrDefault(m => m.Id == payload.Id);
            Assert.NotNull(meetup);
            Assert.Equal("Saturday Ride", meetup!.Title);
            Assert.Equal(groupId, meetup.GroupId);
        }
    }

    [Fact]
    public async Task RejectsEmptyTitle()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        var groupId = SeedGroupWithMembership(userId);

        var response = await _client.PostAsJsonAsync($"/groups/{groupId}/meetups", new CreateMeetupRequest
        {
            Title = "",
            ScheduledAt = DateTimeOffset.UtcNow.AddDays(7)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RejectsPastScheduledAt()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        var groupId = SeedGroupWithMembership(userId);

        var response = await _client.PostAsJsonAsync($"/groups/{groupId}/meetups", new CreateMeetupRequest
        {
            Title = "Overdue Meetup",
            ScheduledAt = DateTimeOffset.UtcNow.AddDays(-1)
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ── T020: GET /groups/{groupId}/meetups ──────────────────────────────

    [Fact]
    public async Task ReturnsOnlyFutureMeetups()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        var groupId = SeedGroupWithMembership(userId);

        // Seed one past meetup and two future meetups
        SeedMeetup(groupId, userId, "Past Event", DateTimeOffset.UtcNow.AddDays(-3));
        SeedMeetup(groupId, userId, "Later Event", DateTimeOffset.UtcNow.AddDays(14));
        SeedMeetup(groupId, userId, "Sooner Event", DateTimeOffset.UtcNow.AddDays(3));

        var response = await _client.GetAsync($"/groups/{groupId}/meetups");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<MeetupsResponse>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload!.Meetups.Count);

        // Verify ascending order by ScheduledAt
        Assert.Equal("Sooner Event", payload.Meetups[0].Title);
        Assert.Equal("Later Event", payload.Meetups[1].Title);
    }

    [Fact]
    public async Task ReturnsEmptyWhenNoMeetups()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        var groupId = SeedGroupWithMembership(userId);

        var response = await _client.GetAsync($"/groups/{groupId}/meetups");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<MeetupsResponse>();
        Assert.NotNull(payload);
        Assert.Empty(payload!.Meetups);
    }

    // ── T024: GET /meetups/upcoming ──────────────────────────────────────

    [Fact]
    public async Task UpcomingReturnsMeetupsAcrossAllGroupsWithGroupName()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Email", "user@test.com");

        var group1Id = SeedGroupWithMembership(userId, "Group Alpha");
        var group2Id = SeedGroupWithMembership(userId, "Group Beta");

        SeedMeetup(group1Id, userId, "Meetup A", DateTimeOffset.UtcNow.AddDays(2));
        SeedMeetup(group2Id, userId, "Meetup B", DateTimeOffset.UtcNow.AddDays(1));
        SeedMeetup(group1Id, userId, "Past Meetup", DateTimeOffset.UtcNow.AddDays(-1));

        var response = await _client.GetAsync("/meetups/upcoming");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<UpcomingMeetupsResponse>();
        Assert.NotNull(payload);
        Assert.Equal(2, payload!.Meetups.Count);
        Assert.Equal("Meetup B", payload.Meetups[0].Title); // soonest first
        Assert.Equal("Meetup A", payload.Meetups[1].Title);
        Assert.Equal("Group Beta", payload.Meetups[0].GroupName);
        Assert.Equal("Group Alpha", payload.Meetups[1].GroupName);
    }

    // ── T028: PATCH /groups/{groupId}/meetups/{meetupId} ──────────────

    [Fact]
    public async Task UpdatesMeetupSuccessfully()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        var groupId = SeedGroupWithMembership(userId);
        var meetupId = SeedMeetup(groupId, userId, "Original", DateTimeOffset.UtcNow.AddDays(5));

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/groups/{groupId}/meetups/{meetupId}")
        {
            Content = JsonContent.Create(new UpdateMeetupRequest
            {
                Title = "Updated Title"
            })
        };

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<MeetupResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Updated Title", payload!.Title);
    }

    [Fact]
    public async Task UpdateReturns404ForNonExistentMeetup()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        var groupId = SeedGroupWithMembership(userId);

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/groups/{groupId}/meetups/{Guid.NewGuid()}")
        {
            Content = JsonContent.Create(new UpdateMeetupRequest { Title = "Nope" })
        };

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── T041: DELETE /groups/{groupId}/meetups/{meetupId} ────────────────

    [Fact]
    public async Task DeletesMeetupSuccessfully()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        var groupId = SeedGroupWithMembership(userId);
        var meetupId = SeedMeetup(groupId, userId, "To Delete", DateTimeOffset.UtcNow.AddDays(5));

        var response = await _client.DeleteAsync($"/groups/{groupId}/meetups/{meetupId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await _client.GetAsync($"/groups/{groupId}/meetups");
        var payload = await getResponse.Content.ReadFromJsonAsync<MeetupsResponse>();
        Assert.NotNull(payload);
        Assert.Empty(payload!.Meetups);
    }

    [Fact]
    public async Task DeleteReturns404ForNonExistentMeetup()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        var groupId = SeedGroupWithMembership(userId);

        var response = await _client.DeleteAsync($"/groups/{groupId}/meetups/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ── 011: read-model resolution (organizer, group name, group owner) ───

    [Fact]
    public async Task GroupMeetupsResolveOrganizerDisplayNameAndGroupName()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        SeedUser(userId, "Ada Lovelace");
        var groupId = SeedGroupWithMembership(userId, "Climbing Club");
        SeedMeetup(groupId, userId, "Boulder Night", DateTimeOffset.UtcNow.AddDays(3));

        var response = await _client.GetAsync($"/groups/{groupId}/meetups");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<MeetupsResponse>();
        var meetup = Assert.Single(payload!.Meetups);
        Assert.Equal("Ada Lovelace", meetup.CreatedByDisplayName);
        Assert.Equal("Climbing Club", meetup.GroupName);
        Assert.Equal(userId, meetup.GroupOwnerUserId);
    }

    [Fact]
    public async Task GroupMeetupsLeaveOrganizerEmptyWhenCreatorIsNoLongerAMember()
    {
        var ownerId = Guid.NewGuid();
        var departedId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", ownerId.ToString());

        SeedUser(ownerId, "Ada Lovelace");
        SeedUser(departedId, "Grace Hopper");
        var groupId = SeedGroupWithMembership(ownerId, "Climbing Club");
        // Grace created the meetup but holds no membership row for this group.
        SeedMeetup(groupId, departedId, "Boulder Night", DateTimeOffset.UtcNow.AddDays(3));

        var response = await _client.GetAsync($"/groups/{groupId}/meetups");

        var payload = await response.Content.ReadFromJsonAsync<MeetupsResponse>();
        var meetup = Assert.Single(payload!.Meetups);
        Assert.Equal(string.Empty, meetup.CreatedByDisplayName);
        Assert.Equal(departedId, meetup.CreatedByUserId);
        Assert.Equal("Climbing Club", meetup.GroupName);
    }

    [Fact]
    public async Task GroupMeetupsLeaveOrganizerEmptyWhenCreatorHasNoProfile()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        // Member row exists, profile row does not.
        var groupId = SeedGroupWithMembership(userId, "Climbing Club");
        SeedMeetup(groupId, userId, "Boulder Night", DateTimeOffset.UtcNow.AddDays(3));

        var response = await _client.GetAsync($"/groups/{groupId}/meetups");

        var payload = await response.Content.ReadFromJsonAsync<MeetupsResponse>();
        var meetup = Assert.Single(payload!.Meetups);
        Assert.Equal(string.Empty, meetup.CreatedByDisplayName);
    }

    [Fact]
    public async Task UpcomingMeetupsResolveOrganizerAndGroupOwnerPerGroup()
    {
        var viewerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", viewerId.ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Email", "viewer@test.com");

        SeedUser(viewerId, "Ada Lovelace");
        SeedUser(otherOwnerId, "Grace Hopper");

        var ownedGroupId = SeedGroupWithMembership(viewerId, "Owned Group");
        // A group the viewer belongs to but does not own; owner also created the meetup.
        var joinedGroupId = SeedGroupWithMembership(otherOwnerId, "Joined Group");
        SeedMembership(joinedGroupId, viewerId, "member");

        SeedMeetup(ownedGroupId, viewerId, "Mine", DateTimeOffset.UtcNow.AddDays(1));
        SeedMeetup(joinedGroupId, otherOwnerId, "Theirs", DateTimeOffset.UtcNow.AddDays(2));

        var response = await _client.GetAsync("/meetups/upcoming");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<UpcomingMeetupsResponse>();
        Assert.Equal(2, payload!.Meetups.Count);

        var mine = payload.Meetups.Single(item => item.Title == "Mine");
        Assert.Equal("Ada Lovelace", mine.CreatedByDisplayName);
        Assert.Equal("Owned Group", mine.GroupName);
        Assert.Equal(viewerId, mine.GroupOwnerUserId);

        var theirs = payload.Meetups.Single(item => item.Title == "Theirs");
        Assert.Equal("Grace Hopper", theirs.CreatedByDisplayName);
        Assert.Equal("Joined Group", theirs.GroupName);
        // The gate the client uses to decide whether to offer editing.
        Assert.Equal(otherOwnerId, theirs.GroupOwnerUserId);
        Assert.NotEqual(viewerId, theirs.GroupOwnerUserId);
    }

    [Fact]
    public void MeetupQueryServiceLogsOrganizerResolutionCounts()
    {
        // Constitution VII: an unexpected organizer-fallback rate must be visible
        // in logs rather than silent. Asserted on source because the log call
        // itself is not observable through the endpoint.
        var servicePath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/LoopMeet.Api/Services/Meetups/MeetupQueryService.cs"));
        var source = File.ReadAllText(servicePath);

        Assert.Equal(2, CountOccurrences(source, "organizersResolved="));
        Assert.Equal(2, CountOccurrences(source, "organizersUnresolved="));
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private void SeedUser(Guid userId, string displayName)
    {
        lock (_store.SyncRoot)
        {
            _store.Users.Add(new User
            {
                Id = userId,
                DisplayName = displayName,
                Email = $"{userId:N}@test.com"
            });
        }
    }

    private void SeedMembership(Guid groupId, Guid userId, string role)
    {
        lock (_store.SyncRoot)
        {
            _store.Memberships.Add(new Membership
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                UserId = userId,
                Role = role,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
    }

    private Guid SeedGroupWithMembership(Guid userId, string? groupName = null)
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId,
            Name = groupName ?? $"Test Group {Guid.NewGuid():N}",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var membership = new Membership
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId = userId,
            Role = "owner",
            CreatedAt = DateTimeOffset.UtcNow
        };

        lock (_store.SyncRoot)
        {
            _store.Groups.Add(group);
            _store.Memberships.Add(membership);
        }

        return group.Id;
    }

    private Guid SeedMeetup(Guid groupId, Guid userId, string title, DateTimeOffset scheduledAt)
    {
        var meetup = new Meetup
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            CreatedByUserId = userId,
            Title = title,
            ScheduledAt = scheduledAt,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        lock (_store.SyncRoot)
        {
            _store.Meetups.Add(meetup);
        }

        return meetup.Id;
    }
}
