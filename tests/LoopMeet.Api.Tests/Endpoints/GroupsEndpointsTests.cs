using System.Net;
using System.Net.Http.Json;
using LoopMeet.Api.Contracts;
using LoopMeet.Api.Tests.Infrastructure;
using LoopMeet.Core.Models;
using Xunit;

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
        Assert.Equal(2, payload.Owned[0].MemberCount);
        Assert.Equal("Beta", payload.Member[0].Name);
        Assert.Equal(2, payload.Member[0].MemberCount);
        Assert.Single(payload.PendingInvitations);
        Assert.Equal("Alpha", payload.PendingInvitations[0].GroupName);
        Assert.Equal("Owner Name", payload.PendingInvitations[0].OwnerName);
        Assert.Equal("Owner Name", payload.PendingInvitations[0].SenderName);
    }

    [Fact]
    public async Task ReturnsGroupDetailWithMemberCountAndSortedMembers()
    {
        var currentUserId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", currentUserId.ToString());

        lock (_store.SyncRoot)
        {
            _store.Groups.Add(new Group
            {
                Id = groupId,
                OwnerUserId = ownerId,
                Name = "Weekend Crew",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

            _store.Users.AddRange(
            [
                new User
                {
                    Id = currentUserId,
                    DisplayName = "Zoe",
                    Email = "zoe@example.com",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new User
                {
                    Id = ownerId,
                    DisplayName = "Ava",
                    Email = "ava@example.com",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                }
            ]);

            _store.Memberships.AddRange(
            [
                new Membership
                {
                    Id = Guid.NewGuid(),
                    GroupId = groupId,
                    UserId = currentUserId,
                    Role = "member",
                    CreatedAt = DateTimeOffset.UtcNow
                },
                new Membership
                {
                    Id = Guid.NewGuid(),
                    GroupId = groupId,
                    UserId = ownerId,
                    Role = "owner",
                    CreatedAt = DateTimeOffset.UtcNow
                }
            ]);
        }

        var response = await _client.GetAsync($"/groups/{groupId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<GroupDetailResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Weekend Crew", payload!.Name);
        Assert.Equal(2, payload.MemberCount);
        Assert.Equal(["Ava", "Zoe"], payload.Members.Select(member => member.DisplayName).ToArray());
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
            _store.Users.AddRange(
            [
                new User
                {
                    Id = userId,
                    DisplayName = "Owner Name",
                    Email = "owner@example.com",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new User
                {
                    Id = memberGroup.OwnerUserId,
                    DisplayName = "Beta Owner",
                    Email = "beta-owner@example.com",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                }
            ]);

            _store.Groups.AddRange([ownedGroup, memberGroup]);
            _store.Memberships.Add(new Membership
            {
                Id = Guid.NewGuid(),
                GroupId = ownedGroup.Id,
                UserId = userId,
                Role = "owner",
                CreatedAt = DateTimeOffset.UtcNow
            });
            _store.Memberships.Add(new Membership
            {
                Id = Guid.NewGuid(),
                GroupId = ownedGroup.Id,
                UserId = Guid.NewGuid(),
                Role = "member",
                CreatedAt = DateTimeOffset.UtcNow
            });
            _store.Memberships.Add(new Membership
            {
                Id = Guid.NewGuid(),
                GroupId = memberGroup.Id,
                UserId = userId,
                Role = "member",
                CreatedAt = DateTimeOffset.UtcNow
            });
            _store.Memberships.Add(new Membership
            {
                Id = Guid.NewGuid(),
                GroupId = memberGroup.Id,
                UserId = memberGroup.OwnerUserId,
                Role = "owner",
                CreatedAt = DateTimeOffset.UtcNow
            });
            _store.Invitations.Add(new Invitation
            {
                Id = Guid.NewGuid(),
                GroupId = ownedGroup.Id,
                InvitedByUserId = userId,
                InvitedEmail = "owner@example.com",
                Status = "pending",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await Task.CompletedTask;
    }
}
