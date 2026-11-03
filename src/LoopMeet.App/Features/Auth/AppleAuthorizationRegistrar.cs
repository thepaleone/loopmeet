using Microsoft.Extensions.Logging;
using Supabase.Functions;
using SupabaseClient = Supabase.Client;

namespace LoopMeet.App.Features.Auth;

/// <summary>
/// Hands Apple's one-time authorization code to the apple-authorization edge function, which
/// exchanges it for a refresh token that permanent deletion revokes (016 research D7). Never
/// throws: a failure is logged and the next Apple sign-in tries again.
/// </summary>
public sealed class AppleAuthorizationRegistrar
{
    private const string FunctionName = "apple-authorization";

    private readonly SupabaseClient _client;
    private readonly ILogger<AppleAuthorizationRegistrar> _logger;

    public AppleAuthorizationRegistrar(SupabaseClient client, ILogger<AppleAuthorizationRegistrar> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task RegisterAsync(string authorizationCode)
    {
        try
        {
            var accessToken = _client.Auth.CurrentSession?.AccessToken;
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                _logger.LogWarning("AppleAuthorizationSkipped: no session after Apple sign-in.");
                return;
            }

            await _client.Functions.Invoke(FunctionName, accessToken, new Client.InvokeFunctionOptions
            {
                Body = new Dictionary<string, object> { ["authorizationCode"] = authorizationCode }
            });
            _logger.LogInformation("AppleAuthorizationRegistered");
        }
        catch (Exception ex)
        {
            _logger.LogWarning("AppleAuthorizationFailed: {ExceptionType}", ex.GetType().Name);
        }
    }
}
