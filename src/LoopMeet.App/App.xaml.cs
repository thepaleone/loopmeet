using Microsoft.Extensions.DependencyInjection;
using LoopMeet.App.Services.Notifications;

namespace LoopMeet.App;

public partial class App : Application
{
	public App(
		NotificationLifecycleRegistrar notificationLifecycleRegistrar,
		OneSignalBootstrapService oneSignalBootstrapService)
	{
		InitializeComponent();
		_ = oneSignalBootstrapService.InitializeAsync();
		_ = notificationLifecycleRegistrar.RegisterAsync();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}
