using LoopMeet.App.Features.Auth.ViewModels;
using LoopMeet.App.Features.Home.Models;

namespace LoopMeet.App.Features.Auth;

public sealed class AuthCoordinator
{
    private readonly CreateAccountViewModel _createAccountViewModel;

    public AuthCoordinator(CreateAccountViewModel createAccountViewModel)
    {
        _createAccountViewModel = createAccountViewModel;
    }

    public Task NavigateToGroupsAsync()
    {
        return Shell.Current.GoToAsync(SignedInTabs.HomeShellPath);
    }

    public async Task NavigateToCreateAccountAsync(string? displayName, string? email, string? phone, bool isOAuthFlow, string? socialAvatarUrl = null)
    {
        _createAccountViewModel.ApplyPrefill(displayName, email, phone, isOAuthFlow, socialAvatarUrl);
        await Shell.Current.GoToAsync("create-account");
    }
}
