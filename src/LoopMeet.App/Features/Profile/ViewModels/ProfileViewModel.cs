using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Auth;
using LoopMeet.App.Features.Profile.Models;
using LoopMeet.App.Services;
using Microsoft.Extensions.Logging;
using Refit;

namespace LoopMeet.App.Features.Profile.ViewModels;

public sealed partial class ProfileViewModel : ObservableObject
{
    private readonly AuthService _authService;
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
    private bool _hasAvatar;

    [ObservableProperty]
    private DateTimeOffset _userSince;

    [ObservableProperty]
    private int _groupCount;

    [ObservableProperty]
    private bool _canChangePassword = true;

    [ObservableProperty]
    private bool _isUploading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _showStatus;

    public ProfileViewModel(AuthService authService, UsersApi usersApi, UserProfileCache userProfileCache, ILogger<ProfileViewModel> logger)
    {
        _authService = authService;
        _usersApi = usersApi;
        _userProfileCache = userProfileCache;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _authService.SignOutAsync();
        }
        finally
        {
            IsBusy = false;
        }

        await Shell.Current.GoToAsync("//login");
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
                DisplayName = DisplayName
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

    [RelayCommand]
    private async Task PickAvatarAsync()
    {
        if (IsBusy || IsUploading)
        {
            return;
        }

        FileResult? file;
        try
        {
#if MACCATALYST
            file = await MainThread.InvokeOnMainThreadAsync(() =>
                FilePicker.Default.PickAsync(new PickOptions
                {
                    FileTypes = FilePickerFileType.Images
                }));
#else
            var options = new List<string> { "Choose from library" };
            if (MediaPicker.Default.IsCaptureSupported)
            {
                options.Insert(0, "Take a new photo");
            }

            var action = await Shell.Current.DisplayActionSheetAsync("Profile photo", "Cancel", null, options.ToArray());
            if (string.IsNullOrEmpty(action) || action == "Cancel")
            {
                return;
            }

            if (action == "Take a new photo")
            {
                var cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                if (cameraStatus != PermissionStatus.Granted)
                {
                    return;
                }
                file = await MediaPicker.Default.CapturePhotoAsync();
            }
            else
            {
                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
                    var storageStatus = await Permissions.RequestAsync<Permissions.StorageRead>();
                    if (storageStatus != PermissionStatus.Granted)
                    {
                        return;
                    }
                }
                var photos = await MediaPicker.Default.PickPhotosAsync();
                file = photos?.FirstOrDefault();
            }
#endif
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to capture or pick photo.");
            return;
        }

        if (file is null)
        {
            return;
        }

        IsUploading = true;
        ShowStatus = false;
        StatusMessage = string.Empty;
        try
        {
            await using var stream = await file.OpenReadAsync();
            var ext = Path.GetExtension(file.FileName ?? "").TrimStart('.').ToLowerInvariant();
            var contentType = ext switch
            {
                "png" => "image/png",
                "gif" => "image/gif",
                "webp" => "image/webp",
                _ => "image/jpeg"
            };
            var part = new StreamPart(stream, file.FileName ?? "photo.jpg", contentType);
            var updated = await _usersApi.UploadAvatarAsync(part);
            _userProfileCache.SetCachedProfile(updated);
            Apply(updated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload avatar.");
            ShowStatus = true;
            StatusMessage = "Unable to update profile photo. Please try again.";
        }
        finally
        {
            IsUploading = false;
        }
    }

    private void Apply(UserProfileResponse profile)
    {
        DisplayName = profile.DisplayName;
        Email = profile.Email;
        AvatarUrl = profile.AvatarUrl;
        HasAvatar = !string.IsNullOrWhiteSpace(AvatarUrl);
        UserSince = profile.UserSince;
        GroupCount = profile.GroupCount;
        CanChangePassword = profile.CanChangePassword;
    }
}
