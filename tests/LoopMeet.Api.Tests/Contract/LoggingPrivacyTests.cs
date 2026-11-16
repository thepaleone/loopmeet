using System.Text.RegularExpressions;

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
    // 023 FR-021: reports, leaving and blocking are logged by id. A report's text and the reporter's
    // email reach only the safety inbox, never a log.
    [Theory]
    [InlineData("src/LoopMeet.Api/Services/Safety", "*.cs")]
    [InlineData("src/LoopMeet.Api/Endpoints/ReportsEndpoints.cs", null)]
    [InlineData("supabase/functions/report-email", "*.ts")]
    public void SafetyLogsNeverCarryEmailsOrReportText(string relativePath, string? pattern)
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var target = Path.Combine(root, relativePath);
        var files = pattern is null
            ? new[] { target }
            : Directory.EnumerateFiles(target, pattern).Where(path => !path.EndsWith(".test.ts", StringComparison.Ordinal)).ToArray();
        Assert.NotEmpty(files);

        var calls = files
            .SelectMany(path => LogCall.Matches(File.ReadAllText(path)).Select(match => (Path.GetFileName(path), match.Value)))
            .ToList();
        if (pattern is not null)
        {
            Assert.NotEmpty(calls);
        }

        foreach (var (file, call) in calls)
        {
            // Event names and message text are fixed literals; what's logged is the code outside them
            // plus the {Placeholder} names inside templates.
            var logged = StringLiteral.Replace(call, literal => string.Join(' ', Placeholder.Matches(literal.Value).Select(match => match.Groups[1].Value)));
            var identifiers = Identifier.Matches(logged).Select(match => match.Value.ToLowerInvariant());
            var leaked = identifiers.Where(ForbiddenIdentifiers.Contains).ToList();
            Assert.True(leaked.Count == 0, $"{file}: a log call logs {string.Join(", ", leaked)}: {call}");
        }
    }

    // A C# ILogger call or a ports.log(...) call in the edge function, up to the end of its statement.
    // Whole identifiers, so a count such as email_attempts is allowed but Email or replyTo isn't.
    private static readonly HashSet<string> ForbiddenIdentifiers = new(StringComparer.Ordinal)
    {
        "email", "reporteremail", "replyto", "reply_to", "details", "displayname", "display_name",
        "reporter_display_name", "reported_display_name", "groupname", "group_name", "text"
    };

    private static readonly Regex Identifier = new(@"[A-Za-z_][A-Za-z0-9_]*", RegexOptions.Compiled);

    private static readonly Regex StringLiteral = new(@"""(?:[^""\\]|\\.)*""", RegexOptions.Compiled);

    private static readonly Regex Placeholder = new(@"\{(\w+)", RegexOptions.Compiled);

    private static readonly Regex LogCall = new(@"(\bLog(Information|Warning|Error|Critical|Debug|Trace)|\bports\.log)\((?s:.*?)\);", RegexOptions.Compiled);
}
