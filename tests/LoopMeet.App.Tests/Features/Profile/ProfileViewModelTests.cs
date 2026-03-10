namespace LoopMeet.App.Tests.Features.Profile;

public sealed class ProfileViewModelTests
{
    [Fact]
    public void LoginViewModel_HasSocialAvatarSyncAfterGoogleSignIn()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App/Features/Auth/ViewModels/LoginViewModel.cs"));
        var source = File.ReadAllText(path);

        Assert.Contains("SocialAvatarUrl", source, StringComparison.Ordinal);
        Assert.Contains("profile.AvatarUrl", source, StringComparison.Ordinal);
        Assert.Contains("UpdateProfileAsync", source, StringComparison.Ordinal);
    }


    [Fact]
    public void ProfileViewModel_DoesNotHaveAvatarInputOrAvatarSource()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs"));
        var source = File.ReadAllText(path);

        Assert.DoesNotContain("AvatarInput", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AvatarSource", source, StringComparison.Ordinal);
        Assert.Contains("HasAvatar", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfilePage_HasCircularAvatarBesideDisplayName()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App/Features/Profile/Views/ProfilePage.xaml"));
        var xaml = File.ReadAllText(path);

        Assert.Contains("Ellipse", xaml, StringComparison.Ordinal);
        Assert.Contains("tab_profile_fallback.png", xaml, StringComparison.Ordinal);
        Assert.Contains("HasAvatar", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfilePage_DoesNotHaveAvatarUrlEntry()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App/Features/Profile/Views/ProfilePage.xaml"));
        var xaml = File.ReadAllText(path);

        Assert.DoesNotContain("AvatarInput", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Avatar URL", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("AvatarSource", xaml, StringComparison.Ordinal);
    }

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

    [Fact]
    public void ProfileViewModel_HasPickAvatarCommandWithMediaPicker()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs"));
        var source = File.ReadAllText(path);

        Assert.Contains("PickAvatarAsync", source, StringComparison.Ordinal);
        Assert.Contains("MediaPicker", source, StringComparison.Ordinal);
        Assert.Contains("IsCaptureSupported", source, StringComparison.Ordinal);
        Assert.Contains("StreamPart", source, StringComparison.Ordinal);
        Assert.Contains("UploadAvatarAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UsersApi_HasUploadAvatarMethod()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App/Services/UsersApi.cs"));
        var source = File.ReadAllText(path);

        Assert.Contains("UploadAvatarAsync", source, StringComparison.Ordinal);
        Assert.Contains("StreamPart", source, StringComparison.Ordinal);
    }
}
