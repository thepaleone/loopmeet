using System.Net;
using System.Net.Http.Json;
using LoopMeet.Api.Contracts;
using LoopMeet.Api.Tests.Infrastructure;
using LoopMeet.Core.Models;
using Serilog.Events;
using Xunit;

namespace LoopMeet.Api.Tests.Endpoints;

[Collection(CapturedLogsCollection.Name)]
public sealed class GroupMembershipEndpointsTests
{
    private readonly InMemoryStore _store = new();
    private readonly TestWebApplicationFactory _factory;
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _memberId = Guid.NewGuid();
    private readonly Guid _otherMemberId = Guid.NewGuid();
    private readonly Guid _groupId;

    public GroupMembershipEndpointsTests()
    {
        _factory = new TestWebApplicationFactory(_store);
        _groupId = SeedGroup(_ownerId, _memberId, _otherMemberId);
    }

    [Fact]
    public async Task AMemberLeavesTheGroup()
    {
        var response = await ClientFor(_memberId).PostAsync($"/groups/{_groupId}/leave", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        lock (_store.SyncRoot)
        {
            Assert.DoesNotContain(_store.Memberships, m => m.GroupId == _groupId && m.UserId == _memberId);
            Assert.Equal(2, _store.Memberships.Count(m => m.GroupId == _groupId));
        }
    }

    [Fact]
    public async Task LeavingRefreshesTheLeaversGroupsAndTheOwnersMemberList()
    {
        var member = ClientFor(_memberId);
        var owner = ClientFor(_ownerId);

        // Warm the 30-second caches first, so the assertions below prove they were invalidated.
        var before = await member.GetFromJsonAsync<GroupsResponse>("/groups");
        Assert.Contains(before!.Member, group => group.Id == _groupId);
        var detailBefore = await owner.GetFromJsonAsync<GroupDetailResponse>($"/groups/{_groupId}");
        Assert.Contains(detailBefore!.Members, m => m.UserId == _memberId);
        var ownersListBefore = await owner.GetFromJsonAsync<GroupsResponse>("/groups");
        Assert.Equal(3, ownersListBefore!.Owned.Single().MemberCount);

        await member.PostAsync($"/groups/{_groupId}/leave", null);

        var after = await member.GetFromJsonAsync<GroupsResponse>("/groups");
        Assert.DoesNotContain(after!.Member, group => group.Id == _groupId);
        var detailAfter = await owner.GetFromJsonAsync<GroupDetailResponse>($"/groups/{_groupId}");
        Assert.DoesNotContain(detailAfter!.Members, m => m.UserId == _memberId);
        var ownersListAfter = await owner.GetFromJsonAsync<GroupsResponse>("/groups");
        Assert.Equal(2, ownersListAfter!.Owned.Single().MemberCount);
    }

    [Fact]
    public async Task TheOwnerCannotLeave()
    {
        var response = await ClientFor(_ownerId).PostAsync($"/groups/{_groupId}/leave", null);

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "owner_cannot_leave",
            "Group owners can't leave their own group.");
    }

    [Fact]
    public async Task SomeoneOutsideTheGroupCannotLeaveIt()
    {
        var response = await ClientFor(Guid.NewGuid()).PostAsync($"/groups/{_groupId}/leave", null);

        await AssertErrorAsync(response, HttpStatusCode.NotFound, "not_group_member",
            "You're not a member of this group.");
    }

    [Fact]
    public async Task LeavingTwiceIsRefusedAsNotAMember()
    {
        var member = ClientFor(_memberId);
        await member.PostAsync($"/groups/{_groupId}/leave", null);

        var second = await member.PostAsync($"/groups/{_groupId}/leave", null);

        await AssertErrorAsync(second, HttpStatusCode.NotFound, "not_group_member",
            "You're not a member of this group.");
    }

    [Fact]
    public async Task AnUnauthenticatedCallerIsRejected()
    {
        var response = await _factory.CreateClient().PostAsync($"/groups/{_groupId}/leave", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TheGroupLeftLogCarriesIdsOnly()
    {
        await ClientFor(_memberId).PostAsync($"/groups/{_groupId}/leave", null);

        var left = Assert.Single(_factory.Logs.Events, e => e.MessageTemplate.Text.StartsWith("GroupLeft", StringComparison.Ordinal));
        Assert.Equal(_memberId.ToString(), ScalarText(left.Properties["UserId"]));
        Assert.Equal(_groupId.ToString(), ScalarText(left.Properties["GroupId"]));
        Assert.DoesNotContain(left.Properties.Values, value => ScalarText(value) == "Book club");
    }

    private static string? ScalarText(LogEventPropertyValue value) => (value as ScalarValue)?.Value?.ToString();

    private HttpClient ClientFor(Guid userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        return client;
    }

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code, string message)
    {
        Assert.Equal(status, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal(code, error!.Code);
        Assert.Equal(message, error.Message);
    }

    private Guid SeedGroup(Guid ownerId, params Guid[] memberIds)
    {
        var groupId = Guid.NewGuid();
        lock (_store.SyncRoot)
        {
            _store.Groups.Add(new Group { Id = groupId, OwnerUserId = ownerId, Name = "Book club" });
            _store.Memberships.Add(new Membership { Id = Guid.NewGuid(), GroupId = groupId, UserId = ownerId, Role = "owner" });
            foreach (var memberId in memberIds)
            {
                _store.Memberships.Add(new Membership { Id = Guid.NewGuid(), GroupId = groupId, UserId = memberId, Role = "member" });
            }
        }

        return groupId;
    }
}
