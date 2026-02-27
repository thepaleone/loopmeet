using LoopMeet.Api.Contracts;
using LoopMeet.Core.Interfaces;
using LoopMeet.Core.Models;
using System.Net.Http.Headers;
using System.Text.Json;

namespace LoopMeet.Api.Services.Auth;

public sealed class UserProfileProjectionService
{
    private readonly IGroupRepository _groupRepository;
    private readonly ProfileAvatarResolver _avatarResolver;
    private readonly CurrentUserService _currentUser;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UserProfileProjectionService> _logger;
    private readonly string _supabaseUrl;
    private readonly string _anonKey;

    public UserProfileProjectionService(
        IGroupRepository groupRepository,
        ProfileAvatarResolver avatarResolver,
        CurrentUserService currentUser,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<UserProfileProjectionService> logger)
    {
        _groupRepository = groupRepository;
        _avatarResolver = avatarResolver;
        _currentUser = currentUser;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _supabaseUrl = ResolveConfigValue(configuration, "Supabase:Url", "SUPABASE__URL", "SUPABASE_URL");
        _anonKey = ResolveConfigValue(
            configuration,
            "Supabase:AnonKey",
            "SUPABASE__ANONKEY",
            "SUPABASE_ANONKEY",
            "SUPABASE_ANON_KEY");
    }

    public async Task<UserProfileResponse> BuildAsync(User user, CancellationToken cancellationToken = default)
    {
        var owned = await _groupRepository.ListOwnedAsync(user.Id, cancellationToken);
        var member = await _groupRepository.ListMemberAsync(user.Id, cancellationToken);
        var groupCount = owned
            .Select(group => group.Id)
            .Concat(member.Select(group => group.Id))
            .Distinct()
            .Count();

        var avatarUrl = _avatarResolver.ResolveEffectiveAvatarUrl(user);
        var hasEmailProvider = await ResolveHasEmailProviderAsync(user.Id, cancellationToken);
        return new UserProfileResponse
        {
            DisplayName = user.DisplayName,
            Email = user.Email,
            Phone = user.Phone,
            AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? null : avatarUrl,
            AvatarSource = _avatarResolver.ResolveSource(user),
            UserSince = user.CreatedAt,
            GroupCount = groupCount,
            CanChangePassword = true,
            HasEmailProvider = hasEmailProvider,
            RequiresCurrentPassword = hasEmailProvider,
            RequiresEmailForPasswordSetup = !hasEmailProvider
        };
    }

    private async Task<bool> ResolveHasEmailProviderAsync(Guid userId, CancellationToken cancellationToken)
    {
        var accessToken = _currentUser.AccessToken;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(_supabaseUrl) || string.IsNullOrWhiteSpace(_anonKey))
        {
            _logger.LogWarning(
                "Unable to resolve auth providers for {UserId} because Supabase configuration is missing.",
                userId);
            return true;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_supabaseUrl}/auth/v1/user");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Add("apikey", _anonKey);

            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Failed to resolve auth providers for {UserId}. Status: {Status}. Body: {Body}",
                    userId,
                    response.StatusCode,
                    body);
                return true;
            }

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return ContainsEmailProvider(json.RootElement);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error resolving auth providers for {UserId}", userId);
            return true;
        }
    }

    private static bool ContainsEmailProvider(JsonElement root)
    {
        if (root.TryGetProperty("identities", out var identities)
            && identities.ValueKind == JsonValueKind.Array)
        {
            foreach (var identity in identities.EnumerateArray())
            {
                if (identity.TryGetProperty("provider", out var providerElement)
                    && string.Equals(providerElement.GetString(), "email", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
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
                    return true;
                }
            }
        }

        return false;
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
