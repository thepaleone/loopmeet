using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LoopMeet.App.Features.Auth;
using Microsoft.Extensions.Logging;

namespace LoopMeet.App.Services.Notifications;

public sealed class DeviceRegistrationService
{
    private readonly AppConfig _config;
    private readonly AuthService _authService;
    private readonly ILogger<DeviceRegistrationService> _logger;

    public DeviceRegistrationService(
        AppConfig config,
        AuthService authService,
        ILogger<DeviceRegistrationService> logger)
    {
        _config = config;
        _authService = authService;
        _logger = logger;
    }

    public async Task SyncPermissionStateAsync(Guid userId, NotificationPermissionState state)
    {
        var token = _authService.GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Device registration skipped for {UserId}: no access token available yet.", userId);
            return;
        }

        var deviceTimezone = TimezoneHelper.GetCurrentDeviceTimezoneId();
        var platform = DeviceInfo.Current.Platform.ToString().ToLowerInvariant();
        var permission = state.ToString().ToLowerInvariant();

        using var http = new HttpClient();

        // Look up an existing active row for this user+platform. RLS scopes the
        // result to the caller's own user_id, so this is just per-device dedupe.
        var existingId = await GetExistingDeviceRowIdAsync(http, token, userId, platform);

        if (existingId is not null)
        {
            await PatchDeviceRowAsync(http, token, existingId.Value, new
            {
                permission_state = permission,
                notifications_enabled = state == NotificationPermissionState.Granted,
                device_timezone = deviceTimezone,
                last_seen_at = DateTimeOffset.UtcNow,
                updated_at = DateTimeOffset.UtcNow,
                invalidated_at = (DateTimeOffset?)null
            }, userId);
        }
        else
        {
            await InsertDeviceRowAsync(http, token, new
            {
                user_id = userId,
                onesignal_external_id = userId.ToString(),
                platform,
                permission_state = permission,
                notifications_enabled = state == NotificationPermissionState.Granted,
                device_timezone = deviceTimezone,
                last_seen_at = DateTimeOffset.UtcNow
            }, userId);
        }
    }

    public async Task SyncUserProfileTimezoneAsync(Guid userId)
    {
        var token = _authService.GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("User profile timezone sync skipped for {UserId}: no access token.", userId);
            return;
        }

        using var http = new HttpClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"{_config.SupabaseUrl}/rest/v1/user_profiles?id=eq.{userId}");

        request.Headers.Add("apikey", _config.SupabaseAnonOrPublisableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Prefer", "return=minimal");

        var payload = new
        {
            timezone = TimezoneHelper.GetCurrentDeviceTimezoneId(),
            updated_at = DateTimeOffset.UtcNow
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "User profile timezone PATCH failed for {UserId}. Status={Status} Body={Body}",
                userId, (int)response.StatusCode, body);
        }
    }

    private async Task<Guid?> GetExistingDeviceRowIdAsync(HttpClient http, string token, Guid userId, string platform)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_config.SupabaseUrl}/rest/v1/user_devices?user_id=eq.{userId}&platform=eq.{platform}&invalidated_at=is.null&select=id&limit=1");

        request.Headers.Add("apikey", _config.SupabaseAnonOrPublisableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "user_devices lookup failed for {UserId}/{Platform}. Status={Status} Body={Body}",
                userId, platform, (int)response.StatusCode, body);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
        {
            return null;
        }

        if (doc.RootElement[0].TryGetProperty("id", out var idElement) &&
            idElement.ValueKind == JsonValueKind.String &&
            Guid.TryParse(idElement.GetString(), out var id))
        {
            return id;
        }

        return null;
    }

    private async Task InsertDeviceRowAsync(HttpClient http, string token, object payload, Guid userId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_config.SupabaseUrl}/rest/v1/user_devices");
        request.Headers.Add("apikey", _config.SupabaseAnonOrPublisableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Prefer", "return=minimal");

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "user_devices INSERT failed for {UserId}. Status={Status} Body={Body}",
                userId, (int)response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }
        else
        {
            _logger.LogInformation("user_devices row inserted for {UserId}.", userId);
        }
    }

    private async Task PatchDeviceRowAsync(HttpClient http, string token, Guid rowId, object payload, Guid userId)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"{_config.SupabaseUrl}/rest/v1/user_devices?id=eq.{rowId}");

        request.Headers.Add("apikey", _config.SupabaseAnonOrPublisableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Prefer", "return=minimal");

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError(
                "user_devices PATCH failed for {UserId} row={RowId}. Status={Status} Body={Body}",
                userId, rowId, (int)response.StatusCode, body);
            response.EnsureSuccessStatusCode();
        }
        else
        {
            _logger.LogInformation("user_devices row {RowId} updated for {UserId}.", rowId, userId);
        }
    }
}
