using LoopMeet.App.Services.Notifications;

namespace LoopMeet.App.Features.Profile.Views;

public partial class SettingsPage : ContentPage
{
    private readonly NotificationSettingsLauncher _settingsLauncher;

    public SettingsPage(NotificationSettingsLauncher settingsLauncher)
    {
        InitializeComponent();
        _settingsLauncher = settingsLauncher;
    }

    private async void OnEnableNotificationsClicked(object? sender, EventArgs e)
    {
        await _settingsLauncher.OpenAppNotificationSettingsAsync();
    }
}
