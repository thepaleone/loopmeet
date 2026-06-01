namespace LoopMeet.App.Tests.Features.Settings;

public sealed class NotificationSettingsCtaTests
{
    [Fact]
    public void SettingsPage_ContainsEnableNotificationsCta()
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "../../../../../src/LoopMeet.App/Features/Profile/Views/SettingsPage.xaml"));
        var source = File.ReadAllText(path);

        Assert.Contains("Enable notifications", source, StringComparison.Ordinal);
        Assert.Contains("EnableNotificationsButton", source, StringComparison.Ordinal);
    }
}
