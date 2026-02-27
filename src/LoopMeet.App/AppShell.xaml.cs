using LoopMeet.App.Features.Auth;
using LoopMeet.App.Features.Auth.Views;
using LoopMeet.App.Features.Home.Models;
using LoopMeet.App.Features.Groups.Views;
using LoopMeet.App.Features.Invitations.Views;
using LoopMeet.App.Features.Profile.Views;
using LoopMeet.App.Services;
using Microsoft.Maui.ApplicationModel;
using Refit;

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
		Routing.RegisterRoute("change-password", typeof(ChangePasswordPage));
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
				var usersApi = services?.GetService<UsersApi>();
				var userProfileCache = services?.GetService<UserProfileCache>();
				if (usersApi is not null && userProfileCache is not null)
				{
					try
					{
						var profile = await usersApi.GetProfileSummaryAsync();
						userProfileCache.SetCachedProfile(profile);
					}
					catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.NotFound)
					{
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine($"Error loading cached profile from API: {ex}");
					}
				}

				await MainThread.InvokeOnMainThreadAsync(() => GoToAsync(SignedInTabs.HomeShellPath));
			}
		}
		catch (Exception ex)
		{
			System.Diagnostics.Debug.WriteLine($"Error restoring session: {ex}");
		}
	}
}
