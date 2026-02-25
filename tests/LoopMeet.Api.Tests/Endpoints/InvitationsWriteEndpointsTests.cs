using System.Net;
using System.Net.Http.Json;
using LoopMeet.Api.Contracts;
using LoopMeet.Api.Tests.Infrastructure;
using LoopMeet.Core.Models;
using Xunit;

namespace LoopMeet.Api.Tests.Endpoints;

public sealed class InvitationsWriteEndpointsTests
{
    private readonly HttpClient _client;
    private readonly InMemoryStore _store;

    public InvitationsWriteEndpointsTests()
    {
        _store = new InMemoryStore();
        var factory = new TestWebApplicationFactory(_store);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreatesInvitationForOwner()
    {
        var ownerId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", ownerId.ToString());

        SeedGroup(ownerId, groupId);

        var response = await _client.PostAsJsonAsync($"/groups/{groupId}/invitations", new CreateInvitationRequest
        {
            Email = "invitee@example.com"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        lock (_store.SyncRoot)
        {
            Assert.Single(_store.Invitations);
            Assert.Equal(groupId, _store.Invitations[0].GroupId);
        }
    }

    [Fact]
    public async Task AcceptsInvitationAndAddsMembership()
    {
        var userId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var invitationId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Email", "invitee@example.com");

        lock (_store.SyncRoot)
        {
            _store.Groups.Add(new Group
            {
                Id = groupId,
                OwnerUserId = Guid.NewGuid(),
                Name = "Group",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            _store.Invitations.Add(new Invitation
            {
                Id = invitationId,
                GroupId = groupId,
                InvitedEmail = "invitee@example.com",
                Status = "pending",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        var response = await _client.PostAsync($"/invitations/{invitationId}/accept", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        lock (_store.SyncRoot)
        {
            Assert.Single(_store.Memberships);
            Assert.Equal(userId, _store.Memberships[0].UserId);
            Assert.Equal(groupId, _store.Memberships[0].GroupId);
            Assert.Equal("accepted", _store.Invitations[0].Status);
        }
    }

    private void SeedGroup(Guid ownerId, Guid groupId)
    {
        lock (_store.SyncRoot)
        {
            _store.Groups.Add(new Group
            {
                Id = groupId,
                OwnerUserId = ownerId,
                Name = "Group",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
    }
}
