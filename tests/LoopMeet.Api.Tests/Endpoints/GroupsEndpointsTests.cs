using System.Net;
using LoopMeet.Api.Tests.Infrastructure;
using LoopMeet.Core.Models;
using Xunit;
using System.Net.Http.Json;

namespace LoopMeet.Api.Tests.Endpoints;

public sealed class GroupsEndpointsTests
{
    private readonly HttpClient _client;
    private readonly InMemoryStore _store;

    public GroupsEndpointsTests()
    {
        _store = new InMemoryStore();
        var factory = new TestWebApplicationFactory(_store);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ReturnsOwnedFirstThenMemberSortedByName()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Email", "owner@example.com");

        await SeedAsync(userId);

        var response = await _client.GetAsync("/groups");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<GroupsResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Alpha", payload!.Owned[0].Name);
        Assert.Equal("Beta", payload.Member[0].Name);
        Assert.Single(payload.PendingInvitations);
    }

    private async Task SeedAsync(Guid userId)
    {
        var ownedGroup = new Group
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId,
            Name = "Alpha",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var memberGroup = new Group
        {
            Id = Guid.NewGuid(),
            OwnerUserId = Guid.NewGuid(),
            Name = "Beta",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        lock (_store.SyncRoot)
        {
            _store.Groups.AddRange([ownedGroup, memberGroup]);
            _store.Memberships.Add(new Membership
            {
                Id = Guid.NewGuid(),
                GroupId = memberGroup.Id,
                UserId = userId,
                Role = "member",
                CreatedAt = DateTimeOffset.UtcNow
            });
            _store.Invitations.Add(new Invitation
            {
                Id = Guid.NewGuid(),
                GroupId = ownedGroup.Id,
                InvitedEmail = "owner@example.com",
                Status = "pending",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await Task.CompletedTask;
    }

    private sealed class GroupsResponse
    {
        public List<GroupSummary> Owned { get; set; } = new();
        public List<GroupSummary> Member { get; set; } = new();
        public List<InvitationSummary> PendingInvitations { get; set; } = new();
    }

    private sealed class GroupSummary
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class InvitationSummary
    {
        public string InvitedEmail { get; set; } = string.Empty;
    }
}
