using LoopMeet.App.Features.Auth.Session;

namespace LoopMeet.App.Tests.TestDoubles;

public sealed class FakeSessionTokenSource : ISessionTokenSource
{
    private readonly Queue<string?> _tokens = new();

    public RenewalOutcome RefreshOutcome { get; set; } = RenewalOutcome.Renewed;
    public int RefreshCalls { get; private set; }

    public FakeSessionTokenSource(params string?[] tokens)
    {
        foreach (var token in tokens)
        {
            _tokens.Enqueue(token);
        }
    }

    public string? GetAccessToken() => _tokens.Count > 1 ? _tokens.Dequeue() : _tokens.FirstOrDefault();

    public Task<RenewalOutcome> RefreshForRetryAsync()
    {
        RefreshCalls++;
        return Task.FromResult(RefreshOutcome);
    }
}
