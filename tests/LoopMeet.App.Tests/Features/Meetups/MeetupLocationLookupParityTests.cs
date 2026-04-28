namespace LoopMeet.App.Tests.Features.Meetups;

public sealed class MeetupLocationLookupParityTests
{
    [Fact]
    public void CreateAndEditViewModels_ShareLocationLookupBehavior()
    {
        var createPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App/Features/Meetups/ViewModels/CreateMeetupViewModel.cs"));
        var editPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.App/Features/Meetups/ViewModels/EditMeetupViewModel.cs"));

        var createSource = File.ReadAllText(createPath);
        var editSource = File.ReadAllText(editPath);

        Assert.Contains("MeetupLocationLookupBehavior", createSource, StringComparison.Ordinal);
        Assert.Contains("MeetupLocationLookupBehavior", editSource, StringComparison.Ordinal);
        Assert.Contains("LocationSearchStatusMessage", createSource, StringComparison.Ordinal);
        Assert.Contains("LocationSearchStatusMessage", editSource, StringComparison.Ordinal);
    }
}
