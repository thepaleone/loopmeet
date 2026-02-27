using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Profile.Models;
using LoopMeet.App.Services;
using Microsoft.Extensions.Logging;

namespace LoopMeet.App.Features.Profile.ViewModels;

public sealed partial class ProfileViewModel : ObservableObject
{
    private readonly UsersApi _usersApi;
    private readonly UserProfileCache _userProfileCache;
    private readonly ILogger<ProfileViewModel> _logger;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string? _avatarUrl;

    [ObservableProperty]
    private string _avatarSource = "none";

    [ObservableProperty]
    private DateTimeOffset _userSince;

    [ObservableProperty]
    private int _groupCount;

    [ObservableProperty]
    private string _avatarInput = string.Empty;

    [ObservableProperty]
    private bool _canChangePassword = true;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _showStatus;

    public ProfileViewModel(UsersApi usersApi, UserProfileCache userProfileCache, ILogger<ProfileViewModel> logger)
    {
        _usersApi = usersApi;
        _userProfileCache = userProfileCache;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ShowStatus = false;
        StatusMessage = string.Empty;
        var hasCachedProfile = false;
        var cached = _userProfileCache.GetCachedProfile();
        if (cached is not null)
        {
            hasCachedProfile = true;
            Apply(cached);
        }

        try
        {
            var profile = await _usersApi.GetProfileSummaryAsync();
            _userProfileCache.SetCachedProfile(profile);
            Apply(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load profile");
            if (!hasCachedProfile)
            {
                ShowStatus = true;
                StatusMessage = "Unable to load profile. Please try again.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveProfileAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ShowStatus = false;
        StatusMessage = string.Empty;
        try
        {
            var updated = await _usersApi.UpdateProfileAsync(new UserProfileUpdateRequest
            {
                DisplayName = DisplayName,
                AvatarOverrideUrl = string.IsNullOrWhiteSpace(AvatarInput) ? null : AvatarInput
            });

            _userProfileCache.SetCachedProfile(updated);
            Apply(updated);
            ShowStatus = true;
            StatusMessage = "Profile updated.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save profile");
            ShowStatus = true;
            StatusMessage = "Unable to save profile changes.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenPasswordChangeAsync()
    {
        await Shell.Current.GoToAsync("change-password");
    }

    private void Apply(UserProfileResponse profile)
    {
        DisplayName = profile.DisplayName;
        Email = profile.Email;
        AvatarUrl = profile.AvatarUrl;
        AvatarSource = profile.AvatarSource;
        AvatarInput = profile.AvatarUrl ?? string.Empty;
        UserSince = profile.UserSince;
        GroupCount = profile.GroupCount;
        CanChangePassword = profile.CanChangePassword;
    }
}
