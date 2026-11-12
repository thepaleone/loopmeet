using System.Net;
using System.Text;
using LoopMeet.App.Features.Account;
using LoopMeet.App.Services;

namespace LoopMeet.App.Tests.Services;

public sealed class AccountStatusHandlerTests
{
    private const string PendingBody =
        "{\"code\":\"account_pending_deletion\",\"message\":\"This account is scheduled for deletion.\",\"deletesAt\":\"2026-12-03T18:00:00Z\"}";

    [Fact]
    public async Task PendingDeletion403RaisesTheSignalWithTheDate()
    {
        var (signal, raised) = Recording();

        await SendAsync(signal, HttpStatusCode.Forbidden, PendingBody);

        Assert.Equal([new DateTimeOffset(2026, 12, 3, 18, 0, 0, TimeSpan.Zero)], raised);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, "{\"code\":\"forbidden\",\"message\":\"Nope.\"}")]
    [InlineData(HttpStatusCode.Forbidden, "not json")]
    [InlineData(HttpStatusCode.Forbidden, "")]
    [InlineData(HttpStatusCode.Forbidden, "{\"code\":\"account_pending_deletion\"}")]
    [InlineData(HttpStatusCode.Unauthorized, PendingBody)]
    [InlineData(HttpStatusCode.OK, PendingBody)]
    public async Task OtherResponsesRaiseNothing(HttpStatusCode status, string body)
    {
        var (signal, raised) = Recording();

        await SendAsync(signal, status, body);

        Assert.Empty(raised);
    }

    [Fact]
    public async Task TheOriginalResponseIsReturnedWithItsBodyStillReadable()
    {
        var (signal, _) = Recording();
        var original = new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent(PendingBody, Encoding.UTF8, "application/json")
        };

        var returned = await SendAsync(signal, original);

        Assert.Same(original, returned);
        Assert.Equal(PendingBody, await returned.Content.ReadAsStringAsync());
    }

    [Theory]
    [InlineData(PendingBody, true)]
    [InlineData("{\"code\":\"invalid_confirmation\",\"message\":\"Type DELETE to confirm.\"}", false)]
    [InlineData(null, false)]
    [InlineData("<html>", false)]
    public void IsPendingDeletionReadsTheBodyCode(string? body, bool expected)
    {
        Assert.Equal(expected, AccountStatusHandler.IsPendingDeletion(body));
    }

    private static (AccountDeletionSignal Signal, List<DateTimeOffset> Raised) Recording()
    {
        var signal = new AccountDeletionSignal();
        var raised = new List<DateTimeOffset>();
        signal.PendingDeletion += raised.Add;
        return (signal, raised);
    }

    private static Task<HttpResponseMessage> SendAsync(AccountDeletionSignal signal, HttpStatusCode status, string body) =>
        SendAsync(signal, new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });

    private static async Task<HttpResponseMessage> SendAsync(AccountDeletionSignal signal, HttpResponseMessage response)
    {
        var handler = new AccountStatusHandler(signal) { InnerHandler = new StubHandler(response) };
        using var invoker = new HttpMessageInvoker(handler);
        return await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "https://api.example/groups"), CancellationToken.None);
    }

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }
}
