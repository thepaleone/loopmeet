using LoopMeet.App.Features.Auth.Models;
using SupabaseClient = Supabase.Client;

namespace LoopMeet.App.Features.Auth;

public sealed class AuthService
{
    private readonly SupabaseClient _client;

    public AuthService(SupabaseClient client)
    {
        _client = client;
    }

    public async Task<AuthSession> SignInWithEmailAsync(string email, string password)
    {
        var response = await _client.Auth.SignIn(email, password);
        return new AuthSession { AccessToken = response?.AccessToken ?? string.Empty };
    }

    public Task SignOutAsync()
    {
        return _client.Auth.SignOut();
    }

    public Task<AuthSession?> GetCurrentSessionAsync()
    {
        return Task.FromResult<AuthSession?>(null);
    }
}
