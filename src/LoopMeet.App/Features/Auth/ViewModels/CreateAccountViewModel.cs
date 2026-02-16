using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace LoopMeet.App.Features.Auth.ViewModels;

public sealed partial class CreateAccountViewModel : ObservableObject
{
    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string? _phone;

    [ObservableProperty]
    private bool _isBusy;

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (IsBusy)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(DisplayName) || string.IsNullOrWhiteSpace(Email))
        {
            return;
        }

        IsBusy = true;
        try
        {
            await Shell.Current.GoToAsync("groups");
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ApplyPrefill(string? displayName, string? email, string? phone)
    {
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
    }
}
