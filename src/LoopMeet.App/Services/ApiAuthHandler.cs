using System.Net.Http.Headers;
using LoopMeet.App.Features.Auth;

namespace LoopMeet.App.Services;

public sealed class ApiAuthHandler : DelegatingHandler
{
    private readonly AuthService _authService;

    public ApiAuthHandler(AuthService authService)
    {
        _authService = authService;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _authService.GetAccessToken();
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
