using LoopMeet.App.Features.Auth.Session;
using Supabase.Gotrue.Exceptions;

namespace LoopMeet.App.Tests.Features.Auth.Session;

public sealed class SessionFailureClassifierTests
{
    [Theory]
    [InlineData(FailureHint.Reason.ExpiredRefreshToken)]
    [InlineData(FailureHint.Reason.InvalidRefreshToken)]
    [InlineData(FailureHint.Reason.NoSessionFound)]
    public void GotrueRejectionReasons_AreDefinitive(FailureHint.Reason reason)
    {
        var exception = new GotrueException("rejected", reason);

        Assert.Equal(SessionFailureKind.Definitive, SessionFailureClassifier.Classify(exception));
    }

    [Fact]
    public void GotrueOffline_IsTransient()
    {
        var exception = new GotrueException("offline", FailureHint.Reason.Offline);

        Assert.Equal(SessionFailureKind.Transient, SessionFailureClassifier.Classify(exception));
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    public void GotrueTokenEndpointClientErrors_AreDefinitive(int statusCode)
    {
        var exception = WithStatusCode(new GotrueException("rejected", FailureHint.Reason.Unknown), statusCode);

        Assert.Equal(SessionFailureKind.Definitive, SessionFailureClassifier.Classify(exception));
    }

    [Theory]
    [InlineData(500)]
    [InlineData(502)]
    [InlineData(503)]
    public void GotrueServerErrors_AreTransient(int statusCode)
    {
        var exception = WithStatusCode(new GotrueException("server error", FailureHint.Reason.Unknown), statusCode);

        Assert.Equal(SessionFailureKind.Transient, SessionFailureClassifier.Classify(exception));
    }

    // StatusCode has no public setter (Gotrue assigns it internally from the
    // HTTP response), so tests populate it the same way via reflection.
    private static GotrueException WithStatusCode(GotrueException exception, int statusCode)
    {
        var property = typeof(GotrueException).GetProperty(nameof(GotrueException.StatusCode))!;
        property.SetValue(exception, statusCode);
        return exception;
    }

    [Fact]
    public void HttpRequestException_IsTransient()
    {
        Assert.Equal(SessionFailureKind.Transient, SessionFailureClassifier.Classify(new HttpRequestException("no network")));
    }

    [Fact]
    public void Timeout_IsTransient()
    {
        Assert.Equal(SessionFailureKind.Transient, SessionFailureClassifier.Classify(new TaskCanceledException("timed out")));
    }

    [Fact]
    public void UnknownException_FailsSafeAsTransient()
    {
        // FR-004a: never force a sign-out on ambiguity.
        Assert.Equal(SessionFailureKind.Transient, SessionFailureClassifier.Classify(new InvalidOperationException("anything")));
    }
}
