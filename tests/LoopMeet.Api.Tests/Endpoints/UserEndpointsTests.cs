using System.Net;
using System.Net.Http.Json;
using LoopMeet.Api.Tests.Infrastructure;
using Xunit;

namespace LoopMeet.Api.Tests.Endpoints;

public sealed class UserEndpointsTests : IClassFixture<PostgresFixture>
{
    private readonly HttpClient _client;

    public UserEndpointsTests(PostgresFixture fixture)
    {
        var factory = new TestWebApplicationFactory(fixture.ConnectionString);
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
