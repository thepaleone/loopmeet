namespace LoopMeet.App.Tests.Services.Notifications;

public sealed class NotificationPermissionServiceTests
{
    [Fact]
    public void NotificationPermissionService_UsesUnknownAsPromptState()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../../src/LoopMeet.App/Services/Notifications/NotificationPermissionService.cs"));
        var source = File.ReadAllText(path);

        Assert.Contains("NotificationPermissionState.Unknown", source, StringComparison.Ordinal);
        Assert.Contains("ShouldPromptAfterSignIn", source, StringComparison.Ordinal);
    }
}
