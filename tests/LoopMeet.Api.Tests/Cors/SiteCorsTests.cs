using System.Net;
using LoopMeet.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace LoopMeet.Api.Tests.Cors;

/// <summary>
/// The Api's <c>Site</c> CORS policy (017 contracts/site.md): the website may call the Api from
/// its own origins only, and requests without an Origin header, like the app's, are unchanged.
/// </summary>
public sealed class SiteCorsTests : IDisposable
{
    private const string Site = "https://site.test";
    private const string Preview = "https://main-preview.test";

    private readonly TestWebApplicationFactory _baseFactory = new(new InMemoryStore());
    private readonly HttpClient _client;

    public SiteCorsTests()
    {
        var factory = _baseFactory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Cors:AllowedOrigins"] = $"{Site}, https://*-preview.test"
                })));
        _client = factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _baseFactory.Dispose();
    }

    private static HttpRequestMessage Preflight(string origin, string method = "POST") =>
        new(HttpMethod.Options, "/users/account-deletion")
        {
            Headers =
            {
                { "Origin", origin },
                { "Access-Control-Request-Method", method },
                { "Access-Control-Request-Headers", "authorization, content-type" }
            }
        };

    private static string? Header(HttpResponseMessage response, string name) =>
        response.Headers.TryGetValues(name, out var values) ? string.Join(", ", values) : null;

    [Fact]
    public async Task PreflightFromTheSiteIsAllowed()
    {
        using var response = await _client.SendAsync(Preflight(Site));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(Site, Header(response, "Access-Control-Allow-Origin"));
        var allowedHeaders = Header(response, "Access-Control-Allow-Headers")!.ToLowerInvariant();
        Assert.Contains("authorization", allowedHeaders);
        Assert.Contains("content-type", allowedHeaders);
        Assert.Contains("POST", Header(response, "Access-Control-Allow-Methods")!);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("DELETE")]
    public async Task PreflightAllowsTheOtherAccountDeletionMethods(string method)
    {
        using var response = await _client.SendAsync(Preflight(Site, method));

        Assert.Equal(Site, Header(response, "Access-Control-Allow-Origin"));
        Assert.Contains(method, Header(response, "Access-Control-Allow-Methods")!);
    }

    [Fact]
    public async Task PreflightFromAPreviewIsAllowed()
    {
        using var response = await _client.SendAsync(Preflight(Preview));

        Assert.Equal(Preview, Header(response, "Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task PreflightFromAnotherSiteGetsNoCorsHeaders()
    {
        using var response = await _client.SendAsync(Preflight("https://evil.test"));

        Assert.Null(Header(response, "Access-Control-Allow-Origin"));
        Assert.Null(Header(response, "Access-Control-Allow-Methods"));
    }

    [Fact]
    public async Task UnauthenticatedRequestFromTheSiteStillCarriesTheOrigin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/users/account-deletion")
        {
            Headers = { { "Origin", Site } }
        };

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(Site, Header(response, "Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task CredentialsAreNeverAllowed()
    {
        using var preflight = await _client.SendAsync(Preflight(Site));
        using var request = new HttpRequestMessage(HttpMethod.Get, "/") { Headers = { { "Origin", Site } } };
        using var actual = await _client.SendAsync(request);

        Assert.Null(Header(preflight, "Access-Control-Allow-Credentials"));
        Assert.Null(Header(actual, "Access-Control-Allow-Credentials"));
    }

    [Fact]
    public async Task DevelopmentSettingsReplaceTheProductionListWithLocalhost()
    {
        // The test host runs as Development, so appsettings.Development.json applies. The setting
        // is one string, so it replaces appsettings.json's list rather than merging by index.
        using var client = _baseFactory.CreateClient();

        using var local = await client.SendAsync(Preflight("http://localhost:4507"));
        using var live = await client.SendAsync(Preflight("https://account.loopmeet.io"));

        Assert.Equal("http://localhost:4507", Header(local, "Access-Control-Allow-Origin"));
        Assert.Null(Header(live, "Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task RequestWithoutAnOriginIsUnchanged()
    {
        using var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(response.Headers, header => header.Key.StartsWith("Access-Control-", StringComparison.OrdinalIgnoreCase));
    }
}
