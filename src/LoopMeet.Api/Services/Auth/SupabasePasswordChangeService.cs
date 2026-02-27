using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace LoopMeet.Api.Services.Auth;

public interface IPasswordChangeService
{
    Task<PasswordChangeResult> ChangePasswordAsync(
        Guid userId,
        string? requestedEmail,
        string? profileEmail,
        string? claimEmail,
        string? accessToken,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default);
}

public sealed class SupabasePasswordChangeService : IPasswordChangeService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SupabasePasswordChangeService> _logger;
    private readonly string _supabaseUrl;
    private readonly string _anonKey;

    public SupabasePasswordChangeService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<SupabasePasswordChangeService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _supabaseUrl = ResolveConfigValue(configuration,
            "Supabase:Url",
            "SUPABASE__URL",
            "SUPABASE_URL");
        _anonKey = ResolveConfigValue(configuration,
            "Supabase:AnonKey",
            "SUPABASE__ANONKEY",
            "SUPABASE_ANONKEY",
            "SUPABASE_ANON_KEY");
    }

    public async Task<PasswordChangeResult> ChangePasswordAsync(
        Guid userId,
        string? requestedEmail,
        string? profileEmail,
        string? claimEmail,
        string? accessToken,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var hasEmailFromRequest = !string.IsNullOrWhiteSpace(requestedEmail);
        var hasEmailFromProfile = !string.IsNullOrWhiteSpace(profileEmail);
        var hasEmailFromClaim = !string.IsNullOrWhiteSpace(claimEmail);

        if (string.IsNullOrWhiteSpace(_supabaseUrl)
            || string.IsNullOrWhiteSpace(_anonKey)
            || string.IsNullOrWhiteSpace(accessToken))
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(_supabaseUrl))
            {
                missing.Add("Supabase Url");
            }
            if (string.IsNullOrWhiteSpace(_anonKey))
            {
                missing.Add("Supabase Anon Key");
            }
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                missing.Add("User Access Token");
            }

            _logger.LogError(
                "Supabase configuration missing required auth values for password change: {MissingValues}",
                string.Join(", ", missing));
            return PasswordChangeResult.Failure(
                PasswordChangeFailureReason.ServiceNotConfigured,
                "Password service is not configured.",
                hasEmailFromRequest,
                hasEmailFromProfile,
                hasEmailFromClaim);
        }

        var identityCheck = await GetCurrentUserIdentityAsync(userId, accessToken, cancellationToken);
        if (!identityCheck.Success)
        {
            _logger.LogWarning(
                "Password change failed identity lookup for {UserId}. Reason: {ReasonCode}",
                userId,
                PasswordChangeFailureReason.IdentityLookupFailed);
            return PasswordChangeResult.Failure(
                PasswordChangeFailureReason.IdentityLookupFailed,
                identityCheck.Error ?? "Unable to verify account identity providers.",
                hasEmailFromRequest,
                hasEmailFromProfile,
                hasEmailFromClaim,
                hasEmailFromAuthUser: !string.IsNullOrWhiteSpace(identityCheck.AuthUserEmail),
                hasEmailIdentity: identityCheck.HasEmailIdentity);
        }

        var emailResolution = ResolveEmail(
            requestedEmail,
            profileEmail,
            claimEmail,
            identityCheck.AuthUserEmail);

        if (string.IsNullOrWhiteSpace(emailResolution.Email))
        {
            var missingEmailMessage = identityCheck.HasEmailIdentity
                ? "Enter your account email to verify your current password."
                : "Enter your account email to set your password.";
            _logger.LogWarning(
                "Password change failed for {UserId}. Reason: {ReasonCode}. HasEmailIdentity: {HasEmailIdentity}",
                userId,
                PasswordChangeFailureReason.MissingEmail,
                identityCheck.HasEmailIdentity);
            return PasswordChangeResult.Failure(
                PasswordChangeFailureReason.MissingEmail,
                missingEmailMessage,
                emailResolution.HasEmailFromRequest,
                emailResolution.HasEmailFromProfile,
                emailResolution.HasEmailFromClaim,
                emailResolution.HasEmailFromAuthUser,
                identityCheck.HasEmailIdentity);
        }

        if (identityCheck.HasEmailIdentity)
        {
            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                return PasswordChangeResult.Failure(
                    PasswordChangeFailureReason.MissingFields,
                    "Current password is required.",
                    emailResolution.HasEmailFromRequest,
                    emailResolution.HasEmailFromProfile,
                    emailResolution.HasEmailFromClaim,
                    emailResolution.HasEmailFromAuthUser,
                    identityCheck.HasEmailIdentity);
            }

            var currentPasswordVerification = await VerifyCurrentPasswordAsync(
                emailResolution.Email,
                currentPassword,
                cancellationToken);
            if (!currentPasswordVerification.IsSuccess)
            {
                _logger.LogWarning(
                    "Password change failed during password verification for {UserId}. Reason: {ReasonCode}",
                    userId,
                    currentPasswordVerification.FailureReason);
                return PasswordChangeResult.Failure(
                    currentPasswordVerification.FailureReason,
                    currentPasswordVerification.Error ?? "Unable to verify current password.",
                    emailResolution.HasEmailFromRequest,
                    emailResolution.HasEmailFromProfile,
                    emailResolution.HasEmailFromClaim,
                    emailResolution.HasEmailFromAuthUser,
                    identityCheck.HasEmailIdentity);
            }

            if (!currentPasswordVerification.IsCurrentPasswordValid)
            {
                _logger.LogWarning(
                    "Password change rejected for {UserId}. Reason: {ReasonCode}",
                    userId,
                    PasswordChangeFailureReason.CurrentPasswordInvalid);
                return PasswordChangeResult.Failure(
                    PasswordChangeFailureReason.CurrentPasswordInvalid,
                    "Current password is incorrect.",
                    emailResolution.HasEmailFromRequest,
                    emailResolution.HasEmailFromProfile,
                    emailResolution.HasEmailFromClaim,
                    emailResolution.HasEmailFromAuthUser,
                    identityCheck.HasEmailIdentity);
            }
        }

        var updateResult = await UpdatePasswordAsync(
            userId,
            identityCheck.AuthUserEmail,
            emailResolution.Email,
            accessToken,
            newPassword,
            includeEmail: !identityCheck.HasEmailIdentity,
            cancellationToken);

        return updateResult with
        {
            HasEmailFromRequest = emailResolution.HasEmailFromRequest,
            HasEmailFromProfile = emailResolution.HasEmailFromProfile,
            HasEmailFromClaim = emailResolution.HasEmailFromClaim,
            HasEmailFromAuthUser = emailResolution.HasEmailFromAuthUser,
            HasEmailIdentity = identityCheck.HasEmailIdentity
        };
    }

    private async Task<(bool Success, bool HasEmailIdentity, string? AuthUserEmail, string? Error)> GetCurrentUserIdentityAsync(
        Guid userId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_supabaseUrl}/auth/v1/user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("apikey", _anonKey);

        var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Failed to inspect current user identities from Supabase for {UserId}. Status: {Status}. Body: {Body}",
                userId,
                response.StatusCode,
                errorBody);
            return (false, false, null, "Unable to inspect identity providers.");
        }

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = json.RootElement;
        var authUserEmail = root.TryGetProperty("email", out var emailElement)
            ? emailElement.GetString()
            : null;

        if (root.TryGetProperty("identities", out var identities)
            && identities.ValueKind == JsonValueKind.Array)
        {
            foreach (var identity in identities.EnumerateArray())
            {
                if (identity.TryGetProperty("provider", out var providerElement)
                    && string.Equals(providerElement.GetString(), "email", StringComparison.OrdinalIgnoreCase))
                {
                    return (true, true, authUserEmail, null);
                }
            }
        }

        if (root.TryGetProperty("app_metadata", out var appMetadata)
            && appMetadata.TryGetProperty("providers", out var providers)
            && providers.ValueKind == JsonValueKind.Array)
        {
            foreach (var provider in providers.EnumerateArray())
            {
                if (string.Equals(provider.GetString(), "email", StringComparison.OrdinalIgnoreCase))
                {
                    return (true, true, authUserEmail, null);
                }
            }
        }

        return (true, false, authUserEmail, null);
    }

    private async Task<(bool IsSuccess, bool IsCurrentPasswordValid, PasswordChangeFailureReason FailureReason, string? Error)> VerifyCurrentPasswordAsync(
        string email,
        string currentPassword,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, $"{_supabaseUrl}/auth/v1/token?grant_type=password");
        request.Headers.Add("apikey", _anonKey);
        request.Content = JsonContent.Create(new
        {
            email,
            password = currentPassword
        });

        var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return (true, true, PasswordChangeFailureReason.None, null);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest || response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return (true, false, PasswordChangeFailureReason.CurrentPasswordInvalid, "Current password is incorrect.");
        }

        var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning(
            "Unexpected response while verifying current password for {Email}. Status: {Status}. Body: {Body}",
            email,
            response.StatusCode,
            errorBody);
        return (
            false,
            false,
            PasswordChangeFailureReason.SupabaseUnexpectedError,
            "Unable to verify current password.");
    }

    private async Task<PasswordChangeResult> UpdatePasswordAsync(
        Guid userId,
        string? authUserEmail,
        string? resolvedEmail,
        string accessToken,
        string newPassword,
        bool includeEmail,
        CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["password"] = newPassword
        };

        var shouldSetEmail = includeEmail
            && !string.IsNullOrWhiteSpace(resolvedEmail)
            && !string.Equals(authUserEmail, resolvedEmail, StringComparison.OrdinalIgnoreCase);

        if (shouldSetEmail)
        {
            payload["email"] = resolvedEmail;
        }

        var client = _httpClientFactory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Put, $"{_supabaseUrl}/auth/v1/user");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("apikey", _anonKey);
        request.Content = JsonContent.Create(payload);

        var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            return PasswordChangeResult.Success();
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogError(
            "Supabase password update failed for {UserId}. Status: {Status}. Body: {Body}",
            userId,
            response.StatusCode,
            body);
        if ((int)response.StatusCode >= 500)
        {
            return PasswordChangeResult.Failure(
                PasswordChangeFailureReason.SupabaseUnexpectedError,
                "Unable to update password.");
        }

        return PasswordChangeResult.Failure(
            PasswordChangeFailureReason.SupabaseUpdateFailed,
            "Unable to update password.");
    }

    private static (string? Email, bool HasEmailFromRequest, bool HasEmailFromProfile, bool HasEmailFromClaim, bool HasEmailFromAuthUser) ResolveEmail(
        string? requestedEmail,
        string? profileEmail,
        string? claimEmail,
        string? authUserEmail)
    {
        var hasEmailFromRequest = !string.IsNullOrWhiteSpace(requestedEmail);
        var hasEmailFromProfile = !string.IsNullOrWhiteSpace(profileEmail);
        var hasEmailFromClaim = !string.IsNullOrWhiteSpace(claimEmail);
        var hasEmailFromAuthUser = !string.IsNullOrWhiteSpace(authUserEmail);

        if (hasEmailFromRequest)
        {
            return (requestedEmail!.Trim(), true, hasEmailFromProfile, hasEmailFromClaim, hasEmailFromAuthUser);
        }

        if (hasEmailFromProfile)
        {
            return (profileEmail!.Trim(), false, true, hasEmailFromClaim, hasEmailFromAuthUser);
        }

        if (hasEmailFromClaim)
        {
            return (claimEmail!.Trim(), false, false, true, hasEmailFromAuthUser);
        }

        if (hasEmailFromAuthUser)
        {
            return (authUserEmail!.Trim(), false, false, false, true);
        }

        return (null, false, false, false, false);
    }

    private static string ResolveConfigValue(IConfiguration configuration, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = configuration[key];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }
}
