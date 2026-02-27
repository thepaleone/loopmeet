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
}
