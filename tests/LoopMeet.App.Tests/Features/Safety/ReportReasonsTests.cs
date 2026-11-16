using LoopMeet.App.Features.Safety;

namespace LoopMeet.App.Tests.Features.Safety;

public sealed class ReportReasonsTests
{
    [Fact]
    public void ListsTheFiveReasonsInOrderWithTheApiCodes()
    {
        Assert.Equal(
            new[]
            {
                ("inappropriate_photo_or_name", "Inappropriate photo or name"),
                ("harassment_or_threats", "Harassment or threats"),
                ("child_safety", "Child safety"),
                ("spam", "Spam"),
                ("other", "Other")
            },
            ReportReasons.All.Select(reason => (reason.Code, reason.Label)));
    }

    [Fact]
    public void ThePickerShowsTheLabel()
    {
        Assert.Equal("Child safety", ReportReasons.All[2].ToString());
    }
}
