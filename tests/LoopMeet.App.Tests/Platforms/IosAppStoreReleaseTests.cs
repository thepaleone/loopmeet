using System.Xml.Linq;

namespace LoopMeet.App.Tests.Platforms;

/// <summary>
/// Release-readiness checks for the App Store / TestFlight build (feature 021):
/// the signed-ipa script and its pre-build guards, the privacy manifest, export
/// compliance, App Transport Security and the Release entitlements. None of these
/// can run off a Mac or a device, so they inspect the source.
/// </summary>
public sealed class IosAppStoreReleaseTests
{
    private const string InfoPlist = "src/LoopMeet.App/Platforms/iOS/Info.plist";
    private const string PrivacyManifest = "src/LoopMeet.App/Platforms/iOS/Resources/PrivacyInfo.xcprivacy";
    private const string ReleaseEntitlements = "src/LoopMeet.App/Platforms/iOS/Entitlements.Release.plist";
    private const string Project = "src/LoopMeet.App/LoopMeet.App.csproj";
    private const string ReleaseScript = "deploy/build-ios-release.sh";

    [Fact]
    public void ReleaseScript_SignsWithPinnedIdentityAndProfileFromEnvironment()
    {
        var script = ReadSource(ReleaseScript);

        Assert.Contains("-c Release", script, StringComparison.Ordinal);
        Assert.Contains("-f net10.0-ios", script, StringComparison.Ordinal);
        Assert.Contains("-r ios-arm64", script, StringComparison.Ordinal);
        Assert.Contains("-p:ApplicationVersion=\"$BUILD_NUMBER\"", script, StringComparison.Ordinal);
        // The SHA-1, never the certificate name: the name matches more than one keychain entry.
        Assert.Contains("-p:CodesignKey=\"$LOOPMEET_IOS_SIGNING_IDENTITY\"", script, StringComparison.Ordinal);
        Assert.Contains("-p:CodesignProvision=", script, StringComparison.Ordinal);
        Assert.Contains("artifacts/ios/$BUILD_NUMBER", script, StringComparison.Ordinal);
        // iOS has no JIT, so the Android AOT/trimming workaround must not be copied (research D2).
        Assert.DoesNotContain("RunAOTCompilation=false", script, StringComparison.Ordinal);
        Assert.DoesNotContain("PublishTrimmed=false", script, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("^[1-9][0-9]*$")]                            // 1: positive build number
    [InlineData("LOOPMEET_IOS_SIGNING_IDENTITY")]            // 2: variable set
    [InlineData("LOOPMEET_IOS_PROVISIONING_PROFILE")]        // 2: variable set
    [InlineData("^[0-9A-Fa-f]{40}$")]                        // 3: SHA-1 form
    [InlineData("security find-identity -v -p codesigning")] // 4: identity in the keychain
    [InlineData("security cms -D")]                          // 5: profile decodes
    [InlineData(".io.loopmeet.app")]                         // 6: App ID
    [InlineData("ProvisionedDevices")]                       // 7: App Store, not development or ad hoc
    [InlineData("aps-environment")]                          // 7: production push
    [InlineData("ExpirationDate")]                           // 8: not expired
    [InlineData("DeveloperCertificates")]                    // 9: profile includes the identity
    public void ReleaseScript_ChecksEveryContractConditionBeforeBuilding(string guard)
    {
        var (beforeBuild, _) = SplitAtFirstDotnetCall(ReadSource(ReleaseScript));

        Assert.Contains(guard, beforeBuild, StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseScript_VerifiesBuildNumberAndPushEnvironmentAfterBuilding()
    {
        var (_, afterBuild) = SplitAtFirstDotnetCall(ReadSource(ReleaseScript));

        Assert.Contains("CFBundleVersion", afterBuild, StringComparison.Ordinal);
        Assert.Contains("CFBundleShortVersionString", afterBuild, StringComparison.Ordinal);
        Assert.Contains("codesign -d --entitlements", afterBuild, StringComparison.Ordinal);
        Assert.Contains("aps-environment", afterBuild, StringComparison.Ordinal);
    }

    [Fact]
    public void Gitignore_ExcludesAppleSigningAssetsAndReleaseOutput()
    {
        var lines = ReadSource(".gitignore").Split('\n').Select(line => line.Trim()).ToHashSet();

        Assert.Contains("*.mobileprovision", lines);
        Assert.Contains("*.p12", lines);
        Assert.Contains("*.cer", lines);
        Assert.Contains("*.p8", lines);
        Assert.Contains("artifacts/", lines);
    }

    [Fact]
    public void PrivacyManifest_DeclaresEveryRequiredReasonApi()
    {
        var root = ReadPlistRoot(PrivacyManifest);
        var types = ValueFor(root, "NSPrivacyAccessedAPITypes");

        var declared = types.Elements("dict").ToDictionary(
            entry => ValueFor(entry, "NSPrivacyAccessedAPIType").Value,
            entry => ValueFor(entry, "NSPrivacyAccessedAPITypeReasons").Elements("string").Select(reason => reason.Value).ToArray());

        // UserDefaults: MAUI Preferences is backed by NSUserDefaults (session, profile cache).
        // A missing category is rejected at upload with ITMS-91053.
        Assert.Equal(
            new Dictionary<string, string[]>
            {
                ["NSPrivacyAccessedAPICategoryFileTimestamp"] = ["C617.1"],
                ["NSPrivacyAccessedAPICategorySystemBootTime"] = ["35F9.1"],
                ["NSPrivacyAccessedAPICategoryDiskSpace"] = ["E174.1"],
                ["NSPrivacyAccessedAPICategoryUserDefaults"] = ["CA92.1"],
            },
            declared);
        Assert.Empty(types.Nodes().OfType<XComment>());
        Assert.Equal("false", ValueFor(root, "NSPrivacyTracking").Name.LocalName);
    }

    [Fact]
    public void InfoPlist_DeclaresExemptEncryptionAndAllowsNoPlainHttp()
    {
        var source = ReadSource(InfoPlist);
        var root = ReadPlistRoot(InfoPlist);

        // HTTPS and system cryptography only, so uploads skip the export-compliance question.
        Assert.Equal("false", ValueFor(root, "ITSAppUsesNonExemptEncryption").Name.LocalName);
        Assert.DoesNotContain(root.Elements("key"), key => key.Value == "NSAppTransportSecurity");
        Assert.DoesNotContain("NSAllowsArbitraryLoads", source, StringComparison.Ordinal);
        Assert.DoesNotContain("NSExceptionAllowsInsecureHTTPLoads", source, StringComparison.Ordinal);
    }

    [Fact]
    public void InfoPlist_TargetsIPhoneOnly()
    {
        var root = ReadPlistRoot(InfoPlist);

        // iPhone only until the layouts are designed for iPad (021 research D11). The App
        // Store doesn't let an update drop iPad once a build has shipped with it.
        Assert.Equal(["1"], ValueFor(root, "UIDeviceFamily").Elements("integer").Select(family => family.Value));
    }

    [Fact]
    public void ReleaseEntitlements_UseProductionPushAndSignInWithApple()
    {
        var root = ReadPlistRoot(ReleaseEntitlements);

        // TestFlight and App Store builds register with the production APNs environment.
        Assert.Equal("production", ValueFor(root, "aps-environment").Value);
        Assert.Equal(["Default"], ValueFor(root, "com.apple.developer.applesignin").Elements("string").Select(scope => scope.Value));

        var project = XDocument.Parse(ReadSource(Project));
        var release = Assert.Single(project.Descendants("CodesignEntitlements"),
            element => (string?)element.Attribute("Condition") == "'$(Configuration)' != 'Debug'");
        Assert.Equal("Platforms/iOS/Entitlements.Release.plist", release.Value);
        var group = (string?)release.Parent!.Attribute("Condition") ?? "";
        Assert.Contains("-ios", group, StringComparison.Ordinal);
        Assert.Contains("simulator", group, StringComparison.Ordinal);
    }

    /// <summary>
    /// Splits the script at its first <c>dotnet</c> command, so guards can be shown to run
    /// before anything builds (contract: a failed check leaves nothing in artifacts/).
    /// </summary>
    private static (string BeforeBuild, string AfterBuild) SplitAtFirstDotnetCall(string script)
    {
        var lines = script.Split('\n');
        var first = Array.FindIndex(lines, line => line.TrimStart().StartsWith("dotnet ", StringComparison.Ordinal));
        Assert.True(first >= 0, "The release script runs no dotnet command.");
        return (string.Join('\n', lines[..first]), string.Join('\n', lines[first..]));
    }

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
