using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Auth.Models;
using LoopMeet.App.Services;
using Microsoft.Extensions.Logging;
using Refit;

namespace LoopMeet.App.Features.Auth.ViewModels;

public sealed partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _authService;
    private readonly AuthCoordinator _authCoordinator;
    private readonly UsersApi _usersApi;
    private readonly ILogger<LoginViewModel> _logger;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _showError;

    public LoginViewModel(
        AuthService authService,
        AuthCoordinator authCoordinator,
        UsersApi usersApi,
        ILogger<LoginViewModel> logger)
    {
        _authService = authService;
        _authCoordinator = authCoordinator;
        _usersApi = usersApi;
        _logger = logger;
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ShowError = false;
        ErrorMessage = string.Empty;
        try
        {
            _logger.LogInformation("Attempting sign-in for {Email}", Email);
            var session = await _authService.SignInWithEmailAsync(Email, Password);
            if (!string.IsNullOrWhiteSpace(session.AccessToken))
            {
                await Shell.Current.GoToAsync("//groups");
                return;
            }

            _logger.LogWarning("Sign-in failed for {Email}: empty access token.", Email);
            ShowError = true;
            ErrorMessage = "That login did not work. Please try again or create an account.";
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Sign-in failed for {Email}: network error.", Email);
            ShowError = true;
            ErrorMessage = "We could not reach the server. Please check your connection and try again.";
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Sign-in timed out for {Email}.", Email);
            ShowError = true;
            ErrorMessage = "The request timed out. Please try again.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sign-in failed for {Email}.", Email);
            ShowError = true;

            var message = ex.Message ?? string.Empty;
            if (message.Contains("invalid login credentials", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "That login did not work. Please try again or create an account.";
            }
            else
            {
                ErrorMessage = "Something unexpected happened. Please try again.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task NavigateToCreateAccountAsync()
    {
        return _authCoordinator.NavigateToCreateAccountAsync(null, null, null, false);
    }

    [RelayCommand]
    private async Task SignInWithGoogleAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ShowError = false;
        ErrorMessage = string.Empty;
        try
        {
            _logger.LogInformation("Starting Google sign-in.");
            var authResult = await _authService.SignInWithGoogleAsync();
            if (string.IsNullOrWhiteSpace(authResult.AccessToken))
            {
                ShowError = true;
                ErrorMessage = "Google sign-in did not complete. Please try again.";
                return;
            }

            var profile = await TryGetProfileAsync();
            if (profile is not null)
            {
                await Shell.Current.GoToAsync("//groups");
                return;
            }

            await _authCoordinator.NavigateToCreateAccountAsync(
                authResult.DisplayName,
                authResult.Email,
                authResult.Phone,
                true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google sign-in failed.");
            ShowError = true;
            ErrorMessage = "Google sign-in failed. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<UserProfileResponse?> TryGetProfileAsync()
    {
        try
        {
            return await _usersApi.GetProfileAsync();
        }
        catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }
}
