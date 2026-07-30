using System.Net;
using System.Net.Http.Headers;
using LoopMeet.App.Features.Auth.Session;

namespace LoopMeet.App.Services;

/// <summary>
/// The single 401 authority (contract §5): attaches the bearer token, and on an
/// unauthorized response asks the coordinator for one renewal and retries the
/// request exactly once. Never navigates, never clears state — routing on
/// definitive rejection belongs to the SessionCoordinator (INV-2).
/// </summary>
public sealed class ApiAuthHandler : DelegatingHandler
{
    private static readonly HttpRequestOptionsKey<bool> RetriedKey = new("loopmeet.auth.retried");

    private readonly ISessionTokenSource _tokenSource;

    public ApiAuthHandler(ISessionTokenSource tokenSource)
    {
        _tokenSource = tokenSource;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Attach(request, _tokenSource.GetAccessToken());
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized
            || (request.Options.TryGetValue(RetriedKey, out var retried) && retried))
        {
            return response;
        }

        var outcome = await _tokenSource.RefreshForRetryAsync();
        if (outcome is not (RenewalOutcome.Renewed or RenewalOutcome.StillValid))
        {
            return response;
        }

        var token = _tokenSource.GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return response;
        }

        var retry = await CloneAsync(request);
        retry.Options.Set(RetriedKey, true);
        Attach(retry, token);
        response.Dispose();
        return await base.SendAsync(retry, cancellationToken);
    }

    private static void Attach(HttpRequestMessage request, string? token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    // An HttpRequestMessage cannot be sent twice; rebuild it, buffering the content.
    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        foreach (var option in request.Options)
        {
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        }

        if (request.Content is not null)
        {
            var buffered = new ByteArrayContent(await request.Content.ReadAsByteArrayAsync());
            foreach (var header in request.Content.Headers)
            {
                buffered.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            clone.Content = buffered;
        }

        return clone;
    }
}
