namespace LoopMeet.App.Tests.Features.Meetups;

/// <summary>
/// Source-inspection assertions for markup and wiring that cannot be executed
/// off a MAUI host (repo pattern; cf. Features/Auth/Session/SessionSurfaceTests).
/// </summary>
public sealed class MeetupInteractionSurfaceTests
{
    private const string CreateForm = "src/LoopMeet.App/Features/Meetups/Views/CreateMeetupPage.xaml";
    private const string EditForm = "src/LoopMeet.App/Features/Meetups/Views/EditMeetupPage.xaml";

    // ── US1: icon save on the title row, no bottom button ─────────────────

    [Theory]
    [InlineData(CreateForm)]
    [InlineData(EditForm)]
    public void MeetupForm_HasIconSaveWithAccessibleDescription(string path)
    {
        var source = ReadSource(path);

        Assert.Contains("ic_save.png", source, StringComparison.Ordinal);
        Assert.Contains("SemanticProperties.Description", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(CreateForm)]
    [InlineData(EditForm)]
    public void MeetupForm_HasExactlyOneSaveAffordance(string path)
    {
        var source = ReadSource(path);

        // FR-003: the bottom full-width button is deleted, not hidden.
        Assert.Equal(1, CountOccurrences(source, "{Binding SaveCommand}"));
        Assert.DoesNotContain("<Button Text=\"Create Meetup\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("<Button Text=\"Save Changes\"", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(CreateForm)]
    [InlineData(EditForm)]
    public void MeetupForm_PutsTheSaveRowOutsideTheScrollView(string path)
    {
        var source = ReadSource(path);

        // FR-002: if the save control were inside the ScrollView it could be
        // scrolled away or covered by the keyboard — the whole point of US1.
        var saveIndex = source.IndexOf("ic_save.png", StringComparison.Ordinal);
        var scrollIndex = source.IndexOf("<ScrollView", StringComparison.Ordinal);
        Assert.True(saveIndex >= 0 && scrollIndex >= 0, "Both the save control and a ScrollView should exist.");
        Assert.True(saveIndex < scrollIndex, "The save control must appear before (outside) the ScrollView.");
    }

    [Theory]
    [InlineData("src/LoopMeet.App/Features/Meetups/ViewModels/CreateMeetupViewModel.cs")]
    [InlineData("src/LoopMeet.App/Features/Meetups/ViewModels/EditMeetupViewModel.cs")]
    public void MeetupViewModel_KeepsItsDuplicateSubmitGuard(string path)
    {
        var source = ReadSource(path);

        // FR-004: a small corner icon is easier to double-tap than the former
        // full-width button, and this guard is the only protection there is.
        var saveIndex = source.IndexOf("private async Task SaveAsync()", StringComparison.Ordinal);
        Assert.True(saveIndex >= 0, "SaveAsync should exist.");
        var guardIndex = source.IndexOf("if (IsBusy)", saveIndex, StringComparison.Ordinal);
        Assert.True(guardIndex > saveIndex, "SaveAsync must still open with its IsBusy re-entrancy guard.");
    }

    // ── US2/US3: cards open details; map control is the only other target ──

    [Theory]
    [InlineData(HomePage, "HomePageRoot")]
    [InlineData(GroupDetailPage, "GroupDetailPageRoot")]
    public void MeetupCard_OpensTheDetailsScreen(string path, string pageRoot)
    {
        var source = ReadSource(path);

        Assert.Contains($"OpenMeetupDetailCommand, Source={{x:Reference {pageRoot}}}", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HomePage)]
    [InlineData(GroupDetailPage)]
    public void MeetupCard_GatesItsMapControlOnOpenableLocation(string path)
    {
        var source = ReadSource(path);

        // FR-020: a place name without coordinates must not offer a maps tap.
        Assert.Contains("IsVisible=\"{Binding CanOpenLocation}\"", source, StringComparison.Ordinal);
        Assert.Contains("Open location in maps", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HomePage)]
    [InlineData(GroupDetailPage)]
    public void MeetupCard_LocationTextIsNotItsOwnTapTarget(string path)
    {
        var source = ReadSource(path);

        // FR-019/SC-010: no unlabelled tap boundary inside the card.
        Assert.DoesNotContain("<Label.GestureRecognizers>\n                                        <TapGestureRecognizer\n                                            Command=\"{Binding BindingContext.OpenLocationCommand", source, StringComparison.Ordinal);
        var locationLabelIndex = source.IndexOf("{Binding LocationDisplay}", StringComparison.Ordinal);
        Assert.True(locationLabelIndex >= 0, "The location label should exist.");
        // The only OpenLocationCommand binding must sit on the glyph, which
        // carries the accessibility description.
        Assert.Equal(1, CountOccurrences(source, "OpenLocationCommand"));
    }

    [Fact]
    public void GroupDetailPage_KeepsOwnerOnlySwipeToDelete()
    {
        var source = ReadSource(GroupDetailPage);

        // FR-018: unchanged by this feature.
        Assert.Contains("<SwipeView IsEnabled=\"{Binding BindingContext.IsOwner, Source={x:Reference GroupDetailPageRoot}}\">", source, StringComparison.Ordinal);
        Assert.Contains("DeleteMeetupCommand", source, StringComparison.Ordinal);
        // The card tap no longer jumps straight into editing.
        Assert.DoesNotContain("EditMeetupCommand, Source={x:Reference GroupDetailPageRoot}", source, StringComparison.Ordinal);
    }

    [Fact]
    public void GroupDetailViewModel_GatesMutationsButNotDetailNavigation()
    {
        var source = ReadSource("src/LoopMeet.App/Features/Groups/ViewModels/GroupDetailViewModel.cs");

        AssertOwnerGuarded(source, "DeleteMeetupAsync");
        AssertOwnerGuarded(source, "EditMeetupAsync");

        // FR-008: every member gets a response from tapping a meetup.
        var detailIndex = source.IndexOf("private Task OpenMeetupDetailAsync", StringComparison.Ordinal);
        Assert.True(detailIndex >= 0, "OpenMeetupDetailAsync should exist.");
        var body = source[detailIndex..source.IndexOf("[RelayCommand]", detailIndex, StringComparison.Ordinal)];
        Assert.DoesNotContain("!IsOwner", body, StringComparison.Ordinal);
    }

    [Fact]
    public void MeetupDetailPage_GatesTheEditControlOnOwnership()
    {
        var source = ReadSource("src/LoopMeet.App/Features/Meetups/Views/MeetupDetailPage.xaml");

        // FR-017: absent for non-owners, not disabled-but-present.
        Assert.Contains("IsVisible=\"{Binding IsOwner}\"", source, StringComparison.Ordinal);
        Assert.Contains("EditCommand", source, StringComparison.Ordinal);
        Assert.Contains("Edit meetup", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MeetupDetailPage_ShowsAllFiveFieldsAndTheGoneState()
    {
        var source = ReadSource("src/LoopMeet.App/Features/Meetups/Views/MeetupDetailPage.xaml");

        Assert.Contains("{Binding Title}", source, StringComparison.Ordinal);
        Assert.Contains("{Binding DateTimeDisplay}", source, StringComparison.Ordinal);
        Assert.Contains("{Binding LocationDisplay}", source, StringComparison.Ordinal);
        Assert.Contains("{Binding GroupName}", source, StringComparison.Ordinal);
        Assert.Contains("{Binding OrganizerDisplay}", source, StringComparison.Ordinal);
        Assert.Contains("{Binding IsNotFound}", source, StringComparison.Ordinal);
        Assert.Contains("no longer available", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MeetupDetailViewModel_LoadsFromIdsAndOffersNoDelete()
    {
        var source = ReadSource("src/LoopMeet.App/Features/Meetups/ViewModels/MeetupDetailViewModel.cs");

        // Deep-link ready: (groupId, meetupId) is all it needs.
        Assert.Contains("GetGroupMeetupsAsync(_groupId)", source, StringComparison.Ordinal);
        Assert.Contains("ApplyParameters", source, StringComparison.Ordinal);
        // FR-016: ownership comes from the group owner, never the creator.
        Assert.Contains("meetup.GroupOwnerUserId", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CreatedByUserId", source, StringComparison.Ordinal);
        // FR-014: read-only apart from the owner edit path — no delete command
        // and no call to the delete endpoint.
        Assert.DoesNotContain("DeleteMeetup", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DeleteCommand", source, StringComparison.Ordinal);
    }

    [Fact]
    public void MeetupDetailPage_ReReadsOnEveryArrival()
    {
        var source = ReadSource("src/LoopMeet.App/Features/Meetups/Views/MeetupDetailPage.xaml.cs");

        // FR-012: returning from an edit must show the new values.
        Assert.Contains("OnAppearing", source, StringComparison.Ordinal);
        Assert.Contains("LoadCommand.Execute", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AppShell_RegistersTheMeetupDetailRoute()
    {
        var source = ReadSource("src/LoopMeet.App/AppShell.xaml.cs");

        Assert.Contains("Routing.RegisterRoute(\"meetup-detail\"", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("src/LoopMeet.App/Features/Home/ViewModels/HomeViewModel.cs")]
    [InlineData("src/LoopMeet.App/Features/Groups/ViewModels/GroupDetailViewModel.cs")]
    public void ViewModel_DefersToCanOpenLocationRatherThanItsOwnGuard(string path)
    {
        var source = ReadSource(path);

        // One definition of "openable" so a card and the details screen cannot disagree.
        Assert.Contains("CanOpenLocation: true", source, StringComparison.Ordinal);
        Assert.DoesNotContain("meetup.Latitude is null || meetup.Longitude is null", source, StringComparison.Ordinal);
    }

    private static void AssertOwnerGuarded(string source, string methodName)
    {
        var index = source.IndexOf(methodName, StringComparison.Ordinal);
        Assert.True(index >= 0, $"{methodName} should exist.");
        var guardIndex = source.IndexOf("!IsOwner", index, StringComparison.Ordinal);
        Assert.True(guardIndex > index && guardIndex - index < 200, $"{methodName} must remain owner-gated.");
    }

    private const string HomePage = "src/LoopMeet.App/Features/Home/Views/HomePage.xaml";
    private const string GroupDetailPage = "src/LoopMeet.App/Features/Groups/Views/GroupDetailPage.xaml";

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
