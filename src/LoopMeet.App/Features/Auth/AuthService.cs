using LoopMeet.App.Features.Auth.Models;
using SupabaseClient = Supabase.Client;

namespace LoopMeet.App.Features.Auth;

public sealed class AuthService
{
    private readonly SupabaseClient _client;
    private string? _accessToken;

    public AuthService(SupabaseClient client)
    {
        _client = client;
    }

    public async Task<AuthSession> SignInWithEmailAsync(string email, string password)
    {
        var response = await _client.Auth.SignIn(email, password);
        _accessToken = response?.AccessToken;
        return new AuthSession { AccessToken = _accessToken ?? string.Empty };
    }

    public async Task<AuthSession> SignUpWithEmailAsync(string email, string password)
    {
        try{
            var response = await _client.Auth.SignUp(email, password);
            _accessToken = response?.AccessToken;
            return new AuthSession { AccessToken = _accessToken ?? string.Empty };
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., user already exists, network issues)
            throw new InvalidOperationException("Failed to sign up with email.", ex);
        }
    }

    public Task SignOutAsync()
    {
        _accessToken = null;
        return _client.Auth.SignOut();
    }

    public Task<AuthSession?> GetCurrentSessionAsync()
    {
        var token = _client.Auth.CurrentSession?.AccessToken ?? _accessToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult<AuthSession?>(null);
        }

        return Task.FromResult<AuthSession?>(new AuthSession { AccessToken = token });
    }

    public string? GetAccessToken()
    {
        return _client.Auth.CurrentSession?.AccessToken ?? _accessToken;
    }
}
