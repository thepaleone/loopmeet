using System.Text.RegularExpressions;

namespace LoopMeet.App.Tests.Features.Groups;

/// <summary>
/// Source inspection of Leave group (feature 023 US2). The confirmation and navigation run only
/// on a device, so these tests read the XAML, the ViewModel and the Refit interface.
/// </summary>
public sealed class GroupLeaveSurfaceTests
{
    private const string GroupDetailPage = "src/LoopMeet.App/Features/Groups/Views/GroupDetailPage.xaml";
    private const string GroupDetailViewModel = "src/LoopMeet.App/Features/Groups/ViewModels/GroupDetailViewModel.cs";
    private const string GroupsApi = "src/LoopMeet.App/Services/GroupsApi.cs";

    [Fact]
    public void GroupDetail_HasALeaveButtonForNonOwnersBelowTheMembers()
    {
        var page = ReadSource(GroupDetailPage);
        var button = Regex.Match(page, "<Button[^>]*Text=\"Leave group\"[^>]*/>").Value;

        Assert.False(string.IsNullOrEmpty(button), "Leave group button not found");
        Assert.Contains("Command=\"{Binding LeaveGroupCommand}\"", button, StringComparison.Ordinal);
        Assert.Contains("IsVisible=\"{Binding CanLeave}\"", button, StringComparison.Ordinal);
        Assert.True(
            page.IndexOf("BindableLayout.ItemsSource=\"{Binding Members}\"", StringComparison.Ordinal)
            < page.IndexOf("Text=\"Leave group\"", StringComparison.Ordinal),
            "Leave group sits below the members");
    }

    [Fact]
    public void LeaveGroup_ConfirmsNamingTheGroupThenLeavesAndReturnsToGroups()
    {
        var method = MethodBody(ReadSource(GroupDetailViewModel), "LeaveGroupAsync()");

        Assert.Contains("DisplayAlert", method, StringComparison.Ordinal);
        Assert.Contains("GroupName", method, StringComparison.Ordinal);
        Assert.Contains("new invitation", method, StringComparison.Ordinal);
        Assert.Contains("_groupsApi.LeaveGroupAsync(GroupId)", method, StringComparison.Ordinal);
        Assert.Contains("GoToAsync(\"//groups\")", method, StringComparison.Ordinal);
        Assert.True(
            method.IndexOf("DisplayAlert", StringComparison.Ordinal)
            < method.IndexOf("_groupsApi.LeaveGroupAsync", StringComparison.Ordinal),
            "the confirmation comes before the call");
    }

    [Fact]
    public void LeaveGroup_TreatsNotAMemberAsAlreadyLeft()
    {
        var method = MethodBody(ReadSource(GroupDetailViewModel), "LeaveGroupAsync()");

        Assert.Contains("SafetyErrorText.MeansAlreadyLeft", method, StringComparison.Ordinal);
    }

    [Fact]
    public void CanLeave_IsOnlyForNonOwners()
    {
        var viewModel = ReadSource(GroupDetailViewModel);

        Assert.Matches(@"public bool CanLeave\s*=>\s*!IsOwner", viewModel);
        Assert.Contains("[NotifyPropertyChangedFor(nameof(CanLeave))]", viewModel, StringComparison.Ordinal);
    }

    [Fact]
    public void GroupsApi_DeclaresTheLeaveRoute()
    {
        var api = ReadSource(GroupsApi);

        Assert.Contains("[Post(\"/groups/{groupId}/leave\")]", api, StringComparison.Ordinal);
        Assert.Contains("Task LeaveGroupAsync(Guid groupId)", api, StringComparison.Ordinal);
    }

    private static string MethodBody(string source, string signature)
    {
        var at = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(at >= 0, $"{signature} not found");
        var open = source.IndexOf('{', at);
        var depth = 0;
        for (var i = open; i < source.Length; i++)
        {
            depth += source[i] switch { '{' => 1, '}' => -1, _ => 0 };
            if (depth == 0)
            {
                return source[open..(i + 1)];
            }
        }

        return source[open..];
    }

    private static string ReadSource(string repoRelativePath) =>
        File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../..", repoRelativePath)));
}
