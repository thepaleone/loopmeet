namespace LoopMeet.Api.Tests.Integration;

/// <summary>
/// Source-inspection guards on the edge functions for feature 012. The decision
/// logic itself is unit-tested in Deno (webhook-router.test.ts); these pin the
/// wiring around it that Deno tests cannot see.
/// </summary>
public sealed class PastMeetupNotificationGuardTests
{
    [Fact]
    public void Dispatch_ChecksSuppressionBeforeResolvingTheType_AndLogsIt()
    {
        var source = ReadFunction("notifications-dispatch/index.ts");

        var suppressionIndex = source.IndexOf("resolveSuppression(", StringComparison.Ordinal);
        var resolveIndex = source.IndexOf("resolveNotificationType(", StringComparison.Ordinal);
        Assert.True(suppressionIndex >= 0, "index.ts must call resolveSuppression.");
        Assert.True(suppressionIndex < resolveIndex, "Suppression must run before the notification type is resolved.");
        // FR-028: a missing push can be explained from the logs.
        Assert.Contains("\"notification_suppressed\"", source, StringComparison.Ordinal);
        Assert.Contains("reason: suppression", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Router_UsesTheSharedLifecycleRule()
    {
        var source = ReadFunction("notifications-dispatch/webhook-router.ts");

        Assert.Contains("from \"../_shared/meetup-lifecycle.ts\"", source, StringComparison.Ordinal);
        Assert.Contains("isMeetupPast(", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Reminders_OnlyFireOnTheMeetupsLocalDay()
    {
        var source = ReadFunction("reminders-scheduler/index.ts");

        // FR-027: a finished day never gets a same-day reminder.
        Assert.Contains("localScheduled.isoDate !== localNow.isoDate", source, StringComparison.Ordinal);
    }

    private static string ReadFunction(string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../../supabase/functions", relativePath));
        return File.ReadAllText(path);
    }
}
