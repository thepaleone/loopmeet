using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Auth.Models;
using Microsoft.Extensions.Logging;

namespace LoopMeet.App.Features.Auth.ViewModels;

public sealed partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _authService;
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

    public LoginViewModel(AuthService authService, ILogger<LoginViewModel> logger)
    {
        _authService = authService;
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
                await Shell.Current.GoToAsync("groups");
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
        return Shell.Current.GoToAsync("create-account");
    }
}
