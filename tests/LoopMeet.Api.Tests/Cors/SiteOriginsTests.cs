using System.Text.Json;
using LoopMeet.Api.Cors;
using Xunit;

namespace LoopMeet.Api.Tests.Cors;

/// <summary>
/// Drives <see cref="SiteOrigins.IsAllowed"/> from the case file the apple-authorization
/// Deno tests also read, so the C# and TypeScript origin rules cannot drift (017 research D6).
/// </summary>
public sealed class SiteOriginsTests
{
    private static readonly string CasesPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../supabase/functions/apple-authorization/site-origins.cases.json"));

    public static IEnumerable<object[]> Cases()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(CasesPath));
        foreach (var item in document.RootElement.EnumerateArray())
        {
            yield return new object[]
            {
                item.GetProperty("name").GetString()!,
                item.GetProperty("origin").GetString()!,
                item.GetProperty("patterns").EnumerateArray().Select(p => p.GetString()!).ToArray(),
                item.GetProperty("expectedAllowed").GetBoolean()
            };
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void IsAllowed_MatchesSharedCase(string name, string origin, string[] patterns, bool expectedAllowed)
    {
        var actual = SiteOrigins.IsAllowed(origin, patterns);

        Assert.True(expectedAllowed == actual, $"{name}: expected {expectedAllowed}, got {actual}");
    }

    [Fact]
    public void SharedCasesKeepCoveringTheRule()
    {
        Assert.True(Cases().Count() >= 20, "site-origins.cases.json must keep at least 20 cases.");
    }

    [Theory]
    [InlineData("https://loopmeet.io")]
    [InlineData("https://loopmeet-account.joelgray.workers.dev")]
    [InlineData("https://main-loopmeet-account.joelgray.workers.dev")]
    public void ShippedSettingsAllowTheLiveSiteAndItsPreviews(string origin)
    {
        // The test host runs as Development, so read the production list from the file itself.
        var settingsPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "../../../../../src/LoopMeet.Api/appsettings.json"));
        using var document = JsonDocument.Parse(File.ReadAllText(settingsPath));
        var allowed = SiteOrigins.Parse(
            document.RootElement.GetProperty("Cors").GetProperty("AllowedOrigins").GetString());

        Assert.True(SiteOrigins.IsAllowed(origin, allowed), $"{origin} is missing from Cors:AllowedOrigins.");
    }

    [Fact]
    public void Parse_SplitsOnCommasTrimsAndDropsEmptyEntries()
    {
        Assert.Equal(["https://a.test", "https://*-b.test"], SiteOrigins.Parse(" https://a.test, ,https://*-b.test ,"));
        Assert.Empty(SiteOrigins.Parse(null));
        Assert.Empty(SiteOrigins.Parse(""));
    }
}
