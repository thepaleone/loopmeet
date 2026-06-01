namespace LoopMeet.App.Tests.Services.Notifications;

public sealed class NotificationNavigatorTests
{
    [Fact]
    public void NotificationNavigator_ContainsAllRequiredDestinationRoutes()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../../src/LoopMeet.App/Services/Notifications/NotificationRouteMap.cs"));
        var source = File.ReadAllText(path);

        Assert.Contains("SignedInTabs.InvitationsShellPath", source, StringComparison.Ordinal);
        Assert.Contains("/group-detail?groupId=", source, StringComparison.Ordinal);
        Assert.Contains("SignedInTabs.HomeShellPath", source, StringComparison.Ordinal);
    }
}
