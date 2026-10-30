namespace LoopMeet.App.Tests.Features.Groups;

/// <summary>
/// Source-inspection assertions for the Upcoming / Past meetup toggle on Group
/// Detail (feature 012), which cannot be executed off a MAUI host.
/// </summary>
public sealed class GroupDetailPastMeetupsSurfaceTests
{
    private const string Page = "src/LoopMeet.App/Features/Groups/Views/GroupDetailPage.xaml";
    private const string CodeBehind = "src/LoopMeet.App/Features/Groups/Views/GroupDetailPage.xaml.cs";
    private const string ViewModel = "src/LoopMeet.App/Features/Groups/ViewModels/GroupDetailViewModel.cs";
    private const string DetailViewModel = "src/LoopMeet.App/Features/Meetups/ViewModels/MeetupDetailViewModel.cs";

    [Fact]
    public void Page_HasAlwaysVisibleUpcomingPastToggle()
    {
        var source = ReadSource(Page);

        Assert.Contains("Text=\"Upcoming Meetups\"", source, StringComparison.Ordinal);
        Assert.Contains("Text=\"Past Meetups\"", source, StringComparison.Ordinal);
        Assert.Contains("SelectMeetupListCommand", source, StringComparison.Ordinal);
        // FR-003: the heading is no longer hidden when the upcoming list is empty.
        Assert.DoesNotContain("Text=\"Upcoming Meetups\" FontAttributes=\"Bold\" FontSize=\"16\" Margin=\"0,8,0,4\"\n                    IsVisible=\"{Binding HasMeetups}\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Toggle_IsASlidingSwitchWithNoTriggerDrivenColors()
    {
        var page = ReadSource(Page);
        var codeBehind = ReadSource(CodeBehind);
        var viewModel = ReadSource(ViewModel);

        // Regression: DataTrigger setters holding AppThemeBinding values were not
        // reverted, leaving both toggle options looking selected.
        Assert.DoesNotContain("<DataTrigger", page, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"MeetupListSwitchThumb\"", page, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding SelectedListLabel}\"", page, StringComparison.Ordinal);
        Assert.Contains("MeetupListSwitchThumb.TranslateToAsync(", codeBehind, StringComparison.Ordinal);
        Assert.Contains("nameof(GroupDetailViewModel.IsPastSelected)", codeBehind, StringComparison.Ordinal);
        Assert.Contains("SelectedListLabel => IsPastSelected ? \"Past Meetups\" : \"Upcoming Meetups\"", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void Page_ShowsBothListsThroughOneSharedCardTemplate()
    {
        var source = ReadSource(Page);

        Assert.Contains("BindableLayout.ItemsSource=\"{Binding Meetups}\"", source, StringComparison.Ordinal);
        Assert.Contains("BindableLayout.ItemsSource=\"{Binding PastMeetups}\"", source, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"MeetupCardTemplate\"", source, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(source, "BindableLayout.ItemTemplate=\"{StaticResource MeetupCardTemplate}\""));
        // iOS release crash: no CollectionView inside the page's ScrollView.
        Assert.DoesNotContain("<CollectionView", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Page_HasEmptyStatesRefreshAndLoadMoreRetry()
    {
        var source = ReadSource(Page);

        Assert.Contains("No upcoming meetups", source, StringComparison.Ordinal);
        Assert.Contains("No past meetups yet", source, StringComparison.Ordinal);
        Assert.Contains("<RefreshView", source, StringComparison.Ordinal);
        Assert.Contains("Command=\"{Binding RefreshMeetupsCommand}\"", source, StringComparison.Ordinal);
        Assert.Contains("Couldn't load more — tap to retry", source, StringComparison.Ordinal);
        Assert.Contains("LoadMorePastMeetupsCommand", source, StringComparison.Ordinal);
        Assert.Contains("Scrolled=\"OnContentScrolled\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CodeBehind_ResetsModeOnNavigationNotOnAppearing()
    {
        var source = ReadSource(CodeBehind);

        var applyIndex = source.IndexOf("ApplyQueryAttributes", StringComparison.Ordinal);
        var appearingIndex = source.IndexOf("OnAppearing", StringComparison.Ordinal);
        var resetIndex = source.IndexOf("ResetListMode()", StringComparison.Ordinal);
        Assert.True(applyIndex >= 0 && appearingIndex >= 0 && resetIndex >= 0);
        // FR-002: reset when the page is opened, not when returning from detail/edit.
        Assert.True(resetIndex > applyIndex && resetIndex < appearingIndex,
            "ResetListMode must be called from ApplyQueryAttributes, before OnAppearing.");
        Assert.Equal(1, CountOccurrences(source, "ResetListMode()"));
        Assert.Contains("LoadMorePastMeetupsCommand", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ViewModel_PagesPastMeetupsOnTheMainThread()
    {
        var source = ReadSource(ViewModel);

        Assert.Contains("GetPastMeetupsAsync", source, StringComparison.Ordinal);
        Assert.Contains("_pastNextCursor", source, StringComparison.Ordinal);
        Assert.Contains("ObservableCollection<MeetupSummary> PastMeetups", source, StringComparison.Ordinal);
        Assert.Contains("MainThread.InvokeOnMainThreadAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MeetupDetail_LoadsSingleMeetupAndMapsNotFound()
    {
        var source = ReadSource(DetailViewModel);

        Assert.Contains("GetMeetupAsync(_groupId, _meetupId)", source, StringComparison.Ordinal);
        Assert.Contains("HttpStatusCode.NotFound", source, StringComparison.Ordinal);
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
