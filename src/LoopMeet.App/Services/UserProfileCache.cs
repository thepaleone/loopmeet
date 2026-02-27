using System.Text.Json;
using LoopMeet.App.Features.Profile.Models;
using Microsoft.Maui.Storage;

namespace LoopMeet.App.Services;

public sealed class UserProfileCache
{
    private const string CacheKey = "loopmeet.profile.cache";
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);
    private UserProfileResponse? _cached;

    public UserProfileResponse? GetCachedProfile()
    {
        if (_cached is not null)
        {
            return Clone(_cached);
        }

        var json = Preferences.Default.Get(CacheKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            _cached = JsonSerializer.Deserialize<UserProfileResponse>(json, _serializerOptions);
        }
        catch
        {
            _cached = null;
            Preferences.Default.Remove(CacheKey);
        }

        return _cached is null ? null : Clone(_cached);
    }

    public void SetCachedProfile(UserProfileResponse profile)
    {
        _cached = Clone(profile);
        var json = JsonSerializer.Serialize(_cached, _serializerOptions);
        Preferences.Default.Set(CacheKey, json);
    }

    public void Clear()
    {
        _cached = null;
        Preferences.Default.Remove(CacheKey);
    }

    private static UserProfileResponse Clone(UserProfileResponse profile)
    {
        return new UserProfileResponse
        {
            DisplayName = profile.DisplayName,
            Email = profile.Email,
            Phone = profile.Phone,
            AvatarUrl = profile.AvatarUrl,
            AvatarSource = profile.AvatarSource,
            UserSince = profile.UserSince,
            GroupCount = profile.GroupCount,
            CanChangePassword = profile.CanChangePassword,
            HasEmailProvider = profile.HasEmailProvider,
            RequiresCurrentPassword = profile.RequiresCurrentPassword,
            RequiresEmailForPasswordSetup = profile.RequiresEmailForPasswordSetup
        };
    }
}
