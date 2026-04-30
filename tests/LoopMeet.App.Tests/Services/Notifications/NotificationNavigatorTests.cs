namespace LoopMeet.App.Tests.Services.Notifications;

public sealed class NotificationNavigatorTests
{
    [Fact]
    public void NotificationNavigator_ContainsAllRequiredDestinationRoutes()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../../src/LoopMeet.App/Services/Notifications/NotificationNavigator.cs"));
        var source = File.ReadAllText(path);

        Assert.Contains("//Invitations/Pending", source, StringComparison.Ordinal);
        Assert.Contains("//Groups/Detail?groupId=", source, StringComparison.Ordinal);
        Assert.Contains("//Home", source, StringComparison.Ordinal);
    }
}
