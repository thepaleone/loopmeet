using System.Net;
using LoopMeet.Api.Tests.Infrastructure;

namespace LoopMeet.Api.Tests.Endpoints;

public sealed class PlacesEndpointsTests
{
    private readonly HttpClient _client;

    public PlacesEndpointsTests()
    {
        var store = new InMemoryStore();
        var factory = new TestWebApplicationFactory(store);
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", Guid.NewGuid().ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Email", "places@example.com");
    }

    [Fact]
    public async Task Autocomplete_ReturnsBadRequest_WhenQueryTooShort()
    {
        var response = await _client.GetAsync("/places/autocomplete?query=a");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Autocomplete_AcceptsOptionalLocationBiasParameters()
    {
        var response = await _client.GetAsync("/places/autocomplete?query=coffee&latitude=37.7749&longitude=-122.4194&radiusMeters=5000");

        Assert.True(response.StatusCode is HttpStatusCode.OK or HttpStatusCode.BadGateway or HttpStatusCode.InternalServerError);
    }
}
