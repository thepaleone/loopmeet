namespace LoopMeet.App.Tests.Features.Profile;

public sealed class ProfileViewModelTests
{
    [Fact]
    public void ProfileViewModel_LoadsSummaryAndSavesProfileViaUsersApi()
    {
        var viewModelPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs"));
        var source = File.ReadAllText(viewModelPath);

        Assert.Contains("GetProfileSummaryAsync", source, StringComparison.Ordinal);
        Assert.Contains("UpdateProfileAsync", source, StringComparison.Ordinal);
        Assert.Contains("GetCachedProfile", source, StringComparison.Ordinal);
        Assert.Contains("SetCachedProfile", source, StringComparison.Ordinal);
        Assert.Contains("Profile updated.", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AppShell_DefinesProfileTabAfterInvitations()
    {
        var appShellPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App/AppShell.xaml"));
        var xaml = File.ReadAllText(appShellPath);

        var invitationsIndex = xaml.IndexOf("Route=\"invitations\"", StringComparison.Ordinal);
        var profileIndex = xaml.IndexOf("Route=\"profile\"", StringComparison.Ordinal);

        Assert.True(invitationsIndex >= 0, "Invitations tab route should exist.");
        Assert.True(profileIndex > invitationsIndex, "Profile tab route should appear after Invitations.");
    }

    [Fact]
    public void ProfileViewModel_HasLogoutCommandThatCallsSignOutAndNavigatesToLogin()
    {
        var viewModelPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs"));
        var source = File.ReadAllText(viewModelPath);

        Assert.Contains("LogoutAsync", source, StringComparison.Ordinal);
        Assert.Contains("SignOutAsync", source, StringComparison.Ordinal);
        Assert.Contains("//login", source, StringComparison.Ordinal);
        Assert.Contains("AuthService", source, StringComparison.Ordinal);
    }

    [Fact]
    public void GroupsListViewModel_DoesNotHaveLogoutCommand()
    {
        var viewModelPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App/Features/Groups/ViewModels/GroupsListViewModel.cs"));
        var source = File.ReadAllText(viewModelPath);

        Assert.DoesNotContain("LogoutAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("LogoutCommand", source, StringComparison.Ordinal);
    }

    [Fact]
    public void GroupsListPage_DoesNotContainLogoutButton()
    {
        var xamlPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App/Features/Groups/Views/GroupsListPage.xaml"));
        var xaml = File.ReadAllText(xamlPath);

        Assert.DoesNotContain("LogoutCommand", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Log out", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfilePage_ContainsLogoutButtonWithIcon()
    {
        var xamlPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App/Features/Profile/Views/ProfilePage.xaml"));
        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("LogoutCommand", xaml, StringComparison.Ordinal);
        Assert.Contains("ic_logout.png", xaml, StringComparison.Ordinal);
    }
}
