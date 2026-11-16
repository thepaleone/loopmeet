using System.Text.RegularExpressions;

namespace LoopMeet.App.Tests.Features.Safety;

/// <summary>
/// Source inspection of the report flow (feature 023 US1). The pages, the mail composer and the
/// clipboard only run on a device, so these tests read the XAML and the ViewModels.
/// </summary>
public sealed class ReportConcernSurfaceTests
{
    private const string ProfilePage = "src/LoopMeet.App/Features/Profile/Views/ProfilePage.xaml";
    private const string ProfileViewModel = "src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs";
    private const string ReportPage = "src/LoopMeet.App/Features/Safety/Views/ReportConcernPage.xaml";
    private const string ReportViewModel = "src/LoopMeet.App/Features/Safety/ViewModels/ReportConcernViewModel.cs";
    private const string GroupDetailPage = "src/LoopMeet.App/Features/Groups/Views/GroupDetailPage.xaml";
    private const string GroupDetailViewModel = "src/LoopMeet.App/Features/Groups/ViewModels/GroupDetailViewModel.cs";

    [Fact]
    public void Profile_HasAReportAConcernRowInThe022RowStyle()
    {
        var page = ReadSource(ProfilePage);
        var row = RowContaining(page, "Text=\"Report a concern\"");

        Assert.Contains("Style=\"{StaticResource SettingsRow}\"", row, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding ReportConcernCommand}\"", row, StringComparison.Ordinal);
        Assert.Contains("ic_flag.png", row, StringComparison.Ordinal);
        Assert.Contains("GoToAsync(\"report-concern\")", ReadSource(ProfileViewModel), StringComparison.Ordinal);
    }

    [Fact]
    public void ReportPage_BindsEveryFieldAndLimitsTheDetails()
    {
        var page = ReadSource(ReportPage);

        foreach (var binding in new[]
                 {
                     "ItemsSource=\"{Binding Groups}\"",
                     "SelectedItem=\"{Binding SelectedGroup}\"",
                     "ItemsSource=\"{Binding People}\"",
                     "SelectedItem=\"{Binding SelectedPerson}\"",
                     "ItemsSource=\"{Binding Reasons}\"",
                     "SelectedItem=\"{Binding SelectedReason}\"",
                     "Text=\"{Binding Details}\"",
                     "Text=\"{Binding RemainingText}\"",
                     "Command=\"{Binding SubmitReportCommand}\""
                 })
        {
            Assert.Contains(binding, page, StringComparison.Ordinal);
        }

        Assert.Contains("MaxLength=\"1000\"", page, StringComparison.Ordinal);
        Assert.Contains("SemanticProperties.Description", page, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportPage_HasANoGroupsStateWithTheCopyableAddress()
    {
        var page = ReadSource(ReportPage);

        Assert.Contains("IsVisible=\"{Binding HasNoGroups}\"", page, StringComparison.Ordinal);
        Assert.Contains("{x:Static safety:ReportFallbackMail.SafetyAddress}", page, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding CopyAddressCommand}\"", page, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportViewModel_FollowsTheConventionsAndFallsBackToEmail()
    {
        var viewModel = ReadSource(ReportViewModel);

        Assert.Contains("public sealed partial class ReportConcernViewModel : ObservableObject", viewModel, StringComparison.Ordinal);
        Assert.Contains("Task SubmitReportAsync(", viewModel, StringComparison.Ordinal);
        Assert.Contains("ReportDraft.CanSubmit(", viewModel, StringComparison.Ordinal);
        Assert.Contains("SafetyErrorText.OffersEmailFallback(", viewModel, StringComparison.Ordinal);
        Assert.Contains("SafetyErrorText.Message(", viewModel, StringComparison.Ordinal);
        Assert.Contains("ReportFallbackMail.Compose(", viewModel, StringComparison.Ordinal);
        Assert.Contains("Email.Default.IsComposeSupported", viewModel, StringComparison.Ordinal);
        Assert.Contains("Email.Default.ComposeAsync(", viewModel, StringComparison.Ordinal);
        Assert.Contains("Clipboard.Default.SetTextAsync(", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportViewModel_NeverLogsTheDetails()
    {
        var logCalls = Regex.Matches(ReadSource(ReportViewModel), @"\bLog(Information|Warning|Error)\((?s:.*?)\);");

        Assert.All(logCalls, call => Assert.DoesNotContain("Details", call.Value, StringComparison.Ordinal));
    }

    [Fact]
    public void GroupDetail_MemberTilesOpenActionsAndAreDescribedForScreenReaders()
    {
        var page = ReadSource(GroupDetailPage);
        var tile = page[page.IndexOf("BindableLayout.ItemsSource=\"{Binding Members}\"", StringComparison.Ordinal)..];
        tile = tile[..tile.IndexOf("</BindableLayout.ItemTemplate>", StringComparison.Ordinal)];

        Assert.Contains("BindingContext.MemberActionsCommand", tile, StringComparison.Ordinal);
        Assert.Contains("CommandParameter=\"{Binding .}\"", tile, StringComparison.Ordinal);
        Assert.Contains("SemanticProperties.Description=\"{Binding ActionsDescription}\"", tile, StringComparison.Ordinal);

        var viewModel = ReadSource(GroupDetailViewModel);
        Assert.Contains("Task MemberActionsAsync(", viewModel, StringComparison.Ordinal);
        Assert.Contains("\"report-concern\"", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void Shell_RegistersTheReportRoute()
    {
        Assert.Contains("Routing.RegisterRoute(\"report-concern\", typeof(ReportConcernPage));",
            ReadSource("src/LoopMeet.App/AppShell.xaml.cs"), StringComparison.Ordinal);
    }

    private static string RowContaining(string page, string marker)
    {
        var at = page.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(at >= 0, $"{marker} not found");
        var start = page.LastIndexOf("<Grid ", at, StringComparison.Ordinal);
        var end = page.IndexOf("</Grid>", at, StringComparison.Ordinal);
        return page[start..end];
    }

    private static string ReadSource(string repoRelativePath) =>
        File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../..", repoRelativePath)));
}
