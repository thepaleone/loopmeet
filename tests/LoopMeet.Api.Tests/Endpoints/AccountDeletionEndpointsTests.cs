using System.Net;
using System.Net.Http.Json;
using System.Text;
using LoopMeet.Api.Contracts;
using LoopMeet.Api.Tests.Infrastructure;
using LoopMeet.Core.Models;
using Serilog.Events;
using Xunit;

namespace LoopMeet.Api.Tests.Endpoints;

[Collection(CapturedLogsCollection.Name)]
public sealed class AccountDeletionEndpointsTests
{
    private const string CallerEmail = "deleting.user@example.com";

    private readonly HttpClient _client;
    private readonly InMemoryStore _store;
    private readonly TestWebApplicationFactory _factory;
    private readonly Guid _userId = Guid.NewGuid();

    public AccountDeletionEndpointsTests()
    {
        _store = new InMemoryStore();
        _factory = new TestWebApplicationFactory(_store);
        _factory.Clock.UtcNow = new DateTimeOffset(2026, 11, 3, 18, 0, 0, TimeSpan.Zero);
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", _userId.ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Email", CallerEmail);
    }

    [Fact]
    public async Task PreviewListsOwnedGroupsByNameWithMemberCountsIncludingOwner()
    {
        var climbers = SeedGroup(_userId, "Friday Climbers", Guid.NewGuid(), Guid.NewGuid());
        var books = SeedGroup(_userId, "Book Club");
        SeedGroup(Guid.NewGuid(), "Someone else's group", _userId);

        var preview = await _client.GetFromJsonAsync<AccountDeletionPreviewResponse>("/users/account-deletion/preview");

        Assert.NotNull(preview);
        Assert.Collection(preview!.OwnedGroups,
            group =>
            {
                Assert.Equal(books, group.Id);
                Assert.Equal("Book Club", group.Name);
                Assert.Equal(1, group.MemberCount);
            },
            group =>
            {
                Assert.Equal(climbers, group.Id);
                Assert.Equal("Friday Climbers", group.Name);
                Assert.Equal(3, group.MemberCount);
            });
    }

    [Fact]
    public async Task PreviewWithNoOwnedGroupsReturnsEmptyList()
    {
        var preview = await _client.GetFromJsonAsync<AccountDeletionPreviewResponse>("/users/account-deletion/preview");

        Assert.NotNull(preview);
        Assert.Empty(preview!.OwnedGroups);
    }

    [Fact]
    public async Task PreviewProjectsThirtyDaysBeforeARequestAndReturnsTheStoredDateAfter()
    {
        var before = await _client.GetFromJsonAsync<AccountDeletionPreviewResponse>("/users/account-deletion/preview");
        Assert.Equal(_factory.Clock.UtcNow.AddDays(30), before!.DeletesAt);

        await RequestDeletionAsync("DELETE");
        _factory.Clock.UtcNow = _factory.Clock.UtcNow.AddDays(2);

        var after = await _client.GetFromJsonAsync<AccountDeletionPreviewResponse>("/users/account-deletion/preview");
        Assert.Equal(new DateTimeOffset(2026, 12, 3, 18, 0, 0, TimeSpan.Zero), after!.DeletesAt);
    }

    [Fact]
    public async Task RequestWithDeleteReturnsAcceptedAndPendingBody()
    {
        var response = await RequestDeletionAsync("DELETE");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AccountDeletionStatusResponse>();
        Assert.Equal("pending", body!.Status);
        Assert.Equal(_factory.Clock.UtcNow, body.RequestedAt);
        Assert.Equal(_factory.Clock.UtcNow.AddDays(30), body.DeletesAt);
    }

    [Fact]
    public async Task RepeatRequestReturnsTheOriginalDates()
    {
        var first = await (await RequestDeletionAsync("DELETE")).Content.ReadFromJsonAsync<AccountDeletionStatusResponse>();
        _factory.Clock.UtcNow = _factory.Clock.UtcNow.AddDays(5);

        var response = await RequestDeletionAsync("DELETE");

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var second = await response.Content.ReadFromJsonAsync<AccountDeletionStatusResponse>();
        Assert.Equal(first!.RequestedAt, second!.RequestedAt);
        Assert.Equal(first.DeletesAt, second.DeletesAt);
    }

    [Theory]
    [InlineData("delete")]
    [InlineData("DELETE ")]
    [InlineData(" DELETE")]
    [InlineData("")]
    [InlineData(null)]
    public async Task RequestWithoutExactDeleteReturnsInvalidConfirmation(string? confirmation)
    {
        var response = await RequestDeletionAsync(confirmation);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("invalid_confirmation", error!.Code);
        Assert.Equal("Type DELETE to confirm.", error.Message);
        Assert.Empty(_store.AccountDeletions);
    }

    [Fact]
    public async Task RequestWithMissingBodyReturnsInvalidConfirmation()
    {
        var response = await _client.PostAsync("/users/account-deletion", new StringContent(string.Empty, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("invalid_confirmation", error!.Code);
    }

    [Fact]
    public async Task RequestWhilePurgingReturnsConflict()
    {
        SeedDeletion(AccountDeletionStatus.Purging, _factory.Clock.UtcNow.AddDays(-1));

        var response = await RequestDeletionAsync("DELETE");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal("deletion_in_progress", error!.Code);
        Assert.Equal("This account is already being deleted.", error.Message);
    }

    [Fact]
    public async Task RequestAfterTheDeadlineReturnsConflict()
    {
        SeedDeletion(AccountDeletionStatus.Pending, _factory.Clock.UtcNow.AddMinutes(-1));

        var response = await RequestDeletionAsync("DELETE");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task StatusIsNoneThenPending()
    {
        var before = await _client.GetFromJsonAsync<AccountDeletionStatusResponse>("/users/account-deletion");
        Assert.Equal("none", before!.Status);
        Assert.Null(before.RequestedAt);
        Assert.Null(before.DeletesAt);

        await RequestDeletionAsync("DELETE");

        var after = await _client.GetFromJsonAsync<AccountDeletionStatusResponse>("/users/account-deletion");
        Assert.Equal("pending", after!.Status);
        Assert.Equal(_factory.Clock.UtcNow.AddDays(30), after.DeletesAt);
    }

    [Fact]
    public async Task NoneStatusOmitsTheDates()
    {
        var json = await _client.GetStringAsync("/users/account-deletion");

        Assert.Equal("{\"status\":\"none\"}", json);
    }

    [Fact]
    public async Task RequestLogsUserIdAndNeverTheEmail()
    {
        await RequestDeletionAsync("DELETE");

        var requested = Assert.Single(_factory.Logs.Events, e => e.MessageTemplate.Text.StartsWith("AccountDeletionRequested", StringComparison.Ordinal));
        Assert.Equal(_userId.ToString(), ScalarText(requested.Properties["UserId"]));

        foreach (var logEvent in _factory.Logs.Events)
        {
            Assert.DoesNotContain(CallerEmail, logEvent.RenderMessage(), StringComparison.OrdinalIgnoreCase);
            Assert.All(logEvent.Properties.Values, value =>
                Assert.DoesNotContain(CallerEmail, value.ToString(), StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task RepeatRequestLogsAlreadyPending()
    {
        await RequestDeletionAsync("DELETE");
        await RequestDeletionAsync("DELETE");

        var repeat = Assert.Single(_factory.Logs.Events, e => e.MessageTemplate.Text.StartsWith("AccountDeletionAlreadyPending", StringComparison.Ordinal));
        Assert.Equal(_userId.ToString(), ScalarText(repeat.Properties["UserId"]));
    }

    private Task<HttpResponseMessage> RequestDeletionAsync(string? confirmation) =>
        _client.PostAsJsonAsync("/users/account-deletion", new AccountDeletionRequest { Confirmation = confirmation });

    private void SeedDeletion(AccountDeletionStatus status, DateTimeOffset deletesAt)
    {
        lock (_store.SyncRoot)
        {
            _store.AccountDeletions[_userId] = new AccountDeletion(_userId, deletesAt.AddDays(-30), deletesAt, status);
        }
    }

    private Guid SeedGroup(Guid ownerId, string name, params Guid[] otherMemberIds)
    {
        var groupId = Guid.NewGuid();
        lock (_store.SyncRoot)
        {
            _store.Groups.Add(new Group { Id = groupId, OwnerUserId = ownerId, Name = name });
            _store.Memberships.Add(new Membership { Id = Guid.NewGuid(), GroupId = groupId, UserId = ownerId, Role = "owner" });
            foreach (var memberId in otherMemberIds)
            {
                _store.Memberships.Add(new Membership { Id = Guid.NewGuid(), GroupId = groupId, UserId = memberId, Role = "member" });
            }
        }

        return groupId;
    }

    private static string? ScalarText(LogEventPropertyValue value) =>
        value is ScalarValue scalar ? scalar.Value?.ToString() : value.ToString();
}
