namespace LoopMeet.Api.Tests.Contract;

// FR-019: the Api never holds a privileged Supabase client. Service-role access is confined to the
// account-purge and apple-authorization edge functions.
public sealed class PrivilegedAccessTests
{
    [Fact]
    public void OnlyConfigurationReferencesTheServiceRoleKey()
    {
        var apiRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/LoopMeet.Api"));
        var configurationDir = Path.Combine(apiRoot, "Services", "Configuration") + Path.DirectorySeparatorChar;
        var buildDirs = new[] { "bin", "obj" }.Select(dir => Path.Combine(apiRoot, dir) + Path.DirectorySeparatorChar).ToArray();

        var sources = Directory.EnumerateFiles(apiRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => !buildDirs.Any(dir => path.StartsWith(dir, StringComparison.Ordinal)))
            .ToList();
        Assert.NotEmpty(sources);

        var offenders = sources
            .Where(path => !path.StartsWith(configurationDir, StringComparison.Ordinal))
            .Where(path => File.ReadAllText(path).Contains("ServiceOrSecretKey", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(apiRoot, path))
            .ToList();

        Assert.Empty(offenders);
    }
}
