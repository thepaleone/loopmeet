using System.Net;
using System.Net.Http.Json;
using LoopMeet.Api.Contracts;
using LoopMeet.Api.Tests.Infrastructure;
using LoopMeet.Core.Models;
using LoopMeet.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LoopMeet.Api.Tests.Endpoints;

public sealed class GroupsWriteEndpointsTests : IClassFixture<PostgresFixture>
{
    private readonly HttpClient _client;
    private readonly PostgresFixture _fixture;

    public GroupsWriteEndpointsTests(PostgresFixture fixture)
    {
        _fixture = fixture;
        var factory = new TestWebApplicationFactory(fixture.ConnectionString);
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreatesGroupAndOwnerMembership()
    {
        var ownerId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", ownerId.ToString());

        var response = await _client.PostAsJsonAsync("/groups", new CreateGroupRequest
        {
            Name = "Weekenders"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<GroupSummaryResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Weekenders", payload!.Name);
        Assert.Equal(ownerId, payload.OwnerUserId);

        await using var dbContext = await CreateDbContextAsync();
        var group = await dbContext.Groups.FirstOrDefaultAsync(group => group.Id == payload.Id);
        Assert.NotNull(group);

        var membership = await dbContext.Memberships
            .FirstOrDefaultAsync(member => member.GroupId == payload.Id && member.UserId == ownerId);
        Assert.NotNull(membership);
        Assert.Equal("owner", membership!.Role);
    }

    [Fact]
    public async Task RejectsDuplicateNameOnCreate()
    {
        var ownerId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", ownerId.ToString());

        await SeedGroupAsync(ownerId, "Crew");

        var response = await _client.PostAsJsonAsync("/groups", new CreateGroupRequest
        {
            Name = "Crew"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RenamesGroup()
    {
        var ownerId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", ownerId.ToString());

        var groupId = await SeedGroupAsync(ownerId, "Original");

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/groups/{groupId}")
        {
            Content = JsonContent.Create(new UpdateGroupRequest
            {
                Name = "Updated"
            })
        };

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<GroupSummaryResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Updated", payload!.Name);

        await using var dbContext = await CreateDbContextAsync();
        var group = await dbContext.Groups.FirstOrDefaultAsync(candidate => candidate.Id == groupId);
        Assert.NotNull(group);
        Assert.Equal("Updated", group!.Name);
    }

    [Fact]
    public async Task RejectsDuplicateNameOnRename()
    {
        var ownerId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", ownerId.ToString());

        var firstGroupId = await SeedGroupAsync(ownerId, "Morning Crew");
        await SeedGroupAsync(ownerId, "Evening Crew");

        var request = new HttpRequestMessage(HttpMethod.Patch, $"/groups/{firstGroupId}")
        {
            Content = JsonContent.Create(new UpdateGroupRequest
            {
                Name = "Evening Crew"
            })
        };

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<LoopMeetDbContext> CreateDbContextAsync()
    {
        var options = new DbContextOptionsBuilder<LoopMeetDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options;

        var dbContext = new LoopMeetDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        return dbContext;
    }

    private async Task<Guid> SeedGroupAsync(Guid ownerId, string name)
    {
        await using var dbContext = await CreateDbContextAsync();

        var group = new Group
        {
            Id = Guid.NewGuid(),
            OwnerUserId = ownerId,
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Groups.Add(group);
        await dbContext.SaveChangesAsync();

        return group.Id;
    }
}
