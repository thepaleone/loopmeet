using System.Net;
using System.Text;
using LoopMeet.App.Features.Auth.Session;
using LoopMeet.App.Services;
using LoopMeet.App.Tests.TestDoubles;

namespace LoopMeet.App.Tests.Features.Auth.Session;

public sealed class ApiAuthHandlerRetryTests
{
    private static HttpClient BuildClient(ScriptedHttpMessageHandler inner, FakeSessionTokenSource tokens) =>
        new(new ApiAuthHandler(tokens) { InnerHandler = inner });

    [Fact]
    public async Task Unauthorized_RefreshRenewed_RetriesOnceWithNewToken()
    {
        var inner = new ScriptedHttpMessageHandler(HttpStatusCode.Unauthorized, HttpStatusCode.OK);
        var tokens = new FakeSessionTokenSource("stale-token", "fresh-token") { RefreshOutcome = RenewalOutcome.Renewed };
        using var client = BuildClient(inner, tokens);

        var response = await client.GetAsync("https://api.test/groups");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, tokens.RefreshCalls);
        Assert.Equal(2, inner.Requests.Count);
        Assert.Equal("stale-token", inner.Requests[0].BearerToken);
        Assert.Equal("fresh-token", inner.Requests[1].BearerToken);
    }

    [Fact]
    public async Task Unauthorized_DefinitiveRejection_DoesNotRetry()
    {
        var inner = new ScriptedHttpMessageHandler(HttpStatusCode.Unauthorized);
        var tokens = new FakeSessionTokenSource("stale-token") { RefreshOutcome = RenewalOutcome.DefinitivelyRejectedSignedOut };
        using var client = BuildClient(inner, tokens);

        var response = await client.GetAsync("https://api.test/groups");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, tokens.RefreshCalls);
        Assert.Single(inner.Requests);
    }

    [Fact]
    public async Task Unauthorized_TransientRefreshFailure_ReturnsOriginal401()
    {
        var inner = new ScriptedHttpMessageHandler(HttpStatusCode.Unauthorized);
        var tokens = new FakeSessionTokenSource("stale-token") { RefreshOutcome = RenewalOutcome.TransientFailureKeptSession };
        using var client = BuildClient(inner, tokens);

        var response = await client.GetAsync("https://api.test/groups");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Single(inner.Requests);
    }

    [Fact]
    public async Task SuccessfulResponse_NeverRefreshes()
    {
        var inner = new ScriptedHttpMessageHandler(HttpStatusCode.OK);
        var tokens = new FakeSessionTokenSource("token");
        using var client = BuildClient(inner, tokens);

        var response = await client.GetAsync("https://api.test/groups");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, tokens.RefreshCalls);
    }

    [Fact]
    public async Task SecondUnauthorizedAfterRetry_IsReturnedAsIs_ExactlyOneRetry()
    {
        var inner = new ScriptedHttpMessageHandler(HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized);
        var tokens = new FakeSessionTokenSource("stale-token", "fresh-token") { RefreshOutcome = RenewalOutcome.Renewed };
        using var client = BuildClient(inner, tokens);

        var response = await client.GetAsync("https://api.test/groups");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(1, tokens.RefreshCalls);
        Assert.Equal(2, inner.Requests.Count);
    }

    [Fact]
    public async Task Retry_ClonesTheRequestBody()
    {
        var inner = new ScriptedHttpMessageHandler(HttpStatusCode.Unauthorized, HttpStatusCode.OK);
        var tokens = new FakeSessionTokenSource("stale-token", "fresh-token") { RefreshOutcome = RenewalOutcome.Renewed };
        using var client = BuildClient(inner, tokens);

        var content = new StringContent("""{"name":"climbing"}""", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("https://api.test/groups", content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, inner.Requests.Count);
        Assert.Equal("""{"name":"climbing"}""", inner.Requests[1].Body);
    }

    [Fact]
    public async Task NoToken_SendsWithoutAuthorizationHeader()
    {
        var inner = new ScriptedHttpMessageHandler(HttpStatusCode.OK);
        var tokens = new FakeSessionTokenSource(default(string));
        using var client = BuildClient(inner, tokens);

        await client.GetAsync("https://api.test/health");

        Assert.Null(inner.Requests[0].BearerToken);
    }
}
