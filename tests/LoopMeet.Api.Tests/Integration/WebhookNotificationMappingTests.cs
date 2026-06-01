namespace LoopMeet.Api.Tests.Integration;

public sealed class WebhookNotificationMappingTests
{
    [Fact]
    public void WebhookRouter_MapsInvitationAndMeetupEvents()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../../supabase/functions/notifications-dispatch/webhook-router.ts"));
        var source = File.ReadAllText(path);

        Assert.Contains("invitation.new", source, StringComparison.Ordinal);
        Assert.Contains("meetup.created", source, StringComparison.Ordinal);
        Assert.Contains("meetup.updated", source, StringComparison.Ordinal);
        Assert.Contains("meetup.canceled", source, StringComparison.Ordinal);
    }
}
