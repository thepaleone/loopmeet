using LoopMeet.App.Features.Auth.Models;
using Supabase;
using Supabase.Gotrue;

namespace LoopMeet.App.Features.Auth;

public sealed class AuthService
{
    private readonly Client _client;

    public AuthService(Client client)
    {
        _client = client;
    }

    public async Task<AuthSession> SignInWithEmailAsync(string email, string password)
    {
        var response = await _client.Auth.SignIn(email, password);
        return new AuthSession { AccessToken = response.AccessToken ?? string.Empty };
    }

    public Task SignOutAsync()
    {
        return _client.Auth.SignOut();
    }

    public async Task<AuthSession?> GetCurrentSessionAsync()
    {
        var session = await _client.Auth.GetSession();
        if (session is null)
        {
            return null;
        }

        return new AuthSession { AccessToken = session.AccessToken ?? string.Empty };
    }
}
