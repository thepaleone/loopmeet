using System.Net;
using System.Net.Http.Json;
using LoopMeet.Api.Contracts;
using LoopMeet.Api.Tests.Infrastructure;
using LoopMeet.Core.Models;
using Serilog.Events;
using Xunit;

namespace LoopMeet.Api.Tests.Endpoints;

[Collection(CapturedLogsCollection.Name)]
public sealed class ReportsEndpointsTests
{
    private const string CallerEmail = "reporter@example.com";
    private const string Details = "He keeps sending threats in the meetup notes.";

    private readonly HttpClient _client;
    private readonly InMemoryStore _store;
    private readonly TestWebApplicationFactory _factory;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly Guid _groupId;

    public ReportsEndpointsTests()
    {
        _store = new InMemoryStore();
        _factory = new TestWebApplicationFactory(_store);
        _factory.Clock.UtcNow = new DateTimeOffset(2026, 11, 16, 18, 0, 0, TimeSpan.Zero);
        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", _userId.ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Email", CallerEmail);
        _groupId = SeedGroup(_ownerId, _userId);
    }

    [Fact]
    public async Task AMemberReportsAnotherMember()
    {
        var response = await PostAsync(Request(_ownerId, "harassment_or_threats", "  " + Details + "  "));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ReportResponse>();
        Assert.NotNull(body);
        Assert.Equal($"/reports/{body!.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(_factory.Clock.UtcNow, body.ReceivedAt);

        var stored = Assert.Single(_store.AbuseReports);
        Assert.Equal(_userId, stored.ReporterUserId);
        Assert.Equal(_ownerId, stored.ReportedUserId);
        Assert.Equal(ReportReason.HarassmentOrThreats, stored.Reason);
        Assert.Equal(Details, stored.Details);
    }

    [Fact]
    public async Task AMemberReportsTheGroupItself()
    {
        var response = await PostAsync(Request(null, "inappropriate_photo_or_name", "The group name is offensive."));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Null(Assert.Single(_store.AbuseReports).ReportedUserId);
    }

    [Theory]
    [InlineData("rude", Details)]
    [InlineData("", Details)]
    [InlineData("spam", "")]
    [InlineData("spam", "   ")]
    public async Task AnInvalidReasonOrBlankDetailsIsRefused(string reason, string details)
    {
        var response = await PostAsync(Request(null, reason, details));

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "invalid_report",
            "Choose a reason and describe what happened (up to 1,000 characters).");
    }

    [Fact]
    public async Task DetailsOverOneThousandCharactersAreRefused()
    {
        var response = await PostAsync(Request(null, "spam", new string('x', 1001)));

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "invalid_report",
            "Choose a reason and describe what happened (up to 1,000 characters).");
    }

    [Fact]
    public async Task AReportAboutAGroupTheCallerIsNotInIsForbidden()
    {
        var otherGroup = SeedGroup(Guid.NewGuid());

        var response = await PostAsync(new ReportRequest { GroupId = otherGroup, Reason = "spam", Details = Details });

        await AssertErrorAsync(response, HttpStatusCode.Forbidden, "not_group_member",
            "You can only report groups you belong to.");
    }

    [Fact]
    public async Task ACallerCannotReportThemselves()
    {
        var response = await PostAsync(Request(_userId, "spam", Details));

        await AssertErrorAsync(response, HttpStatusCode.UnprocessableEntity, "invalid_reported_user",
            "Choose someone in this group other than yourself.");
    }

    [Fact]
    public async Task APersonOutsideTheGroupCannotBeReported()
    {
        var response = await PostAsync(Request(Guid.NewGuid(), "spam", Details));

        await AssertErrorAsync(response, HttpStatusCode.UnprocessableEntity, "invalid_reported_user",
            "Choose someone in this group other than yourself.");
    }

    [Fact]
    public async Task TheEleventhReportInADayIsRefusedButOlderReportsDoNotCount()
    {
        lock (_store.SyncRoot)
        {
            _store.AbuseReports.Add(new AbuseReport(Guid.NewGuid(), _userId, _groupId, null, ReportReason.Spam, "old",
                _factory.Clock.UtcNow.AddHours(-25)));
        }

        for (var i = 0; i < 10; i++)
        {
            var accepted = await PostAsync(Request(null, "spam", $"report {i}"));
            Assert.Equal(HttpStatusCode.Created, accepted.StatusCode);
        }

        var refused = await PostAsync(Request(null, "spam", "one more"));

        await AssertErrorAsync(refused, (HttpStatusCode)429, "report_limit_reached",
            "You've sent 10 reports today. You can send more later, or email abuse@loopmeet.io.");
    }

    [Fact]
    public async Task AnUnauthenticatedCallerIsRejected()
    {
        using var anonymous = _factory.CreateClient();

        var response = await anonymous.PostAsJsonAsync("/reports", Request(null, "spam", Details));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LogsCarryIdsAndTheReasonCodeButNeverTheDetailsOrAnEmail()
    {
        await PostAsync(Request(_ownerId, "child_safety", Details));
        await PostAsync(Request(_userId, "spam", Details));

        var submitted = Assert.Single(_factory.Logs.Events, e => e.MessageTemplate.Text.StartsWith("ReportSubmitted", StringComparison.Ordinal));
        Assert.Equal(_groupId.ToString(), ScalarText(submitted.Properties["GroupId"]));
        Assert.Equal("child_safety", ScalarText(submitted.Properties["Reason"]));
        Assert.Contains(_factory.Logs.Events, e => e.MessageTemplate.Text.StartsWith("ReportRefused", StringComparison.Ordinal));

        foreach (var logEvent in _factory.Logs.Events)
        {
            var rendered = logEvent.RenderMessage() + string.Join(' ', logEvent.Properties.Values);
            Assert.DoesNotContain(CallerEmail, rendered, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("threats in the meetup notes", rendered, StringComparison.Ordinal);
        }
    }

    private static string? ScalarText(LogEventPropertyValue value) => (value as ScalarValue)?.Value?.ToString();

    private ReportRequest Request(Guid? reportedUserId, string reason, string details) => new()
    {
        GroupId = _groupId,
        ReportedUserId = reportedUserId,
        Reason = reason,
        Details = details
    };

    private Task<HttpResponseMessage> PostAsync(ReportRequest request) => _client.PostAsJsonAsync("/reports", request);

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string code, string message)
    {
        Assert.Equal(status, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Equal(code, error!.Code);
        Assert.Equal(message, error.Message);
    }

    private Guid SeedGroup(Guid ownerId, params Guid[] otherMemberIds)
    {
        var groupId = Guid.NewGuid();
        lock (_store.SyncRoot)
        {
            _store.Groups.Add(new Group { Id = groupId, OwnerUserId = ownerId, Name = "Book club" });
            _store.Memberships.Add(new Membership { Id = Guid.NewGuid(), GroupId = groupId, UserId = ownerId, Role = "owner" });
            foreach (var memberId in otherMemberIds)
            {
                _store.Memberships.Add(new Membership { Id = Guid.NewGuid(), GroupId = groupId, UserId = memberId, Role = "member" });
            }
        }

        return groupId;
    }
}
