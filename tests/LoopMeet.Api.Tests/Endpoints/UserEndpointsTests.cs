using System.Net;
using System.Net.Http.Json;
using LoopMeet.Api.Tests.Infrastructure;
using Xunit;

namespace LoopMeet.Api.Tests.Endpoints;

public sealed class UserEndpointsTests
{
    private readonly HttpClient _client;

    public UserEndpointsTests()
    {
        var factory = new TestWebApplicationFactory(new InMemoryStore());
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ProfileUpsertRequiresAuth()
    {
        var response = await _client.PostAsJsonAsync("/users/profile", new
        {
            DisplayName = "Test User",
            Email = "test@example.com",
            Phone = "123"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
