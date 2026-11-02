using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace LoopMeet.App.Tests.Platforms;

/// <summary>
/// Release-readiness checks for the Google Play build (feature 015): the
/// manifest's permission set, the version scheme shared with iOS, and the
/// signed-bundle script. None of these can run off a device, so they inspect
/// the source.
/// </summary>
public sealed class AndroidPlayReleaseTests
{
    private const string Manifest = "src/LoopMeet.App/Platforms/Android/AndroidManifest.xml";
    private const string Project = "src/LoopMeet.App/LoopMeet.App.csproj";
    private const string ReleaseScript = "deploy/build-android-release.sh";
    private static readonly XNamespace AndroidNs = "http://schemas.android.com/apk/res/android";

    [Fact]
    public void Manifest_DoesNotRequestRestrictedPermissions()
    {
        var permissions = ReadPermissions().Keys;

        // Play's photo and video permissions policy: an occasional avatar pick must
        // use the system photo picker, which needs no media permission.
        Assert.DoesNotContain("android.permission.READ_MEDIA_IMAGES", permissions);
        Assert.DoesNotContain("android.permission.READ_MEDIA_VIDEO", permissions);
        Assert.DoesNotContain("android.permission.MANAGE_EXTERNAL_STORAGE", permissions);
        Assert.DoesNotContain("android.permission.ACCESS_BACKGROUND_LOCATION", permissions);
    }

    [Fact]
    public void Manifest_KeepsBothForegroundLocationPermissions()
    {
        var permissions = ReadPermissions().Keys;

        // MAUI's Permissions.LocationWhenInUse throws PermissionException unless both
        // are declared, which would silently drop the meetup location bias.
        Assert.Contains("android.permission.ACCESS_COARSE_LOCATION", permissions);
        Assert.Contains("android.permission.ACCESS_FINE_LOCATION", permissions);
    }

    [Fact]
    public void Manifest_LimitsLegacyStorageReadToAndroid12AndBelow()
    {
        var permissions = ReadPermissions();

        Assert.True(permissions.TryGetValue("android.permission.READ_EXTERNAL_STORAGE", out var element));
        Assert.Equal("32", (string?)element.Attribute(AndroidNs + "maxSdkVersion"));
    }

    [Fact]
    public void Project_KeepsPackageNameAndSharedVersionScheme()
    {
        var project = XDocument.Parse(ReadSource(Project));

        // The Play package name is permanent once the app exists in the console.
        Assert.Equal("io.loopmeet.app", Single(project, "ApplicationId"));
        Assert.Matches(new Regex(@"^\d+\.\d+\.\d+$"), Single(project, "ApplicationDisplayVersion"));
        Assert.Matches(new Regex(@"^[1-9]\d*$"), Single(project, "ApplicationVersion"));
    }

    [Fact]
    public void ReleaseScript_BuildsSignedBundleWithSecretsFromEnvironment()
    {
        var script = ReadSource(ReleaseScript);

        Assert.Contains("-p:AndroidPackageFormat=aab", script, StringComparison.Ordinal);
        Assert.Contains("-p:ApplicationVersion=\"$BUILD_NUMBER\"", script, StringComparison.Ordinal);
        Assert.Contains("-p:AndroidSigningStorePass=env:LOOPMEET_ANDROID_KEYSTORE_PASS", script, StringComparison.Ordinal);
        Assert.Contains("-p:AndroidSigningKeyPass=env:LOOPMEET_ANDROID_KEY_PASS", script, StringComparison.Ordinal);
        // Same workaround as deploy-android.sh for the Release UnsatisfiedLinkError.
        Assert.Contains("-p:RunAOTCompilation=false", script, StringComparison.Ordinal);
        Assert.Contains("-p:PublishTrimmed=false", script, StringComparison.Ordinal);
    }

    [Fact]
    public void Gitignore_ExcludesUploadKeysAndReleaseOutput()
    {
        var lines = ReadSource(".gitignore").Split('\n').Select(line => line.Trim()).ToHashSet();

        Assert.Contains("*.keystore", lines);
        Assert.Contains("*.jks", lines);
        Assert.Contains("artifacts/", lines);
    }

    private static Dictionary<string, XElement> ReadPermissions()
        => XDocument.Parse(ReadSource(Manifest).TrimStart('﻿'))
            .Root!
            .Elements("uses-permission")
            .ToDictionary(element => (string)element.Attribute(AndroidNs + "name")!);

    private static string Single(XDocument project, string property)
        => Assert.Single(project.Descendants(property)).Value;

    private static string ReadSource(string repoRelativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "../../../../..", repoRelativePath));
        return File.ReadAllText(fullPath);
    }
}
