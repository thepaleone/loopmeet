using System.Net;
using System.Net.Http.Json;
using LoopMeet.Api.Contracts;
using LoopMeet.Api.Tests.Infrastructure;
using LoopMeet.Core.Models;
using Xunit;

namespace LoopMeet.Api.Tests.Endpoints;

public sealed class UserEndpointsTests
{
    private readonly HttpClient _client;
    private readonly InMemoryStore _store;

    public UserEndpointsTests()
    {
        _store = new InMemoryStore();
        var factory = new TestWebApplicationFactory(_store);
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

    [Fact]
    public async Task GetProfileReturnsSummaryProjection()
    {
        var userId = Guid.NewGuid();
        var ownedGroupId = Guid.NewGuid();
        var memberGroupId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        lock (_store.SyncRoot)
        {
            _store.Users.Add(new User
            {
                Id = userId,
                DisplayName = "Profile User",
                Email = "profile@example.com",
                SocialAvatarUrl = "https://example.com/social.png",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-10),
                UpdatedAt = DateTimeOffset.UtcNow
            });

            _store.Groups.Add(new Group
            {
                Id = ownedGroupId,
                OwnerUserId = userId,
                Name = "Owned Group",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

            _store.Groups.Add(new Group
            {
                Id = memberGroupId,
                OwnerUserId = Guid.NewGuid(),
                Name = "Member Group",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

            _store.Memberships.Add(new Membership
            {
                Id = Guid.NewGuid(),
                GroupId = memberGroupId,
                UserId = userId,
                Role = "member",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        var response = await _client.GetAsync("/users/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.NotNull(payload);
        Assert.Equal("Profile User", payload!.DisplayName);
        Assert.Equal("profile@example.com", payload.Email);
        Assert.Equal("social", payload.AvatarSource);
        Assert.Equal("https://example.com/social.png", payload.AvatarUrl);
        Assert.Equal(2, payload.GroupCount);
        Assert.True(payload.CanChangePassword);
        Assert.True(payload.HasEmailProvider);
        Assert.True(payload.RequiresCurrentPassword);
        Assert.False(payload.RequiresEmailForPasswordSetup);
    }

    [Fact]
    public async Task PatchProfileUpdatesDisplayNameAndAvatarOverride()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        lock (_store.SyncRoot)
        {
            _store.Users.Add(new User
            {
                Id = userId,
                DisplayName = "Before",
                Email = "before@example.com",
                SocialAvatarUrl = "https://example.com/social.png",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        var patchResponse = await _client.PatchAsJsonAsync("/users/profile", new
        {
            DisplayName = "After",
            AvatarOverrideUrl = "https://example.com/override.png"
        });

        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        var payload = await patchResponse.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.NotNull(payload);
        Assert.Equal("After", payload!.DisplayName);
        Assert.Equal("user_override", payload.AvatarSource);
        Assert.Equal("https://example.com/override.png", payload.AvatarUrl);
    }

    [Fact]
    public async Task UpsertProfileCopiesSocialAvatarWhenNoOverride()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        var response = await _client.PostAsJsonAsync("/users/profile", new
        {
            DisplayName = "Social User",
            Email = "social@example.com",
            SocialAvatarUrl = "https://example.com/social-bootstrap.png"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.NotNull(payload);
        Assert.Equal("social", payload!.AvatarSource);
        Assert.Equal("https://example.com/social-bootstrap.png", payload.AvatarUrl);
    }

    [Fact]
    public async Task UpsertProfileDoesNotOverwriteAvatarOverrideWithSocialAvatar()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        lock (_store.SyncRoot)
        {
            _store.Users.Add(new User
            {
                Id = userId,
                DisplayName = "Override User",
                Email = "override@example.com",
                SocialAvatarUrl = "https://example.com/social-old.png",
                AvatarOverrideUrl = "https://example.com/override.png",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }

        var response = await _client.PostAsJsonAsync("/users/profile", new
        {
            DisplayName = "Override User",
            SocialAvatarUrl = "https://example.com/social-new.png"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<UserProfileResponse>();
        Assert.NotNull(payload);
        Assert.Equal("user_override", payload!.AvatarSource);
        Assert.Equal("https://example.com/override.png", payload.AvatarUrl);
    }

    [Fact]
    public async Task PasswordChangeReturnsValidationErrorForMismatch()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        var response = await _client.PostAsJsonAsync("/users/password", new
        {
            CurrentPassword = "CurrentPass123!",
            NewPassword = "NewPass123!",
            ConfirmPassword = "Different123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal("password_mismatch", payload!.Code);
        Assert.Equal("The new password and confirmation do not match.", payload.Message);
    }

    [Fact]
    public async Task PasswordChangeReturnsCurrentPasswordError()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Email", "email-user@example.com");

        lock (_store.SyncRoot)
        {
            _store.Users.Add(new User
            {
                Id = userId,
                DisplayName = "Email User",
                Email = "email-user@example.com",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            _store.AuthIdentities.Add(new AuthIdentity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Provider = "email",
                ProviderSubject = "email-user@example.com",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        var response = await _client.PostAsJsonAsync("/users/password", new
        {
            CurrentPassword = "invalid",
            NewPassword = "NewPass123!",
            ConfirmPassword = "NewPass123!"
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal("invalid_current_password", payload!.Code);
        Assert.Equal("Current password is incorrect.", payload.Message);
    }

    [Fact]
    public async Task PasswordChangeReturnsNoContentWhenValid()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Email", "email-user@example.com");

        lock (_store.SyncRoot)
        {
            _store.Users.Add(new User
            {
                Id = userId,
                DisplayName = "Email User",
                Email = "email-user@example.com",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            _store.AuthIdentities.Add(new AuthIdentity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Provider = "email",
                ProviderSubject = "email-user@example.com",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        var response = await _client.PostAsJsonAsync("/users/password", new
        {
            CurrentPassword = "CurrentPass123!",
            NewPassword = "NewPass123!",
            ConfirmPassword = "NewPass123!"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PasswordChangeAllowsSocialOnlyUserWithoutCurrentPasswordMatch()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());
        _client.DefaultRequestHeaders.Add("X-Test-Email", "social-user@example.com");

        lock (_store.SyncRoot)
        {
            _store.Users.Add(new User
            {
                Id = userId,
                DisplayName = "Social User",
                Email = "social-user@example.com",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            _store.AuthIdentities.Add(new AuthIdentity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Provider = "google",
                ProviderSubject = "google-subject-123",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        var response = await _client.PostAsJsonAsync("/users/password", new
        {
            CurrentPassword = string.Empty,
            NewPassword = "NewPass123!",
            ConfirmPassword = "NewPass123!"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PasswordChangeReturnsMissingAccountEmailWhenUnresolvable()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        lock (_store.SyncRoot)
        {
            _store.Users.Add(new User
            {
                Id = userId,
                DisplayName = "No Email User",
                Email = string.Empty,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            _store.AuthIdentities.Add(new AuthIdentity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Provider = "google",
                ProviderSubject = "google-subject-123",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        var response = await _client.PostAsJsonAsync("/users/password", new
        {
            CurrentPassword = "any-value",
            NewPassword = "NewPass123!",
            ConfirmPassword = "NewPass123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal("missing_account_email", payload!.Code);
        Assert.Equal("Enter your account email to set your password.", payload.Message);
    }

    [Fact]
    public async Task PasswordChangeAllowsSocialOnlyUserWhenEmailProvidedInRequest()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        lock (_store.SyncRoot)
        {
            _store.Users.Add(new User
            {
                Id = userId,
                DisplayName = "Social User",
                Email = string.Empty,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            _store.AuthIdentities.Add(new AuthIdentity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Provider = "google",
                ProviderSubject = "google-subject-123",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        var response = await _client.PostAsJsonAsync("/users/password", new
        {
            Email = "social-user@example.com",
            CurrentPassword = "any-value",
            NewPassword = "NewPass123!",
            ConfirmPassword = "NewPass123!"
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PasswordChangeReturnsMissingAccountEmailForEmailProviderWhenUnresolvable()
    {
        var userId = Guid.NewGuid();
        _client.DefaultRequestHeaders.Add("X-Test-UserId", userId.ToString());

        lock (_store.SyncRoot)
        {
            _store.Users.Add(new User
            {
                Id = userId,
                DisplayName = "Email User",
                Email = string.Empty,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });
            _store.AuthIdentities.Add(new AuthIdentity
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Provider = "email",
                ProviderSubject = "email-user@example.com",
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        var response = await _client.PostAsJsonAsync("/users/password", new
        {
            CurrentPassword = "CurrentPass123!",
            NewPassword = "NewPass123!",
            ConfirmPassword = "NewPass123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.NotNull(payload);
        Assert.Equal("missing_account_email", payload!.Code);
        Assert.Equal("Enter your account email to verify your current password.", payload.Message);
    }
}
