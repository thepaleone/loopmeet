using System.Text.RegularExpressions;

namespace LoopMeet.App.Tests.Features.Account;

// MAUI/XAML wiring for the scheduled-deletion screen (016 US3), verified by source inspection.
// Quickstart rows 6–8 cover the behaviour on a device.
public sealed class PendingDeletionSurfaceTests
{
    [Fact]
    public void ApiClient_AddsAccountStatusHandlerAfterApiAuthHandler()
    {
        var source = ReadSource("src/LoopMeet.App/Services/ApiClient.cs");

        var auth = source.IndexOf(".AddHttpMessageHandler<ApiAuthHandler>()", StringComparison.Ordinal);
        var status = source.IndexOf(".AddHttpMessageHandler<AccountStatusHandler>()", StringComparison.Ordinal);
        Assert.True(auth >= 0, "ApiAuthHandler is no longer added");
        Assert.True(status > auth, "AccountStatusHandler must be added after ApiAuthHandler");
    }

    [Fact]
    public void ApiAuthHandler_StillNeverNavigates()
    {
        Assert.DoesNotContain("GoToAsync", ReadSource("src/LoopMeet.App/Services/ApiAuthHandler.cs"), StringComparison.Ordinal);
    }

    [Fact]
    public void AppShell_DeclaresThePendingDeletionContentOutsideTheTabBar()
    {
        var source = ReadSource("src/LoopMeet.App/AppShell.xaml");

        var route = source.IndexOf("Route=\"pending-deletion\"", StringComparison.Ordinal);
        Assert.True(route >= 0, "pending-deletion ShellContent missing");
        Assert.True(route < source.IndexOf("<TabBar>", StringComparison.Ordinal), "pending-deletion must not be a tab");
        Assert.Contains("ContentTemplate=\"{DataTemplate account:PendingDeletionPage}\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ViewModel_ExposesOnlyRestoreAndSignOut()
    {
        var source = ReadSource("src/LoopMeet.App/Features/Account/ViewModels/PendingDeletionViewModel.cs");

        var commands = Regex.Matches(source, @"\[RelayCommand\]\s+private\s+async\s+Task\s+(\w+)Async\(")
            .Select(match => match.Groups[1].Value)
            .Order()
            .ToList();
        Assert.Equal(["Restore", "SignOut"], commands);
        Assert.Contains("_usersApi.RestoreAccountAsync()", source, StringComparison.Ordinal);
        Assert.Contains("_gate.Reset()", Slice(source, "private async Task RestoreAsync(", "private async Task SignOutAsync("), StringComparison.Ordinal);
        Assert.Contains("_gate.Reset()", Slice(source, "private async Task SignOutAsync(", "\n    }\n"), StringComparison.Ordinal);
        Assert.Contains("SignOutReason.UserInitiated", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Page_BindsRestoreVisibilityToIsRestoreAvailable()
    {
        var source = ReadSource("src/LoopMeet.App/Features/Account/Views/PendingDeletionPage.xaml");

        Assert.Matches(new Regex(@"<Button[^>]*Command=""\{Binding RestoreCommand\}""[^>]*IsVisible=""\{Binding IsRestoreAvailable\}""", RegexOptions.Singleline), source);
        Assert.Contains("{Binding SignOutCommand}", source, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(source, "<Button").Count);
    }

    [Fact]
    public void Gate_NavigatesOnTheMainThreadOncePerEpisode()
    {
        var source = ReadSource("src/LoopMeet.App/Features/Account/AccountDeletionGate.cs");

        Assert.Contains("MainThread.InvokeOnMainThreadAsync", source, StringComparison.Ordinal);
        Assert.Contains("GoToAsync(\"//pending-deletion", source, StringComparison.Ordinal);
        Assert.Contains("Interlocked.Exchange(ref _navigated, 1) == 1", source, StringComparison.Ordinal);
        Assert.Contains("public void Reset() => Interlocked.Exchange(ref _navigated, 0);", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MauiProgram_ResolvesTheGateBeforeTheFirstApiCall()
    {
        var source = ReadSource("src/LoopMeet.App/MauiProgram.cs");

        Assert.Contains("AddSingleton<AccountDeletionSignal>()", source, StringComparison.Ordinal);
        Assert.Contains("AddTransient<AccountStatusHandler>()", source, StringComparison.Ordinal);
        Assert.Contains("app.Services.GetRequiredService<AccountDeletionGate>();", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Login_SignInPathsLeavePendingDeletionToTheGate()
    {
        var source = ReadSource("src/LoopMeet.App/Features/Auth/ViewModels/LoginViewModel.cs");
        const string pendingCatch = "catch (ApiException ex) when (AccountStatusHandler.IsPendingDeletion(ex.Content))";

        foreach (var method in new[] { "SignInAsync()", "SignInWithGoogleAsync()", "SignInWithAppleAsync()" })
        {
            var body = Slice(source, $"private async Task {method}", "finally");
            var pending = body.IndexOf(pendingCatch, StringComparison.Ordinal);
            var generic = body.IndexOf("catch (Exception ex)", StringComparison.Ordinal);
            Assert.True(pending >= 0, $"{method} doesn't catch account_pending_deletion");
            Assert.True(pending < generic, $"{method} must catch account_pending_deletion before its generic catch");
        }
    }

    private static string Slice(string source, string start, string end)
    {
        var from = source.IndexOf(start, StringComparison.Ordinal);
        Assert.True(from >= 0, $"'{start}' not found");
        var to = source.IndexOf(end, from, StringComparison.Ordinal);
        Assert.True(to > from, $"'{end}' not found after '{start}'");
        return source[from..to];
    }

    private static string ReadSource(string repoRelativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../", repoRelativePath));
        return File.ReadAllText(fullPath);
    }
}
