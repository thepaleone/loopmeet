namespace LoopMeet.Api.Tests.Contract;

public sealed class NotificationPayloadContractTests
{
    [Fact]
    public void NotificationContract_DefinesAllRequiredCanonicalKeys()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../../supabase/functions/_shared/notification-contract.ts"));
        var source = File.ReadAllText(path);

        Assert.Contains("notification_type", source, StringComparison.Ordinal);
        Assert.Contains("target_kind", source, StringComparison.Ordinal);
        Assert.Contains("target_id", source, StringComparison.Ordinal);
        Assert.Contains("fallback_route", source, StringComparison.Ordinal);
        Assert.Contains("event_id", source, StringComparison.Ordinal);
        Assert.Contains("sent_at", source, StringComparison.Ordinal);
    }
}
