using LoopMeet.App.Features.Profile;

namespace LoopMeet.App.Tests.Features.Profile;

public sealed class AppVersionTextTests
{
    [Fact]
    public void FormatsTheDisplayVersionAndBuildNumber()
    {
        Assert.Equal("Version 0.1.3 (4)", AppVersionText.Format("0.1.3", "4"));
    }

    [Fact]
    public void TrimsWhitespaceFromThePlatformValues()
    {
        Assert.Equal("Version 0.1.3 (4)", AppVersionText.Format(" 0.1.3\n", " 4 "));
    }
}
