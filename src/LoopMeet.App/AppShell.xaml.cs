using LoopMeet.App.Features.Auth;
using LoopMeet.App.Features.Auth.Views;
using LoopMeet.App.Features.Groups.Views;
using LoopMeet.App.Features.Invitations.Views;
using Microsoft.Maui.ApplicationModel;

namespace LoopMeet.App;

public partial class AppShell : Shell
{
	private bool _authInitialized;

	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute("create-account", typeof(CreateAccountPage));
		Routing.RegisterRoute("group-detail", typeof(GroupDetailPage));
		Routing.RegisterRoute("create-group", typeof(CreateGroupPage));
		Routing.RegisterRoute("edit-group", typeof(EditGroupPage));
		Routing.RegisterRoute("invite-member", typeof(InviteMemberPage));
		Routing.RegisterRoute("invitation-detail", typeof(InvitationDetailPage));
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (_authInitialized)
		{
			return;
		}

		_authInitialized = true;
		var services = Application.Current?.Handler?.MauiContext?.Services;
		var authService = services?.GetService<AuthService>();
		if (authService is null)
		{
			return;
		}

		try
		{
			var session = await authService.RestoreSessionAsync();
			if (session is not null && !string.IsNullOrWhiteSpace(session.AccessToken))
			{
				await MainThread.InvokeOnMainThreadAsync(() => GoToAsync("//groups"));
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error restoring session: {ex}");
		}
	}
}
