using System.Text.RegularExpressions;

namespace LoopMeet.App.Tests.Features;

// 018: pull to refresh on Home and the Groups list, matching group detail, plus the Profile version
// line. MAUI/XAML wiring, so verified by source inspection; quickstart.md covers the device checks.
public sealed class RefreshSurfaceTests
{
    [Theory]
    [InlineData("src/LoopMeet.App/Features/Home/Views/HomePage.xaml")]
    [InlineData("src/LoopMeet.App/Features/Groups/Views/GroupsListPage.xaml")]
    public void Page_WrapsItsScrollViewInARefreshView(string page)
    {
        var source = ReadSource(page);

        Assert.Matches(new Regex(@"<RefreshView\s+Command=""\{Binding RefreshCommand\}""\s+IsRefreshing=""\{Binding IsRefreshing\}"">\s*<ScrollView"), source);
        Assert.Contains("</ScrollView>", source[source.IndexOf("<RefreshView", StringComparison.Ordinal)..source.IndexOf("</RefreshView>", StringComparison.Ordinal)], StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("src/LoopMeet.App/Features/Home/ViewModels/HomeViewModel.cs")]
    [InlineData("src/LoopMeet.App/Features/Groups/ViewModels/GroupsListViewModel.cs")]
    public void ViewModel_RefreshAlwaysStopsTheSpinner(string viewModel)
    {
        var source = ReadSource(viewModel);
        var refresh = Slice(source, "private async Task RefreshAsync()", "\n    }\n");

        Assert.Contains("private bool _isRefreshing;", source, StringComparison.Ordinal);
        Assert.Matches(new Regex(@"try\s*\{\s*await LoadOnceAsync\(\);\s*\}\s*finally\s*\{\s*IsRefreshing = false;\s*\}"), refresh);
    }

    [Theory]
    [InlineData("src/LoopMeet.App/Features/Home/ViewModels/HomeViewModel.cs")]
    [InlineData("src/LoopMeet.App/Features/Groups/ViewModels/GroupsListViewModel.cs")]
    public void ViewModel_LoadsAreSingleFlight(string viewModel)
    {
        var source = ReadSource(viewModel);

        Assert.Contains("private Task LoadAsync() => LoadOnceAsync();", source, StringComparison.Ordinal);
        Assert.Contains("if (_inFlightLoad is { IsCompleted: false })", source, StringComparison.Ordinal);
        Assert.Contains("return _inFlightLoad = LoadCoreAsync();", source, StringComparison.Ordinal);
    }

    // A failed load, including an offline pull to refresh, used to show "Service Unavailable" and
    // exit the app. It now keeps the lists on screen and shows a dismissible message (018 T010).
    [Fact]
    public void GroupsList_NeverQuitsTheAppWhenALoadFails()
    {
        var source = ReadSource("src/LoopMeet.App/Features/Groups/ViewModels/GroupsListViewModel.cs");

        foreach (var exit in new[] { "Quit()", "Environment.Exit", "Process.GetCurrentProcess().Kill", "terminateWithSuccess" })
        {
            Assert.DoesNotContain(exit, source, StringComparison.Ordinal);
        }

        Assert.Contains("catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)", source, StringComparison.Ordinal);
        Assert.Contains("\"Couldn't load your groups. Check your connection and try again.\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Profile_ShowsTheVersionFromTheInstalledApp()
    {
        var viewModel = ReadSource("src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs");
        var page = ReadSource("src/LoopMeet.App/Features/Profile/Views/ProfilePage.xaml");

        Assert.Contains("AppVersionText.Format(AppInfo.Current.VersionString, AppInfo.Current.BuildString)", viewModel, StringComparison.Ordinal);
        var label = page.IndexOf("Text=\"{Binding VersionText}\"", StringComparison.Ordinal);
        Assert.True(label >= 0, "Profile has no label bound to VersionText");
        Assert.True(label > page.LastIndexOf("<Button", StringComparison.Ordinal), "the version line must come after the last button");
    }

    private static string Slice(string source, string start, string end)
    {
        var from = source.IndexOf(start, StringComparison.Ordinal);
        Assert.True(from >= 0, $"'{start}' not found");
        var to = source.IndexOf(end, from, StringComparison.Ordinal);
        Assert.True(to > from, $"'{end}' not found after '{start}'");
        return source[from..to];
    }

    private static string ReadSource(string repoRelativePath) =>
        File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../", repoRelativePath)));
}
