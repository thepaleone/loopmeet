namespace LoopMeet.App.Tests.Services.Notifications;

public sealed class NotificationTypeRegressionTests
{
    [Fact]
    public void NotificationRouteMap_HandlesBaselineNotificationTypes()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../../src/LoopMeet.App/Services/Notifications/NotificationRouteMap.cs"));
        var source = File.ReadAllText(path);

        Assert.Contains("invitation.new", source, StringComparison.Ordinal);
        Assert.Contains("meetup.created", source, StringComparison.Ordinal);
        Assert.Contains("meetup.updated", source, StringComparison.Ordinal);
        Assert.Contains("meetup.canceled", source, StringComparison.Ordinal);
        Assert.Contains("meetup.today_reminder", source, StringComparison.Ordinal);
    }
}
