using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Auth.Models;

namespace LoopMeet.App.Features.Auth.ViewModels;

public sealed partial class LoginViewModel : ObservableObject
{
    private readonly AuthService _authService;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public LoginViewModel(AuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var session = await _authService.SignInWithEmailAsync(Email, Password);
            if (!string.IsNullOrWhiteSpace(session.AccessToken))
            {
                await Shell.Current.GoToAsync("groups");
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
