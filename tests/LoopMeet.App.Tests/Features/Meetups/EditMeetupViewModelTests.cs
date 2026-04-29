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
}
