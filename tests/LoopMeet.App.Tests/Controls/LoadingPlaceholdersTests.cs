using LoopMeet.App.Controls;

namespace LoopMeet.App.Tests.Controls;

public sealed class LoadingPlaceholdersTests
{
    [Theory]
    [InlineData(false, 0, true)]
    [InlineData(false, 1, false)]
    [InlineData(false, 3, false)]
    [InlineData(true, 0, false)]
    [InlineData(true, 2, false)]
    public void ShowsOnlyBeforeTheFirstLoadAndWhileNothingIsListed(bool hasCompletedLoad, int itemCount, bool expected)
    {
        Assert.Equal(expected, LoadingPlaceholders.ShouldShow(hasCompletedLoad, itemCount));
    }
}
