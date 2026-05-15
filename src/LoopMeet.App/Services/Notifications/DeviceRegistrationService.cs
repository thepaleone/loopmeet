using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LoopMeet.App.Features.Auth;

namespace LoopMeet.App.Services.Notifications;

public sealed class DeviceRegistrationService
{
    private readonly AppConfig _config;
    private readonly AuthService _authService;

    public DeviceRegistrationService(AppConfig config, AuthService authService)
    {
        _config = config;
        _authService = authService;
    }

    public async Task SyncPermissionStateAsync(Guid userId, NotificationPermissionState state)
    {
        var token = _authService.GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        using var http = new HttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_config.SupabaseUrl}/rest/v1/user_devices");

        request.Headers.Add("apikey", _config.SupabaseAnonOrPublisableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Prefer", "resolution=merge-duplicates,return=minimal");

        var deviceTimezone = TimezoneHelper.GetCurrentDeviceTimezoneId();
        var payload = new[]
        {
            new
            {
                user_id = userId,
                onesignal_external_id = userId.ToString(),
                platform = DeviceInfo.Current.Platform.ToString().ToLowerInvariant(),
                permission_state = state.ToString().ToLowerInvariant(),
                notifications_enabled = state == NotificationPermissionState.Granted,
                device_timezone = deviceTimezone,
                last_seen_at = DateTimeOffset.UtcNow,
                updated_at = DateTimeOffset.UtcNow
            }
        };

        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        using var response = await http.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public async Task SyncUserProfileTimezoneAsync(Guid userId)
    {
        var token = _authService.GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
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
        response.EnsureSuccessStatusCode();
    }
}
