namespace LoopMeet.Api.Tests.Infrastructure;

/// <summary>
/// Clock the Api reads through <see cref="TimeProvider"/>, settable per test. It
/// does not drive <c>IMemoryCache</c> expiry, which stays on the real clock.
/// </summary>
public sealed class FixedTimeProvider : TimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow() => UtcNow;
}
