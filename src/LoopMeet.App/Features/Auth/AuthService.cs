using System.Text.Json;
using LoopMeet.App.Features.Auth.Models;
using LoopMeet.App.Features.Auth.Session;
using Microsoft.Maui.Authentication;
using Supabase.Gotrue;
using SupabaseClient = Supabase.Client;

namespace LoopMeet.App.Features.Auth;

public sealed class AuthService
{
    private const string OAuthRedirectUri = "loopmeet://auth-callback";
    private readonly SupabaseClient _client;
    private readonly ISessionTokenSource _tokenSource;

    public AuthService(SupabaseClient client, ISessionTokenSource tokenSource)
    {
        _client = client;
        _tokenSource = tokenSource;
    }

    public async Task<AuthSession> SignInWithEmailAsync(string email, string password)
    {
        var response = await _client.Auth.SignIn(email, password);
        return new AuthSession { AccessToken = response?.AccessToken ?? string.Empty };
    }

    public async Task<AuthSession> SignUpWithEmailAsync(string email, string password)
    {
        try{
            var response = await _client.Auth.SignUp(email, password);
            return new AuthSession { AccessToken = response?.AccessToken ?? string.Empty };
        }
        catch (Exception ex)
        {
            // Handle exceptions (e.g., user already exists, network issues)
            throw new InvalidOperationException("Failed to sign up with email.", ex);
        }
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
        var accessToken = session?.AccessToken;

        var user = session?.User;
        return new OAuthSignInResult
        {
            AccessToken = accessToken ?? string.Empty,
            DisplayName = GetUserDisplayName(user),
            Email = user?.Email ?? TryGetJwtClaim(accessToken, "email"),
            Phone = user?.Phone,
            AvatarUrl = GetUserAvatarUrl(user)
        };
    }

    public async Task<OAuthSignInResult> SignInWithAppleAsync()
    {
#if IOS || MACCATALYST
        var (rawNonce, hashedNonce) = AppleAuthNonce.Generate();
        var credential = await Platforms.Apple.AppleAuthCredentialProvider.RequestCredentialAsync(hashedNonce);

        if (credential is null)
        {
            return new OAuthSignInResult();
        }

        var idTokenBytes = credential.IdentityToken;
        if (idTokenBytes is null)
        {
            return new OAuthSignInResult();
        }

        var idToken = System.Text.Encoding.UTF8.GetString(idTokenBytes.ToArray());

        var session = await _client.Auth.SignInWithIdToken(Constants.Provider.Apple, idToken, rawNonce);
        var accessToken = session?.AccessToken;

        var user = session?.User;
        var fullName = BuildAppleDisplayName(credential);
        return new OAuthSignInResult
        {
            AccessToken = accessToken ?? string.Empty,
            DisplayName = fullName ?? GetUserDisplayName(user),
            Email = credential.Email ?? user?.Email ?? TryGetJwtClaim(accessToken, "email"),
            Phone = null,
            AvatarUrl = null
        };
#else
        await Task.CompletedTask;
        throw new PlatformNotSupportedException("Sign in with Apple is only available on iOS and MacCatalyst.");
#endif
    }

#if IOS || MACCATALYST
    private static string? BuildAppleDisplayName(AuthenticationServices.ASAuthorizationAppleIdCredential credential)
    {
        var given = credential.FullName?.GivenName;
        var family = credential.FullName?.FamilyName;
        if (string.IsNullOrWhiteSpace(given) && string.IsNullOrWhiteSpace(family))
        {
            return null;
        }

        return $"{given} {family}".Trim();
    }
#endif

    // Identity reads (GetCurrentUserId → IsOwner checks) must always agree with
    // the token the ApiAuthHandler sends — both flow through the coordinator.
    public string? GetAccessToken()
    {
        return _tokenSource.GetAccessToken();
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

    private static string? GetUserAvatarUrl(User? user)
    {
        if (user?.UserMetadata is null || user.UserMetadata.Count == 0)
        {
            return null;
        }

        if (TryGetMetadataValue(user.UserMetadata, "avatar_url", out var avatarUrl))
        {
            return avatarUrl;
        }

        if (TryGetMetadataValue(user.UserMetadata, "picture", out var picture))
        {
            return picture;
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
