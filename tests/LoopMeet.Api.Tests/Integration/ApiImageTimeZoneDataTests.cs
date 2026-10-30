namespace LoopMeet.Api.Tests.Integration;

/// <summary>
/// Regression guard for the 012 staging outage: the Alpine runtime image had no
/// /usr/share/zoneinfo, so resolving America/Los_Angeles threw on every meetup
/// list request. Tests run on hosts that do have zone data, so the image itself
/// is pinned by source inspection.
/// </summary>
public sealed class ApiImageTimeZoneDataTests
{
    [Fact]
    public void RuntimeImage_InstallsTimeZoneData()
    {
        var dockerfile = ReadSource("src/LoopMeet.Api/Dockerfile");

        var runtimeStage = dockerfile.IndexOf("AS runtime", StringComparison.Ordinal);
        var tzdata = dockerfile.IndexOf("apk add --no-cache tzdata", StringComparison.Ordinal);
        Assert.True(runtimeStage >= 0, "Dockerfile must have a runtime stage.");
        Assert.True(tzdata > runtimeStage, "The runtime stage must install tzdata.");
    }

    [Fact]
    public void Startup_FailsFastWhenZoneDataIsMissing()
    {
        var program = ReadSource("src/LoopMeet.Api/Program.cs");

        var check = program.IndexOf("TimeZoneInfo.FindSystemTimeZoneById(MeetupLifecycle.DefaultTimezone);", StringComparison.Ordinal);
        var build = program.IndexOf("var app = builder.Build();", StringComparison.Ordinal);
        Assert.True(check >= 0 && check < build, "Program.cs must resolve the default meetup timezone before building the app.");
    }

    private static string ReadSource(string repoRelativePath) =>
        File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../..", repoRelativePath)));
}
