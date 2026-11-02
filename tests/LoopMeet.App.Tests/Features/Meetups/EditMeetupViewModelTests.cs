namespace LoopMeet.App.Tests.Features.Meetups;

public sealed class EditMeetupViewModelTests
{
    [Fact]
    public void EditMeetupViewModel_UsesLocationBiasWhenSearching()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App/Features/Meetups/ViewModels/EditMeetupViewModel.cs"));
        var source = File.ReadAllText(path);

        Assert.Contains("GetLookupContextAsync", source, StringComparison.Ordinal);
        Assert.Contains("AutocompleteAsync(query, lookupContext.Latitude, lookupContext.Longitude, lookupContext.RadiusMeters)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void EditMeetupViewModel_HasPermissionAwareFallbackMessage()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App/Features/Meetups/ViewModels/EditMeetupViewModel.cs"));
        var source = File.ReadAllText(path);

        Assert.Contains("LocationSearchStatusMessage", source, StringComparison.Ordinal);
        Assert.Contains("lookupContext.FallbackMessage", source, StringComparison.Ordinal);
    }

    [Fact]
    public void EditMeetupViewModel_LoadsTheSingleMeetup_AndSendsTimeAndTimezoneOnlyWhenChanged()
    {
        var source = ReadEditViewModel();

        Assert.Contains("GetMeetupAsync(GroupId, MeetupId)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("GetGroupMeetupsAsync", source, StringComparison.Ordinal);
        Assert.Contains("ScheduledAt = timeChanged ? scheduledAt : null", source, StringComparison.Ordinal);
        // FR-021a: an edit that keeps the time must not rewrite the timezone.
        Assert.Contains("Timezone = timeChanged ?", source, StringComparison.Ordinal);
    }

    [Fact]
    public void EditMeetupViewModel_FutureTimeRuleSkipsPastMeetups()
    {
        var source = ReadEditViewModel();

        // FR-018/FR-019/FR-020: only a changed time on an upcoming meetup must be in
        // the future, so a past meetup can be moved anywhere, including forward.
        Assert.Contains("if (timeChanged && !_loadedIsPast && scheduledAt <= DateTimeOffset.Now)", source, StringComparison.Ordinal);
        Assert.Equal(1, source.Split("Scheduled time must be in the future.").Length - 1);
    }

    private static string ReadEditViewModel() =>
        File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../../src/LoopMeet.App/Features/Meetups/ViewModels/EditMeetupViewModel.cs")));
}
