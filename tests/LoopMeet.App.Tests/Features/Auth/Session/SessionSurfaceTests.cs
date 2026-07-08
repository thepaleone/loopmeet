namespace LoopMeet.App.Tests.Features.Auth.Session;

/// <summary>
/// Source-surface assertions (repo pattern) for wiring that cannot be executed
/// off a MAUI host: lifecycle hooks, Shell layout, and the removal of the
/// legacy per-screen session handling.
/// </summary>
public sealed class SessionSurfaceTests
{
    // --- US1: restore refreshes instead of destroying the refresh token (root cause A1) ---

    [Fact]
    public void AuthService_NoLongerSignsOutOrRestoresSessions()
    {
        var source = ReadSource("src/LoopMeet.App/Features/Auth/AuthService.cs");

        // The destroy-on-expired restore path (and the whole restore/sign-out
        // surface) moved to SessionCoordinator; AuthService must not touch it.
        Assert.DoesNotContain("RestoreSessionAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Auth.SignOut", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AuthService_HasNoLegacyRawTokenStore()
    {
        var source = ReadSource("src/LoopMeet.App/Features/Auth/AuthService.cs");

        Assert.DoesNotContain("loopmeet.auth.access_token", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Preferences.Default", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SessionCoordinator_RefreshesExpiredSessionsAndCleansLegacyKey()
    {
        var source = ReadSource("src/LoopMeet.App/Features/Auth/Session/SessionCoordinator.cs");

        Assert.Contains("RefreshToken()", source, StringComparison.Ordinal);
        Assert.Contains("SessionFailureClassifier.Classify", source, StringComparison.Ordinal);
        Assert.Contains("loopmeet.auth.access_token", source, StringComparison.Ordinal);
    }

    [Fact]
    public void App_RefreshesSessionOnWindowResume()
    {
        var source = ReadSource("src/LoopMeet.App/App.xaml.cs");

        Assert.Contains("Resumed", source, StringComparison.Ordinal);
        Assert.Contains("EnsureFreshSessionAsync", source, StringComparison.Ordinal);
        Assert.Contains("RenewalTrigger.AppForegrounded", source, StringComparison.Ordinal);
    }

    // --- US2: login screen cannot be wedged by hung setup or abandoned provider flows ---

    [Fact]
    public void LoginViewModel_BoundsPostSignInSetupInAllSignInCommands()
    {
        var source = ReadSource("src/LoopMeet.App/Features/Auth/ViewModels/LoginViewModel.cs");

        Assert.Contains("PostSignInSetupTimeout", source, StringComparison.Ordinal);
        Assert.DoesNotContain("await _authSessionService.HandleSuccessfulSignInAsync()", source, StringComparison.Ordinal);
        var boundedCalls = CountOccurrences(source, "await RunPostSignInSetupBoundedAsync()");
        Assert.True(boundedCalls >= 5, $"Expected every sign-in success path to use the bounded setup; found {boundedCalls}.");
    }

    [Fact]
    public void LoginViewModel_CancelsAbandonedProviderSignIns()
    {
        var source = ReadSource("src/LoopMeet.App/Features/Auth/ViewModels/LoginViewModel.cs");

        Assert.Contains("_providerSignInCts", source, StringComparison.Ordinal);
        Assert.Contains("CancelPendingProviderSignIn", source, StringComparison.Ordinal);
        Assert.Contains(".WaitAsync(cancellation)", source, StringComparison.Ordinal);
        Assert.Contains("catch (OperationCanceledException)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void LoginPage_CancelsProviderSignInWhenDisappearing()
    {
        var source = ReadSource("src/LoopMeet.App/Features/Auth/Views/LoginPage.xaml.cs");

        Assert.Contains("OnDisappearing", source, StringComparison.Ordinal);
        Assert.Contains("CancelPendingProviderSignIn", source, StringComparison.Ordinal);
    }

    // --- US3: one sign-out path, no per-screen redirects, complete clearing ---

    [Fact]
    public void SessionCoordinator_ClearsEverythingOnSignOut()
    {
        var source = ReadSource("src/LoopMeet.App/Features/Auth/Session/SessionCoordinator.cs");

        Assert.Contains("DestroySession", source, StringComparison.Ordinal);
        Assert.Contains("_userProfileCache.Clear()", source, StringComparison.Ordinal);
        Assert.Contains("LogoutAsync", source, StringComparison.Ordinal); // OneSignal identity
        Assert.Contains("MainThread.InvokeOnMainThreadAsync", source, StringComparison.Ordinal);
        Assert.Contains("IHasUnsavedInput", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ProfileViewModel_RoutesLogoutThroughTheCoordinator()
    {
        var source = ReadSource("src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs");

        Assert.Contains("SignOutAsync(SignOutReason.UserInitiated)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("_authService.SignOutAsync", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("src/LoopMeet.App/Features/Groups/ViewModels/GroupsListViewModel.cs")]
    [InlineData("src/LoopMeet.App/Features/Invitations/ViewModels/PendingInvitationsViewModel.cs")]
    [InlineData("src/LoopMeet.App/Features/Home/ViewModels/HomeViewModel.cs")]
    [InlineData("src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs")]
    public void Screens_DoNotRedirectToLoginThemselves(string path)
    {
        var source = ReadSource(path);

        Assert.DoesNotContain("GoToAsync(\"//login\")", source, StringComparison.Ordinal);
    }

    [Fact]
    public void OneSignalIdentityService_SupportsLogout()
    {
        var source = ReadSource("src/LoopMeet.App/Services/Notifications/OneSignalIdentityService.cs");

        Assert.Contains("OneSignal.Logout()", source, StringComparison.Ordinal);
    }

    // --- US4: startup gate is the first Shell content; AppShell has no session logic ---

    [Fact]
    public void AppShell_StartsWithTheGateBeforeLogin()
    {
        var source = ReadSource("src/LoopMeet.App/AppShell.xaml");

        var startupIndex = source.IndexOf("Route=\"startup\"", StringComparison.Ordinal);
        var loginIndex = source.IndexOf("Route=\"login\"", StringComparison.Ordinal);
        Assert.True(startupIndex >= 0, "AppShell.xaml must declare the startup gate route.");
        Assert.True(loginIndex > startupIndex, "The startup gate must be the first ShellContent, before login.");
    }

    [Fact]
    public void AppShell_HasNoSessionLogic()
    {
        var source = ReadSource("src/LoopMeet.App/AppShell.xaml.cs");

        Assert.DoesNotContain("RestoreSessionAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetProfileSummaryAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("OnAppearing", source, StringComparison.Ordinal);
    }

    [Fact]
    public void StartupGate_ResolvesViaTheCoordinator()
    {
        var viewModel = ReadSource("src/LoopMeet.App/Features/Auth/ViewModels/StartupGateViewModel.cs");
        var page = ReadSource("src/LoopMeet.App/Features/Auth/Views/StartupGatePage.xaml");

        Assert.Contains("ResolveStartupAsync", viewModel, StringComparison.Ordinal);
        Assert.Contains("ActivityIndicator", page, StringComparison.Ordinal);
        Assert.Contains("Checking your session", viewModel, StringComparison.Ordinal);
    }

    private static string ReadSource(string repoRelativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "../../../../..", repoRelativePath));
        return File.ReadAllText(fullPath);
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
