using System.Net;
using System.Net.Http.Json;
using LoopMeet.Api.Contracts;
using LoopMeet.Api.Tests.Infrastructure;
using LoopMeet.Core.Models;
using Xunit;

namespace LoopMeet.Api.Tests.Endpoints;

public sealed class GroupsWriteEndpointsTests
{
    private readonly HttpClient _client;
    private readonly InMemoryStore _store;

    public GroupsWriteEndpointsTests()
    {
        _store = new InMemoryStore();
        var factory = new TestWebApplicationFactory(_store);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreatesGroupAndOwnerMembership()
    {
        var ownerId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", ownerId.ToString());

        var response = await _client.PostAsJsonAsync("/groups", new CreateGroupRequest
        {
            Name = "Weekenders"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<GroupSummaryResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Weekenders", payload!.Name);
        Assert.Equal(ownerId, payload.OwnerUserId);

        lock (_store.SyncRoot)
        {
            var group = _store.Groups.FirstOrDefault(group => group.Id == payload.Id);
            Assert.NotNull(group);

            var membership = _store.Memberships
                .FirstOrDefault(member => member.GroupId == payload.Id && member.UserId == ownerId);
            Assert.NotNull(membership);
            Assert.Equal("owner", membership!.Role);
        }
    }

    [Fact]
    public async Task RejectsDuplicateNameOnCreate()
    {
        var ownerId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", ownerId.ToString());

        await SeedGroupAsync(ownerId, "Crew");

        var response = await _client.PostAsJsonAsync("/groups", new CreateGroupRequest
        {
            Name = "Crew"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RenamesGroup()
    {
        var ownerId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", ownerId.ToString());

        var groupId = await SeedGroupAsync(ownerId, "Original");

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/groups/{groupId}")
        {
            Content = JsonContent.Create(new UpdateGroupRequest
            {
                Name = "Updated"
            })
        };

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<GroupSummaryResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Updated", payload!.Name);

        lock (_store.SyncRoot)
        {
            var group = _store.Groups.FirstOrDefault(candidate => candidate.Id == groupId);
            Assert.NotNull(group);
            Assert.Equal("Updated", group!.Name);
        }
    }

    [Fact]
    public async Task RejectsDuplicateNameOnRename()
    {
        var ownerId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", ownerId.ToString());

        var firstGroupId = await SeedGroupAsync(ownerId, "Morning Crew");
        await SeedGroupAsync(ownerId, "Evening Crew");

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/groups/{firstGroupId}")
        {
            Content = JsonContent.Create(new UpdateGroupRequest
            {
                Name = "Evening Crew"
            })
        };

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<Guid> SeedGroupAsync(Guid ownerId, string name)
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerId,
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        lock (_store.SyncRoot)
        {
            _store.Groups.Add(group);
        }

        return group.Id;
    }
}
