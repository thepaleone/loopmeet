using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Auth.Models;
using LoopMeet.App.Features.Home.Models;
using LoopMeet.App.Services;

namespace LoopMeet.App.Features.Auth.ViewModels;

public sealed partial class CreateAccountViewModel : ObservableObject
{
    private readonly AuthService _authService;
    private readonly UsersApi _usersApi;
    private readonly UserProfileCache _userProfileCache;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string? _phone;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string? _socialAvatarUrl;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isOAuthFlow;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _showError;

    public CreateAccountViewModel(AuthService authService, UsersApi usersApi, UserProfileCache userProfileCache)
    {
        _authService = authService;
        _usersApi = usersApi;
        _userProfileCache = userProfileCache;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ShowError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(DisplayName)
            || string.IsNullOrWhiteSpace(Email)
            || (!IsOAuthFlow && string.IsNullOrWhiteSpace(Password)))
        {
            ShowError = true;
            ErrorMessage = "Please complete all required fields.";
            return;
        }

        if (!IsOAuthFlow && !string.Equals(Password, ConfirmPassword, StringComparison.Ordinal))
        {
            ShowError = true;
            ErrorMessage = "Passwords do not match.";
            return;
        }

        IsBusy = true;
        try
        {
            if (!IsOAuthFlow)
            {
                var session = await _authService.SignUpWithEmailAsync(Email, Password);
                if (string.IsNullOrWhiteSpace(session.AccessToken))
                {
                    return;
                }
            }

            var profile = await _usersApi.UpsertProfileAsync(new UserProfileRequest
            {
                DisplayName = DisplayName,
                Email = Email,
                Phone = Phone,
                Password = Password,
                SocialAvatarUrl = SocialAvatarUrl
            });
            _userProfileCache.SetCachedProfile(new Features.Profile.Models.UserProfileResponse
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
            });

            await Shell.Current.GoToAsync(SignedInTabs.HomeShellPath);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ApplyPrefill(string? displayName, string? email, string? phone, bool isOAuthFlow, string? socialAvatarUrl = null)
    {
        IsOAuthFlow = isOAuthFlow;
        SocialAvatarUrl = socialAvatarUrl;

        if (!string.IsNullOrWhiteSpace(displayName) && string.IsNullOrWhiteSpace(DisplayName))
        {
            DisplayName = displayName;
        }

        if (!string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(Email))
        {
            Email = email;
        }

        if (!string.IsNullOrWhiteSpace(phone) && string.IsNullOrWhiteSpace(Phone))
        {
            Phone = phone;
        }

        if (IsOAuthFlow)
        {
            Password = string.Empty;
            ConfirmPassword = string.Empty;
        }
    }
}
