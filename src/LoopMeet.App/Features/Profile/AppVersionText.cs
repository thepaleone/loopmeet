namespace LoopMeet.App.Features.Profile;

/// <summary>The Profile version line. MAUI-free so it can be unit tested; the values come from AppInfo.</summary>
public static class AppVersionText
{
    public static string Format(string version, string build) => $"Version {version.Trim()} ({build.Trim()})";
}
