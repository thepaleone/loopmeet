using System.Net;
using System.Net.Http.Json;
using LoopMeet.Api.Contracts;
using LoopMeet.Api.Tests.Infrastructure;
using LoopMeet.Core.Models;
using Xunit;

namespace LoopMeet.Api.Tests.Endpoints;

public sealed class InvitationsEndpointsTests
{
    private readonly HttpClient _client;
    private readonly InMemoryStore _store;

    public InvitationsEndpointsTests()
    {
        _store = new InMemoryStore();
        var factory = new TestWebApplicationFactory(_store);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ReturnsPendingInvitationsWithResolvedOwnerAndSenderMetadata()
    {
        var userId = Guid.NewGuid();
        var email = "invitee@example.com";
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Email", email);

        var ownerId = Guid.NewGuid();
        var senderId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddMinutes(-10);

        SeedUser(ownerId, "Owner Name", "owner@example.com");
        SeedUser(senderId, "Sender Name", "sender@example.com");
        SeedGroup(groupId, ownerId, "Trip Crew");
        SeedInvitation(groupId, email, senderId, createdAt);

        var response = await _client.GetAsync("/invitations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<InvitationsResponse>();
        Assert.NotNull(payload);
        Assert.Single(payload!.Invitations);

        var invitation = payload.Invitations[0];
        Assert.Equal(email, invitation.InvitedEmail);
        Assert.Equal("Trip Crew", invitation.GroupName);
        Assert.Equal("Owner Name", invitation.OwnerName);
        Assert.Equal("owner@example.com", invitation.OwnerEmail);
        Assert.Equal("Sender Name", invitation.SenderName);
        Assert.Equal("sender@example.com", invitation.SenderEmail);
        Assert.Equal(createdAt, invitation.CreatedAt);
    }

    [Fact]
    public async Task FallsBackSenderMetadataToOwnerForLegacyInvitations()
    {
        var userId = Guid.NewGuid();
        var email = "invitee@example.com";
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Email", email);

        var ownerId = Guid.NewGuid();
        var groupId = Guid.NewGuid();
        SeedUser(ownerId, "Owner Name", "owner@example.com");
        SeedGroup(groupId, ownerId, "Legacy Group");
        SeedInvitation(groupId, email, invitedByUserId: null, createdAt: DateTimeOffset.UtcNow.AddMinutes(-5));

        var response = await _client.GetAsync("/invitations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<InvitationsResponse>();
        Assert.NotNull(payload);
        Assert.Single(payload!.Invitations);

        var invitation = payload.Invitations[0];
        Assert.Equal("Owner Name", invitation.OwnerName);
        Assert.Equal("owner@example.com", invitation.OwnerEmail);
        Assert.Equal("Owner Name", invitation.SenderName);
        Assert.Equal("owner@example.com", invitation.SenderEmail);
    }

    private void SeedUser(Guid userId, string displayName, string email)
    {
        lock (_store.SyncRoot)
        {
            _store.Users.Add(new User
            {
                Id = userId,
                DisplayName = displayName,
                Email = email,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
    }

    private void SeedGroup(Guid groupId, Guid ownerId, string name)
    {
        lock (_store.SyncRoot)
        {
            _store.Groups.Add(new Group
            {
                Id = groupId,
                OwnerUserId = ownerId,
                Name = name,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
    }

    private void SeedInvitation(Guid groupId, string invitedEmail, Guid? invitedByUserId, DateTimeOffset createdAt)
    {
        lock (_store.SyncRoot)
        {
            _store.Invitations.Add(new Invitation
            {
                Id = Guid.NewGuid(),
                GroupId = groupId,
                InvitedByUserId = invitedByUserId,
                InvitedEmail = invitedEmail,
                Status = "pending",
                CreatedAt = createdAt
            });
        }
    }
}
