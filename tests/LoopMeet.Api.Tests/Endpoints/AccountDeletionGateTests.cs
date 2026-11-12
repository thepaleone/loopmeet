using System.Net;
using System.Net.Http.Json;
using LoopMeet.Api.Contracts;
using LoopMeet.Api.Tests.Infrastructure;
using LoopMeet.Core.Models;
using Xunit;

namespace LoopMeet.Api.Tests.Endpoints;

public sealed class AccountDeletionGateTests
{
    private readonly HttpClient _client;
    private readonly InMemoryStore _store;
    private readonly TestWebApplicationFactory _factory;
    private readonly Guid _userId = Guid.NewGuid();

    public AccountDeletionGateTests()
    {
        _store = new InMemoryStore();
        _factory = new TestWebApplicationFactory(_store);
        _factory.Clock.UtcNow = new DateTimeOffset(2026, 11, 3, 18, 0, 0, TimeSpan.Zero);
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", _userId.ToString());
    }

    [Theory]
    [InlineData("/groups")]
    [InlineData("/users/profile")]
    public async Task PendingCallerIsBlockedWithTheDeletionDate(string path)
    {
        var deletesAt = SeedDeletion(AccountDeletionStatus.Pending, _factory.Clock.UtcNow.AddDays(20));

        var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AccountPendingDeletionResponse>();
        Assert.Equal("account_pending_deletion", body!.Code);
        Assert.Equal("This account is scheduled for deletion.", body.Message);
        Assert.Equal(deletesAt, body.DeletesAt);
    }

    [Fact]
    public async Task PurgingCallerIsBlocked()
    {
        SeedDeletion(AccountDeletionStatus.Purging, _factory.Clock.UtcNow.AddDays(-1));

        var response = await _client.GetAsync("/groups");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PendingCallerCanStillUseTheDeletionRoutes()
    {
        SeedDeletion(AccountDeletionStatus.Pending, _factory.Clock.UtcNow.AddDays(20));

        var status = await _client.GetAsync("/users/account-deletion");
        var preview = await _client.GetAsync("/users/account-deletion/preview");
        var request = await _client.PostAsJsonAsync("/users/account-deletion", new AccountDeletionRequest { Confirmation = "DELETE" });

        Assert.Equal(HttpStatusCode.OK, status.StatusCode);
        Assert.Equal(HttpStatusCode.OK, preview.StatusCode);
        Assert.Equal(HttpStatusCode.Accepted, request.StatusCode);
    }

    [Fact]
    public async Task ActiveCallerIsNotBlocked()
    {
        var response = await _client.GetAsync("/groups");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedRequestsAreUnaffected()
    {
        SeedDeletion(AccountDeletionStatus.Pending, _factory.Clock.UtcNow.AddDays(20));
        var anonymous = _factory.CreateClient();

        var root = await anonymous.GetAsync("/");
        var groups = await anonymous.GetAsync("/groups");

        Assert.Equal(HttpStatusCode.OK, root.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, groups.StatusCode);
    }

    [Fact]
    public async Task RequestingDeletionBlocksTheNextCallDespiteTheCachedActiveStatus()
    {
        var before = await _client.GetAsync("/groups");
        Assert.Equal(HttpStatusCode.OK, before.StatusCode);

        await _client.PostAsJsonAsync("/users/account-deletion", new AccountDeletionRequest { Confirmation = "DELETE" });
        var after = await _client.GetAsync("/groups");

        Assert.Equal(HttpStatusCode.Forbidden, after.StatusCode);
    }

    private DateTimeOffset SeedDeletion(AccountDeletionStatus status, DateTimeOffset deletesAt)
    {
        lock (_store.SyncRoot)
        {
            _store.AccountDeletions[_userId] = new AccountDeletion(_userId, deletesAt.AddDays(-30), deletesAt, status);
        }

        return deletesAt;
    }
}
