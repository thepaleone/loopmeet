using LoopMeet.App.Features.Auth;
using GotrueSession = Supabase.Gotrue.Session;

namespace LoopMeet.App.Tests.Features.Auth;

public sealed class SessionStorageFormatTests
{
    // Exactly what MauiSessionPersistence stored under Supabase.Gotrue 6.0.3: System.Text.Json
    // ignored that version's Newtonsoft attributes, so every name is the C# property name.
    // Generated with the real 6.0.3 package; the tokens and ids are fake.
    private const string LegacyJson = """
        {
          "AccessToken": "legacy-access-token",
          "ExpiresIn": 3600,
          "ProviderToken": null,
          "ProviderRefreshToken": null,
          "RefreshToken": "legacy-refresh-token",
          "TokenType": "bearer",
          "User": {
            "AppMetadata": { "provider": "google", "providers": [ "google" ] },
            "Aud": "authenticated",
            "ConfirmationSentAt": null,
            "ConfirmedAt": null,
            "CreatedAt": "2026-03-09T17:00:00Z",
            "Email": "t1@example.test",
            "EmailConfirmedAt": null,
            "Id": "6f1c2a4e-0000-4000-8000-000000000001",
            "Identities": [
              {
                "Id": "111",
                "UserId": "6f1c2a4e-0000-4000-8000-000000000001",
                "IdentityData": { "email": "t1@example.test" },
                "IdentityId": "6f1c2a4e-0000-4000-8000-0000000000aa",
                "Provider": "google",
                "CreatedAt": "0001-01-01T00:00:00",
                "LastSignInAt": "0001-01-01T00:00:00",
                "UpdatedAt": null
              }
            ],
            "InvitedAt": null,
            "LastSignInAt": "2026-11-01T08:00:00Z",
            "Phone": null,
            "PhoneConfirmedAt": null,
            "RecoverySentAt": null,
            "Role": "authenticated",
            "UpdatedAt": null,
            "BannedUntil": null,
            "IsAnonymous": false,
            "Factors": [],
            "UserMetadata": { "full_name": "Test One", "avatar_url": "https://example.test/a.png" }
          },
          "CreatedAt": "2026-11-10T18:00:00Z"
        }
        """;

    [Fact]
    public void Read_LegacyFormat_KeepsTheCredentialsAndTheirAge()
    {
        var stored = SessionStorageFormat.Read(LegacyJson);

        Assert.NotNull(stored);
        Assert.True(stored.Value.IsLegacyFormat);
        var session = stored.Value.Session;
        Assert.Equal("legacy-access-token", session.AccessToken);
        Assert.Equal("legacy-refresh-token", session.RefreshToken);
        Assert.Equal("bearer", session.TokenType);
        // Both feed the expiry: losing CreatedAt would make an expired token look fresh.
        Assert.Equal(3600, session.ExpiresIn);
        Assert.Equal(new DateTime(2026, 11, 10, 18, 0, 0, DateTimeKind.Utc), session.CreatedAt.ToUniversalTime());
    }

    [Fact]
    public void Read_LegacyFormat_KeepsTheUser()
    {
        var user = SessionStorageFormat.Read(LegacyJson)!.Value.Session.User;

        Assert.NotNull(user);
        Assert.Equal("6f1c2a4e-0000-4000-8000-000000000001", user.Id);
        Assert.Equal("t1@example.test", user.Email);
        Assert.Equal("google", Assert.Single(user.Identities).Provider);
        Assert.Equal("Test One", user.UserMetadata["full_name"]?.ToString());
    }

    [Fact]
    public void SerializeThenRead_RoundTripsInTheCurrentFormat()
    {
        var session = new GotrueSession
        {
            AccessToken = "access",
            RefreshToken = "refresh",
            ExpiresIn = 3600,
            CreatedAt = new DateTime(2026, 11, 10, 18, 0, 0, DateTimeKind.Utc),
        };

        var json = SessionStorageFormat.Serialize(session);
        var stored = SessionStorageFormat.Read(json);

        Assert.Contains("\"access_token\":\"access\"", json);
        Assert.NotNull(stored);
        Assert.False(stored.Value.IsLegacyFormat);
        Assert.Equal("access", stored.Value.Session.AccessToken);
        Assert.Equal("refresh", stored.Value.Session.RefreshToken);
        Assert.Equal(3600, stored.Value.Session.ExpiresIn);
        Assert.Equal(session.CreatedAt, stored.Value.Session.CreatedAt.ToUniversalTime());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"something\":\"else\"}")]
    public void Read_NothingUsable_ReturnsNull(string? json)
    {
        Assert.Null(SessionStorageFormat.Read(json));
    }
}
