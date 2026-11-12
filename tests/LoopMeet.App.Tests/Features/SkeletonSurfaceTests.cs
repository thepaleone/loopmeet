using System.Text.RegularExpressions;

namespace LoopMeet.App.Tests.Features;

// 019: one shared placeholder component on Home, group detail and the Groups list, shown only while a
// section has nothing to display. MAUI/XAML wiring, so verified by source inspection; the
// quickstart covers how it looks on a device.
public sealed class SkeletonSurfaceTests
{
    [Theory]
    [InlineData("src/LoopMeet.App/Features/Home/Views/HomePage.xaml", "HomeMeetupCard", 3, "ShowMeetupsSkeleton")]
    [InlineData("src/LoopMeet.App/Features/Groups/Views/GroupDetailPage.xaml", "MemberTile", 4, "ShowMembersSkeleton")]
    [InlineData("src/LoopMeet.App/Features/Groups/Views/GroupDetailPage.xaml", "GroupMeetupCard", 2, "ShowUpcomingSkeleton")]
    [InlineData("src/LoopMeet.App/Features/Groups/Views/GroupDetailPage.xaml", "GroupMeetupCard", 2, "ShowPastSkeleton")]
    [InlineData("src/LoopMeet.App/Features/Groups/Views/GroupsListPage.xaml", "GroupCard", 3, "ShowOwnedGroupsSkeleton")]
    [InlineData("src/LoopMeet.App/Features/Groups/Views/GroupsListPage.xaml", "GroupCard", 2, "ShowMemberGroupsSkeleton")]
    public void Page_UsesTheSharedSkeletonForEachSection(string page, string variant, int count, string property)
    {
        var source = ReadSource(page);

        Assert.Contains("xmlns:controls=\"clr-namespace:LoopMeet.App.Controls\"", source, StringComparison.Ordinal);
        Assert.Matches(
            new Regex($@"<controls:SkeletonList\s+Variant=""{variant}""\s+Count=""{count}""\s+IsActive=""\{{Binding {property}\}}"""),
            source);
    }

    [Theory]
    [InlineData("src/LoopMeet.App/Features/Home/ViewModels/HomeViewModel.cs", "ShowMeetupsSkeleton")]
    [InlineData("src/LoopMeet.App/Features/Groups/ViewModels/GroupDetailViewModel.cs", "ShowMembersSkeleton")]
    [InlineData("src/LoopMeet.App/Features/Groups/ViewModels/GroupDetailViewModel.cs", "ShowUpcomingSkeleton")]
    [InlineData("src/LoopMeet.App/Features/Groups/ViewModels/GroupDetailViewModel.cs", "ShowPastSkeleton")]
    [InlineData("src/LoopMeet.App/Features/Groups/ViewModels/GroupsListViewModel.cs", "ShowOwnedGroupsSkeleton")]
    [InlineData("src/LoopMeet.App/Features/Groups/ViewModels/GroupsListViewModel.cs", "ShowMemberGroupsSkeleton")]
    public void ViewModel_DecidesWithTheSharedRule(string viewModel, string property)
    {
        var source = ReadSource(viewModel);

        Assert.Matches(new Regex($@"public bool {property}\s*=>[^;]*LoadingPlaceholders\.ShouldShow\("), source);
    }

    [Theory]
    [InlineData("src/LoopMeet.App/Features/Home/ViewModels/HomeViewModel.cs", "_hasLoadedOnce")]
    [InlineData("src/LoopMeet.App/Features/Groups/ViewModels/GroupsListViewModel.cs", "_hasLoadedOnce")]
    [InlineData("src/LoopMeet.App/Features/Groups/ViewModels/GroupDetailViewModel.cs", "_membersLoaded")]
    public void ViewModel_EndsPlaceholdersHoweverTheLoadEnds(string viewModel, string flag)
    {
        var source = ReadSource(viewModel);

        Assert.Matches(new Regex($@"finally\s*\{{[^}}]*{flag} = true;"), source);
    }

    [Fact]
    public void GroupsList_HandWrittenSkeletonsAndTheirShimmerAreGone()
    {
        var page = ReadSource("src/LoopMeet.App/Features/Groups/Views/GroupsListPage.xaml");
        var codeBehind = ReadSource("src/LoopMeet.App/Features/Groups/Views/GroupsListPage.xaml.cs");
        var viewModel = ReadSource("src/LoopMeet.App/Features/Groups/ViewModels/GroupsListViewModel.cs");

        foreach (var leftover in new[] { "Shimmer", "#E2E8F0", "#F8FAFC", "IsLoadingOwnedGroups", "IsLoadingMemberGroups", "DataTrigger" })
        {
            Assert.DoesNotContain(leftover, page, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("Shimmer", codeBehind, StringComparison.Ordinal);
        Assert.DoesNotContain("IsLoadingOwnedGroups", viewModel, StringComparison.Ordinal);
        Assert.DoesNotContain("IsLoadingMemberGroups", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void SkeletonList_ShimmersOnlyWhileActiveAndLoaded()
    {
        var source = ReadSource("src/LoopMeet.App/Controls/SkeletonList.cs");

        foreach (var variant in new[] { "HomeMeetupCard", "GroupMeetupCard", "MemberTile", "GroupCard" })
        {
            Assert.Contains($"SkeletonVariant.{variant}", source, StringComparison.Ordinal);
        }

        Assert.Contains("Loaded += ", source, StringComparison.Ordinal);
        Assert.Contains("Unloaded += ", source, StringComparison.Ordinal);
        Assert.Contains("IsActive && IsLoaded", source, StringComparison.Ordinal);
        Assert.DoesNotContain("#E2E8F0", source, StringComparison.Ordinal);

        // The shimmer sweeps inside each bar and avatar, which clip it. A band laid over a whole
        // card swept as a block the card's full height.
        Assert.Contains("Content = band", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Children = { content, band }", source, StringComparison.Ordinal);
    }

    private static string ReadSource(string repoRelativePath) =>
        File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../", repoRelativePath)));
}
