using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Profile.Models;
using LoopMeet.App.Services;
using Microsoft.Extensions.Logging;
using Refit;
using System.Text.Json;

namespace LoopMeet.App.Features.Profile.ViewModels;

public sealed partial class ChangePasswordViewModel : ObservableObject
{
    private readonly UsersApi _usersApi;
    private readonly UserProfileCache _userProfileCache;
    private readonly ILogger<ChangePasswordViewModel> _logger;
    private bool _hasLoadedContext;

    [ObservableProperty]
    private string _currentPassword = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private bool _requiresCurrentPassword = true;

    [ObservableProperty]
    private bool _requiresEmailForPasswordSetup;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _showError;

    public ChangePasswordViewModel(UsersApi usersApi, UserProfileCache userProfileCache, ILogger<ChangePasswordViewModel> logger)
    {
        _usersApi = usersApi;
        _userProfileCache = userProfileCache;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoadContextAsync()
    {
        if (_hasLoadedContext)
        {
            return;
        }

        _hasLoadedContext = true;
        var profile = _userProfileCache.GetCachedProfile();
        if (profile is null)
        {
            try
            {
                profile = await _usersApi.GetProfileSummaryAsync();
                _userProfileCache.SetCachedProfile(profile);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to load profile context for password change. Falling back to current-password flow.");
                return;
            }
        }

        ApplyProfileContext(profile);
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

        if (string.IsNullOrWhiteSpace(NewPassword)
            || string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            ShowError = true;
            ErrorMessage = "New password and confirmation are required.";
            return;
        }

        if (RequiresCurrentPassword && string.IsNullOrWhiteSpace(CurrentPassword))
        {
            ShowError = true;
            ErrorMessage = "Current password is required.";
            return;
        }

        if (RequiresEmailForPasswordSetup && string.IsNullOrWhiteSpace(Email))
        {
            ShowError = true;
            ErrorMessage = "Email is required to set your password.";
            return;
        }

        IsBusy = true;
        try
        {
            var email = string.IsNullOrWhiteSpace(Email) ? null : Email.Trim();
            await _usersApi.ChangePasswordAsync(new PasswordChangeRequest
            {
                Email = email,
                CurrentPassword = RequiresCurrentPassword ? CurrentPassword : string.Empty,
                NewPassword = NewPassword,
                ConfirmPassword = ConfirmPassword
            });

            var cached = _userProfileCache.GetCachedProfile();
            if (cached is not null)
            {
                if (!string.IsNullOrWhiteSpace(email))
                {
                    cached.Email = email;
                }

                cached.HasEmailProvider = true;
                cached.RequiresCurrentPassword = true;
                cached.RequiresEmailForPasswordSetup = false;
                _userProfileCache.SetCachedProfile(cached);
            }

            if (Shell.Current is not null)
            {
                await Shell.Current.DisplayAlertAsync("Password Updated", "Your password has been updated.", "OK");
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (ApiException apiException)
        {
            _logger.LogWarning(apiException, "Password change request rejected. StatusCode: {StatusCode}", apiException.StatusCode);
            ShowError = true;
            ErrorMessage = MapApiErrorMessage(apiException);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change password");
            ShowError = true;
            ErrorMessage = "Unable to change password. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task CancelAsync()
    {
        return Shell.Current is null ? Task.CompletedTask : Shell.Current.GoToAsync("..");
    }

    private static string MapApiErrorMessage(ApiException apiException)
    {
        const string fallback = "Unable to change password. Please try again.";
        if (string.IsNullOrWhiteSpace(apiException.Content))
        {
            return fallback;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<ErrorResponsePayload>(
                apiException.Content,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            if (payload is null)
            {
                return fallback;
            }

            if (string.Equals(payload.Code, "invalid_current_password", StringComparison.OrdinalIgnoreCase))
            {
                return "Current password is incorrect.";
            }

            if (string.Equals(payload.Code, "missing_account_email", StringComparison.OrdinalIgnoreCase))
            {
                return "Enter your account email to set your password.";
            }

            if (!string.IsNullOrWhiteSpace(payload.Message))
            {
                return payload.Message;
            }
        }
        catch
        {
            return fallback;
        }

        return fallback;
    }

    private void ApplyProfileContext(UserProfileResponse profile)
    {
        RequiresCurrentPassword = profile.RequiresCurrentPassword || profile.HasEmailProvider;
        RequiresEmailForPasswordSetup = profile.RequiresEmailForPasswordSetup || !profile.HasEmailProvider;
        Email = RequiresEmailForPasswordSetup ? profile.Email : string.Empty;
    }

    private sealed class ErrorResponsePayload
    {
        public string? Code { get; init; }
        public string? Message { get; init; }
    }
}
