namespace LoopMeet.Api.Tests.Contract;

// IdentityModel's ShowPII puts token details, which can include an email, into validation error
// logs. data-retention.md promises Api logs never contain emails, so it stays Development-only.
public sealed class LoggingPrivacyTests
{
    [Fact]
    public void ShowPiiIsOnlyEnabledInDevelopment()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.Api/Program.cs"));
        var source = File.ReadAllText(path);

        var assignment = source.IndexOf("IdentityModelEventSource.ShowPII = true;", StringComparison.Ordinal);
        Assert.True(assignment >= 0, "ShowPII assignment not found; update this test if it was removed");
        Assert.Equal(1, source.Split("ShowPII").Length - 1);

        var guard = source.LastIndexOf("if (isDevelopment)", assignment, StringComparison.Ordinal);
        Assert.True(guard >= 0, "ShowPII must be set inside an isDevelopment check");
        var block = source[guard..assignment];
        Assert.DoesNotContain("}", block, StringComparison.Ordinal);
    }
}
