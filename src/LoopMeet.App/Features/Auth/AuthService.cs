using System.Text;
using System.Text.Json;
using LoopMeet.App.Features.Auth.Models;
using Microsoft.Maui.Authentication;
using Supabase.Gotrue;
using Microsoft.Maui.Storage;
using SupabaseClient = Supabase.Client;

namespace LoopMeet.App.Features.Auth;

public sealed class AuthService
{
    private const string AccessTokenKey = "loopmeet.auth.access_token";
    private const string OAuthRedirectUri = "loopmeet://auth-callback";
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
        SaveAccessToken(_accessToken);
        return new AuthSession { AccessToken = _accessToken ?? string.Empty };
    }

    public async Task<AuthSession> SignUpWithEmailAsync(string email, string password)
    {
        try{
            var response = await _client.Auth.SignUp(email, password);
            _accessToken = response?.AccessToken;
            SaveAccessToken(_accessToken);
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
        Preferences.Default.Remove(AccessTokenKey);
        return _client.Auth.SignOut();
    }

    public async Task<OAuthSignInResult> SignInWithGoogleAsync()
    {
        var authState = await _client.Auth.SignIn(Constants.Provider.Google, new SignInOptions
        {
            FlowType = Constants.OAuthFlowType.PKCE,
            RedirectTo = OAuthRedirectUri
        });

        var result = await WebAuthenticator.AuthenticateAsync(authState.Uri, new Uri(OAuthRedirectUri));
        if (!result.Properties.TryGetValue("code", out var authCode) || string.IsNullOrWhiteSpace(authCode))
        {
            return new OAuthSignInResult();
        }

        var session = await _client.Auth.ExchangeCodeForSession(authState.PKCEVerifier ?? string.Empty, authCode);
        _accessToken = session?.AccessToken;
        SaveAccessToken(_accessToken);

        var user = session?.User;
        return new OAuthSignInResult
        {
            AccessToken = _accessToken ?? string.Empty,
            DisplayName = GetUserDisplayName(user),
            Email = user?.Email ?? TryGetJwtClaim(_accessToken, "email"),
            Phone = user?.Phone
        };
    }

    public Task<AuthSession?> GetCurrentSessionAsync()
    {
        var session = _client.Auth.CurrentSession;
        if (session is null || session.Expired())
        {
            return Task.FromResult<AuthSession?>(null);
        }

        var token = session.AccessToken ?? _accessToken;
        if (string.IsNullOrWhiteSpace(token))
        {
            return Task.FromResult<AuthSession?>(null);
        }

        _accessToken = token;
        SaveAccessToken(_accessToken);
        return Task.FromResult<AuthSession?>(new AuthSession { AccessToken = token });
    }

    public async Task<AuthSession?> RestoreSessionAsync()
    {
        await _client.InitializeAsync();

        var session = _client.Auth.CurrentSession;
        if (session is null || session.Expired())
        {
            if (session is not null)
            {
                await _client.Auth.SignOut();
            }

            var cachedToken = Preferences.Default.Get(AccessTokenKey, string.Empty);
            if (!string.IsNullOrWhiteSpace(cachedToken) && !IsJwtExpired(cachedToken))
            {
                _accessToken = cachedToken;
                return new AuthSession { AccessToken = _accessToken };
            }

            Preferences.Default.Remove(AccessTokenKey);
            return null;
        }

        _accessToken = session.AccessToken;
        if (string.IsNullOrWhiteSpace(_accessToken))
        {
            return null;
        }

        SaveAccessToken(_accessToken);
        return new AuthSession { AccessToken = _accessToken };
    }

    public string? GetAccessToken()
    {
        return _client.Auth.CurrentSession?.AccessToken ?? _accessToken;
    }

    public Guid? GetCurrentUserId()
    {
        var token = GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var sub = TryGetJwtSubject(token);
        return Guid.TryParse(sub, out var userId) ? userId : null;
    }

    private static void SaveAccessToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        Preferences.Default.Set(AccessTokenKey, token);
    }

    private static bool IsJwtExpired(string token)
    {
        var exp = TryGetJwtExpiry(token);
        if (exp is null)
        {
            return true;
        }

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return exp.Value <= now;
    }

    private static long? TryGetJwtExpiry(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        var payload = parts[1]
            .Replace('-', '+')
            .Replace('_', '/');

        switch (payload.Length % 4)
        {
            case 2:
                payload += "==";
                break;
            case 3:
                payload += "=";
                break;
        }

        try
        {
            var bytes = Convert.FromBase64String(payload);
            using var json = JsonDocument.Parse(bytes);
            if (json.RootElement.TryGetProperty("exp", out var expElement)
                && expElement.TryGetInt64(out var exp))
            {
                return exp;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static string? TryGetJwtSubject(string token)
    {
        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        var payload = parts[1]
            .Replace('-', '+')
            .Replace('_', '/');

        switch (payload.Length % 4)
        {
            case 2:
                payload += "==";
                break;
            case 3:
                payload += "=";
                break;
        }

        try
        {
            var bytes = Convert.FromBase64String(payload);
            using var json = JsonDocument.Parse(bytes);
            if (json.RootElement.TryGetProperty("sub", out var subElement))
            {
                return subElement.GetString();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static string? GetUserDisplayName(User? user)
    {
        if (user?.UserMetadata is null || user.UserMetadata.Count == 0)
        {
            return null;
        }

        if (TryGetMetadataValue(user.UserMetadata, "full_name", out var fullName))
        {
            return fullName;
        }

        if (TryGetMetadataValue(user.UserMetadata, "name", out var name))
        {
            return name;
        }

        if (TryGetMetadataValue(user.UserMetadata, "given_name", out var givenName)
            && TryGetMetadataValue(user.UserMetadata, "family_name", out var familyName))
        {
            return $"{givenName} {familyName}".Trim();
        }

        return null;
    }

    private static bool TryGetMetadataValue(Dictionary<string, object> metadata, string key, out string value)
    {
        value = string.Empty;
        if (!metadata.TryGetValue(key, out var raw) || raw is null)
        {
            return false;
        }

        switch (raw)
        {
            case string text:
                value = text;
                return !string.IsNullOrWhiteSpace(value);
            case JsonElement element when element.ValueKind == JsonValueKind.String:
                value = element.GetString() ?? string.Empty;
                return !string.IsNullOrWhiteSpace(value);
            default:
                value = raw.ToString() ?? string.Empty;
                return !string.IsNullOrWhiteSpace(value);
        }
    }

    private static string? TryGetJwtClaim(string? token, string claim)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var parts = token.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        var payload = parts[1]
            .Replace('-', '+')
            .Replace('_', '/');

        switch (payload.Length % 4)
        {
            case 2:
                payload += "==";
                break;
            case 3:
                payload += "=";
                break;
        }

        try
        {
            var bytes = Convert.FromBase64String(payload);
            using var json = JsonDocument.Parse(bytes);
            if (json.RootElement.TryGetProperty(claim, out var claimElement))
            {
                return claimElement.GetString();
            }
        }
        catch
        {
            return null;
        }

        return null;
    }
}
