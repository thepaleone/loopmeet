namespace LoopMeet.App.Tests.Features.Account;

public sealed class DeleteAccountSurfaceTests
{
    [Fact]
    public void DeleteAccountPage_BindsConfirmationButtonAndOwnedGroups()
    {
        var source = ReadAppFile("Features/Account/Views/DeleteAccountPage.xaml");

        Assert.Contains("Text=\"{Binding Confirmation}\"", source, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding DeleteCommand}\"", source, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding CanDelete}\"", source, StringComparison.Ordinal);
        Assert.Contains("BindableLayout.ItemsSource=\"{Binding OwnedGroups}\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CollectionView", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DeleteAccountViewModel_UsesConfirmationRuleRequestAndSignOut()
    {
        var source = ReadAppFile("Features/Account/ViewModels/DeleteAccountViewModel.cs");

        Assert.Contains("AccountDeletionConfirmation.IsConfirmed", source, StringComparison.Ordinal);
        Assert.Contains("RequestAccountDeletionAsync", source, StringComparison.Ordinal);
        Assert.Contains("SignOutAsync(SignOutReason.UserInitiated)", source, StringComparison.Ordinal);
        Assert.Contains("MainThread", source, StringComparison.Ordinal);
        Assert.Contains("Couldn't schedule the deletion. Check your connection and try again.", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AppShell_RegistersDeleteAccountRoute()
    {
        var source = ReadAppFile("AppShell.xaml.cs");

        Assert.Contains("Routing.RegisterRoute(\"delete-account\", typeof(DeleteAccountPage))", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfileViewModel_NavigatesToDeleteAccount()
    {
        var source = ReadAppFile("Features/Profile/ViewModels/ProfileViewModel.cs");

        Assert.Contains("GoToAsync(\"delete-account\")", source, StringComparison.Ordinal);
        Assert.DoesNotContain("isn't available yet", source, StringComparison.Ordinal);
    }

    private static string ReadAppFile(string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App", relativePath));
        return File.ReadAllText(path);
    }
}
