using LoopMeet.App.Features.Profile;

namespace LoopMeet.App.Tests.Features.Profile;

public sealed class ProfileLayoutTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(375, false)]
    [InlineData(799.99, false)]
    [InlineData(800, true)]
    [InlineData(956, true)]
    [InlineData(1366, true)]
    public void UsesTwoColumns_FromEightHundredPoints(double width, bool expected)
    {
        Assert.Equal(expected, ProfileLayout.UsesTwoColumns(width));
    }

    [Fact]
    public void TwoColumnMinWidth_IsEightHundred()
    {
        Assert.Equal(800, ProfileLayout.TwoColumnMinWidth);
    }

    [Theory]
    [InlineData(false, 600)]
    [InlineData(true, 1100)]
    public void MaxContentWidth_KeepsRowsReadable(bool twoColumns, double expected)
    {
        Assert.Equal(expected, ProfileLayout.MaxContentWidth(twoColumns));
    }
}
