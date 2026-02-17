using System.Net;
using LoopMeet.Api.Tests.Infrastructure;
using LoopMeet.Core.Models;
using Xunit;
using System.Net.Http.Json;

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
    public async Task ReturnsPendingInvitationsForEmail()
    {
        var userId = Guid.NewGuid();
        var email = "invitee@example.com";
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Email", email);

        await SeedAsync(email);

        var response = await _client.GetAsync("/invitations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<InvitationsResponse>();
        Assert.NotNull(payload);
        Assert.Single(payload!.Invitations);
        Assert.Equal(email, payload.Invitations[0].InvitedEmail);
    }

    private async Task SeedAsync(string email)
    {
        lock (_store.SyncRoot)
        {
            _store.Invitations.Add(new Invitation
            {
                Id = Guid.NewGuid(),
                GroupId = Guid.NewGuid(),
                InvitedEmail = email,
                Status = "pending",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await Task.CompletedTask;
    }

    private sealed class InvitationsResponse
    {
        public List<InvitationSummary> Invitations { get; set; } = new();
    }

    private sealed class InvitationSummary
    {
        public string InvitedEmail { get; set; } = string.Empty;
    }
}
