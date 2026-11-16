using LoopMeet.App.Features.Safety;

namespace LoopMeet.App.Tests.Features.Safety;

public sealed class ReportDraftTests
{
    private static readonly Guid GroupId = Guid.NewGuid();

    [Fact]
    public void CanSubmitWithAGroupAReasonAndDetails()
    {
        Assert.True(ReportDraft.CanSubmit(GroupId, "spam", "They keep posting ads."));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CannotSubmitBlankDetails(string? details)
    {
        Assert.False(ReportDraft.CanSubmit(GroupId, "spam", details));
    }

    [Fact]
    public void CannotSubmitWithoutAGroupOrAReason()
    {
        Assert.False(ReportDraft.CanSubmit(null, "spam", "details"));
        Assert.False(ReportDraft.CanSubmit(GroupId, null, "details"));
        Assert.False(ReportDraft.CanSubmit(GroupId, "", "details"));
    }

    [Fact]
    public void AllowsExactlyOneThousandCharactersAfterTrimming()
    {
        Assert.True(ReportDraft.CanSubmit(GroupId, "other", new string('x', 1000)));
        Assert.True(ReportDraft.CanSubmit(GroupId, "other", " " + new string('x', 1000) + " "));
        Assert.False(ReportDraft.CanSubmit(GroupId, "other", new string('x', 1001)));
    }

    [Theory]
    [InlineData(null, "1,000 characters left")]
    [InlineData("", "1,000 characters left")]
    [InlineData("Hello", "995 characters left")]
    public void RemainingCountsDown(string? details, string expected)
    {
        Assert.Equal(expected, ReportDraft.Remaining(details));
    }

    [Fact]
    public void RemainingNeverGoesBelowZeroAndSaysOneCharacter()
    {
        Assert.Equal("1 character left", ReportDraft.Remaining(new string('x', 999)));
        Assert.Equal("0 characters left", ReportDraft.Remaining(new string('x', 1000)));
    }

    [Fact]
    public void TrimmedRemovesSurroundingSpaces()
    {
        Assert.Equal("Rude name", ReportDraft.Trimmed("  Rude name \n"));
        Assert.Equal(string.Empty, ReportDraft.Trimmed(null));
    }
}
