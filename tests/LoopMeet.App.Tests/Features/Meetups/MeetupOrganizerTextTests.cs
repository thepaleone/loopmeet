using LoopMeet.App.Features.Meetups;

namespace LoopMeet.App.Tests.Features.Meetups;

public sealed class MeetupOrganizerTextTests
{
    [Fact]
    public void ResolvedName_PassesThroughUnchanged()
    {
        Assert.Equal("Ada Lovelace", MeetupOrganizerText.Format("Ada Lovelace"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void UnresolvedName_YieldsThePlaceholder(string? displayName)
    {
        // FR-011: never a blank field, never a raw identifier.
        Assert.Equal("A group member", MeetupOrganizerText.Format(displayName));
        Assert.Equal(MeetupOrganizerText.UnknownOrganizer, MeetupOrganizerText.Format(displayName));
    }
}
