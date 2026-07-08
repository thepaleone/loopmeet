using Microsoft.Extensions.DependencyInjection;
using LoopMeet.App.Features.Auth.Session;
using LoopMeet.App.Services.Notifications;

namespace LoopMeet.App;

public partial class App : Application
{
	private readonly SessionCoordinator _sessionCoordinator;

	public App(
		NotificationLifecycleRegistrar notificationLifecycleRegistrar,
		OneSignalBootstrapService oneSignalBootstrapService,
		SessionCoordinator sessionCoordinator)
	{
		InitializeComponent();
		_sessionCoordinator = sessionCoordinator;
		_ = oneSignalBootstrapService.InitializeAsync();
		_ = notificationLifecycleRegistrar.RegisterAsync();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = new Window(new AppShell());
		// Fire-and-forget by design: resume must not block the UI; failures are
		// classified and handled inside the coordinator (FR-002 / FR-004a).
		window.Resumed += (_, _) => _ = _sessionCoordinator.EnsureFreshSessionAsync(RenewalTrigger.AppForegrounded);
		return window;
	}
}
