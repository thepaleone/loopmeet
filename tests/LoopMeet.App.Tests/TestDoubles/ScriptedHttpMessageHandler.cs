using System.Net;

namespace LoopMeet.App.Tests.TestDoubles;

/// <summary>
/// Returns a scripted sequence of status codes and records every request it
/// sees (with its bearer token and a buffered copy of its body).
/// </summary>
public sealed class ScriptedHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpStatusCode> _responses = new();

    public List<(string? BearerToken, string? Body)> Requests { get; } = new();

    public ScriptedHttpMessageHandler(params HttpStatusCode[] responses)
    {
        foreach (var response in responses)
        {
            _responses.Enqueue(response);
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request.Headers.Authorization?.Parameter, body));

        var status = _responses.Count > 0 ? _responses.Dequeue() : HttpStatusCode.OK;
        return new HttpResponseMessage(status) { RequestMessage = request };
    }
}
