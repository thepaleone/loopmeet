using LoopMeet.App.Features.Auth.Session;
using GotrueSession = Supabase.Gotrue.Session;

namespace LoopMeet.App.Tests.Features.Auth.Session;

public sealed class SessionExpiryTests
{
    private static readonly DateTime IssuedUtc = new(2026, 11, 10, 18, 0, 0, DateTimeKind.Utc);

    private static GotrueSession Issued(DateTime createdAt, long expiresIn) =>
        new() { AccessToken = "access", RefreshToken = "refresh", CreatedAt = createdAt, ExpiresIn = expiresIn };

    [Fact]
    public void ExpiresAtUtc_IsIssueTimePlusLifetime()
    {
        Assert.Equal(
            new DateTimeOffset(2026, 11, 10, 19, 0, 0, TimeSpan.Zero),
            SessionExpiry.ExpiresAtUtc(Issued(IssuedUtc, 3600)));
    }

    [Fact]
    public void ExpiresAtUtc_ConvertsALocalIssueTime()
    {
        var local = IssuedUtc.ToLocalTime();

        Assert.Equal(
            new DateTimeOffset(2026, 11, 10, 19, 0, 0, TimeSpan.Zero),
            SessionExpiry.ExpiresAtUtc(Issued(local, 3600)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ExpiresAtUtc_WithoutALifetime_IsUnknown(long expiresIn)
    {
        Assert.Null(SessionExpiry.ExpiresAtUtc(Issued(IssuedUtc, expiresIn)));
    }

    [Fact]
    public void ExpiresAtUtc_WithoutAnIssueTime_IsUnknown()
    {
        Assert.Null(SessionExpiry.ExpiresAtUtc(Issued(default, 3600)));
    }

    [Fact]
    public void IsExpired_BeforeAndAfterTheExpiry()
    {
        var session = Issued(IssuedUtc, 3600);

        Assert.False(SessionExpiry.IsExpired(session, new DateTimeOffset(2026, 11, 10, 18, 59, 59, TimeSpan.Zero)));
        Assert.True(SessionExpiry.IsExpired(session, new DateTimeOffset(2026, 11, 10, 19, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void IsExpired_UnknownExpiry_CountsAsExpired()
    {
        // As Gotrue 6's Expired() did: a session that can't show it's valid gets renewed, not trusted.
        Assert.True(SessionExpiry.IsExpired(Issued(IssuedUtc, 0), new DateTimeOffset(IssuedUtc)));
    }
}
