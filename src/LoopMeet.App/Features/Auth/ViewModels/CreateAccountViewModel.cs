using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Auth.Models;
using LoopMeet.App.Services;

namespace LoopMeet.App.Features.Auth.ViewModels;

public sealed partial class CreateAccountViewModel : ObservableObject
{
    private readonly AuthService _authService;
    private readonly UsersApi _usersApi;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string? _phone;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isOAuthFlow;

    public CreateAccountViewModel(AuthService authService, UsersApi usersApi)
    {
        _authService = authService;
        _usersApi = usersApi;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(DisplayName)
            || string.IsNullOrWhiteSpace(Email)
            || (!IsOAuthFlow && string.IsNullOrWhiteSpace(Password)))
        {
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

            await _usersApi.UpsertProfileAsync(new UserProfileRequest
            {
                DisplayName = DisplayName,
                Email = Email,
                Phone = Phone,
                Password = Password
            });

            await Shell.Current.GoToAsync("//groups");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ApplyPrefill(string? displayName, string? email, string? phone, bool isOAuthFlow)
    {
        IsOAuthFlow = isOAuthFlow;

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
        }
    }
}
