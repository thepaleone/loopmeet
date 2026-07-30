using LoopMeet.App.Features.Auth.Session;

namespace LoopMeet.App.Tests.Features.Auth.Session;

public sealed class SessionRenewalPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 8, 12, 0, 0, TimeSpan.Zero);
    private readonly SessionRenewalPolicy _policy = new();

    [Fact]
    public void ExpiredToken_AlwaysAttempts()
    {
        Assert.True(_policy.ShouldAttempt(Now, lastSuccessUtc: Now.AddSeconds(-5), tokenExpiryUtc: Now.AddMinutes(-1)));
    }

    [Fact]
    public void UnknownExpiry_AlwaysAttempts()
    {
        Assert.True(_policy.ShouldAttempt(Now, lastSuccessUtc: Now.AddSeconds(-5), tokenExpiryUtc: null));
    }

    [Fact]
    public void NoPriorSuccess_Attempts()
    {
        Assert.True(_policy.ShouldAttempt(Now, lastSuccessUtc: null, tokenExpiryUtc: Now.AddHours(1)));
    }

    [Fact]
    public void WithinFinalFifthOfLifetime_Attempts()
    {
        // Last success (≈ issue time) 50min ago, expiry in 10min → 1h lifetime,
        // final fifth = 12min, and Now is inside it.
        Assert.True(_policy.ShouldAttempt(Now, lastSuccessUtc: Now.AddMinutes(-50), tokenExpiryUtc: Now.AddMinutes(10)));
    }

    [Fact]
    public void FreshCheckAndTokenFarFromExpiry_Skips()
    {
        // Checked 10s ago, token has 50 more minutes → inside debounce, outside final fifth.
        Assert.False(_policy.ShouldAttempt(Now, lastSuccessUtc: Now.AddSeconds(-10), tokenExpiryUtc: Now.AddMinutes(50)));
    }

    [Fact]
    public void OutsideDebounceWindow_Attempts()
    {
        Assert.True(_policy.ShouldAttempt(Now, lastSuccessUtc: Now - SessionRenewalPolicy.DebounceWindow, tokenExpiryUtc: Now.AddMinutes(50)));
    }

    [Fact]
    public void JustInsideDebounceWindow_Skips()
    {
        var lastSuccess = Now - SessionRenewalPolicy.DebounceWindow + TimeSpan.FromSeconds(1);

        Assert.False(_policy.ShouldAttempt(Now, lastSuccessUtc: lastSuccess, tokenExpiryUtc: Now.AddMinutes(50)));
    }

    [Fact]
    public void LastSuccessAfterExpiry_UsesFixedMargin()
    {
        // Degenerate clock data: falls back to the fixed 5-minute margin.
        Assert.True(_policy.ShouldAttempt(Now, lastSuccessUtc: Now.AddMinutes(10), tokenExpiryUtc: Now.AddMinutes(4)));
    }
}
