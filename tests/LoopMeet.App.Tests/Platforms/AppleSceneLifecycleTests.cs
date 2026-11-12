using System.Xml.Linq;

namespace LoopMeet.App.Tests.Platforms;

/// <summary>
/// Apps built with the Xcode 27 SDK must use the UIScene lifecycle or they crash at
/// launch (feature 013). iOS and Mac Catalyst can't be launched in CI, so the
/// configuration that opts in is pinned by source inspection.
/// </summary>
public sealed class AppleSceneLifecycleTests
{
    private const string AppRoot = "src/LoopMeet.App";

    [Theory]
    [InlineData("iOS")]
    [InlineData("MacCatalyst")]
    public void InfoPlist_DeclaresSingleMauiSceneWithSceneDelegate(string platform)
    {
        var root = ReadPlistRoot($"{AppRoot}/Platforms/{platform}/Info.plist");

        var manifest = ValueFor(root, "UIApplicationSceneManifest");
        Assert.Equal("false", ValueFor(manifest, "UIApplicationSupportsMultipleScenes").Name.LocalName);

        var roles = ValueFor(manifest, "UISceneConfigurations");
        var configuration = Assert.Single(ValueFor(roles, "UIWindowSceneSessionRoleApplication").Elements("dict"));
        // MAUI looks the configuration up by this exact name.
        Assert.Equal("__MAUI_DEFAULT_SCENE_CONFIGURATION__", ValueFor(configuration, "UISceneConfigurationName").Value);
        Assert.Equal("SceneDelegate", ValueFor(configuration, "UISceneDelegateClassName").Value);
        Assert.DoesNotContain(configuration.Elements("key"), key => key.Value == "UISceneStoryboardFile");
    }

    [Theory]
    [InlineData("iOS")]
    [InlineData("MacCatalyst")]
    public void SceneDelegate_IsRegisteredUnderThePlistNameAndDerivesFromMaui(string platform)
    {
        var source = ReadSource($"{AppRoot}/Platforms/{platform}/SceneDelegate.cs");

        Assert.Contains("[Register(\"SceneDelegate\")]", source, StringComparison.Ordinal);
        Assert.Contains("public class SceneDelegate : MauiUISceneDelegate", source, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("iOS")]
    [InlineData("MacCatalyst")]
    public void AppDelegate_StaysTheApplicationDelegate(string platform)
    {
        var appDelegate = ReadSource($"{AppRoot}/Platforms/{platform}/AppDelegate.cs");
        var program = ReadSource($"{AppRoot}/Platforms/{platform}/Program.cs");

        Assert.Contains("public class AppDelegate : MauiUIApplicationDelegate", appDelegate, StringComparison.Ordinal);
        Assert.Contains("UIApplication.Main(args, null, typeof(AppDelegate));", program, StringComparison.Ordinal);
    }

    [Fact]
    public void Project_UsesAMauiVersionWithSceneSupport_AndTheXcode27MinimumOsVersions()
    {
        var project = XDocument.Parse(ReadSource($"{AppRoot}/LoopMeet.App.csproj"));

        var maui = project.Descendants("PackageReference")
            .Single(reference => (string?)reference.Attribute("Include") == "Microsoft.Maui.Controls");
        // 10.0.110 is the first release that forwards scene callbacks (e.g. to WebAuthenticator).
        Assert.True(Version.Parse((string)maui.Attribute("Version")!) >= new Version(10, 0, 110),
            "Microsoft.Maui.Controls must be 10.0.110 or later for the UIScene lifecycle.");

        Assert.Equal("15.0", MinimumOsVersion(project, "ios"));
        Assert.Equal("17.0", MinimumOsVersion(project, "maccatalyst"));
    }

    private static string MinimumOsVersion(XDocument project, string platform) =>
        project.Descendants("SupportedOSPlatformVersion")
            .Single(element => ((string?)element.Attribute("Condition"))?.EndsWith($"== '{platform}'", StringComparison.Ordinal) == true)
            .Value;

    private static XElement ReadPlistRoot(string repoRelativePath) =>
        XDocument.Parse(ReadSource(repoRelativePath)).Root!.Element("dict")!;

    /// <summary>Returns the plist value element that follows <c>&lt;key&gt;name&lt;/key&gt;</c> in a dict.</summary>
    private static XElement ValueFor(XElement dict, string key)
    {
        var keyElement = dict.Elements("key").SingleOrDefault(element => element.Value == key);
        Assert.True(keyElement is not null, $"Missing plist key {key}.");
        return keyElement!.ElementsAfterSelf().First();
    }

    private static string ReadSource(string repoRelativePath) =>
        File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../..", repoRelativePath)));
}
