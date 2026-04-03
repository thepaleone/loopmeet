using LoopMeet.App.Features.Auth;
using LoopMeet.App.Features.Auth.ViewModels;
using LoopMeet.App.Features.Auth.Views;
using LoopMeet.App.Features.Home.ViewModels;
using LoopMeet.App.Features.Home.Views;
using LoopMeet.App.Features.Groups.ViewModels;
using LoopMeet.App.Features.Groups.Views;
using LoopMeet.App.Features.Invitations.ViewModels;
using LoopMeet.App.Features.Invitations.Views;
using LoopMeet.App.Features.Meetups.ViewModels;
using LoopMeet.App.Features.Meetups.Views;
using LoopMeet.App.Features.Profile.ViewModels;
using LoopMeet.App.Features.Profile.Views;
using LoopMeet.App.Features.DevTools.ViewModels;
using LoopMeet.App.Features.DevTools.Views;
using LoopMeet.App.Services;
using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices;
using Supabase;

namespace LoopMeet.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG || STAGING
		builder.Logging.AddDebug();
#endif

#if DEBUG
		var apiBaseUrl = "http://dev.loopmeet.io:8080";
		var supabaseUrl = "http://dev.loopmeet.io:54321";
		var supabaseAnonOrPublishableKey = "sb_publishable_ACJWlzQHlZjBrEguHvfOxg_3BJgxAaH";
#elif STAGING
// #if DEBUG || STAGING
		var apiBaseUrl = "https://api-staging.loopmeet.io";
		var supabaseUrl ="https://cswfsnikasaorexwhsas.supabase.co";
		var supabaseAnonOrPublishableKey = "sb_publishable__0wAiCklh-5wV_AmK0GJdQ_VAC5dYE8";
#else
		throw new InvalidOperationException("Production not yet implemented.");
		var apiBaseUrl = string.Empty;
		var supabaseUrl = string.Empty;
		var supabaseAnonOrPublishableKey = string.Empty;
#endif

		var config = new AppConfig
		{
			ApiBaseUrl = Environment.GetEnvironmentVariable("LOOPMEET_API_BASE_URL") ?? apiBaseUrl,
			SupabaseUrl = Environment.GetEnvironmentVariable("LOOPMEET_SUPABASE_URL") ?? supabaseUrl,
			SupabaseAnonOrPublisableKey = Environment.GetEnvironmentVariable("LOOPMEET_SUPABASE_ANON_KEY") ?? supabaseAnonOrPublishableKey
		};

		builder.Services.AddSingleton(config);
		builder.Services.AddSingleton(_ => new Client(config.SupabaseUrl, config.SupabaseAnonOrPublisableKey, new SupabaseOptions
		{
			AutoConnectRealtime = false,
			AutoRefreshToken = true,
			SessionHandler = new MauiSessionPersistence()
		}));
		builder.Services.AddSingleton<UserProfileCache>();
		builder.Services.AddSingleton<AuthService>();
		builder.Services.AddSingleton<AuthCoordinator>();
		builder.Services.AddTransient<ApiAuthHandler>();
		builder.Services.AddLoopMeetApi<IGroupsApi>(config);
		builder.Services.AddLoopMeetApi<IInvitationsApi>(config);
		builder.Services.AddLoopMeetApi<IUsersApi>(config);
		builder.Services.AddLoopMeetApi<IMeetupsApi>(config);
		builder.Services.AddLoopMeetApi<IPlacesApi>(config);
		builder.Services.AddSingleton<GroupsApi>();
		builder.Services.AddSingleton<InvitationsApi>();
		builder.Services.AddSingleton<UsersApi>();
		builder.Services.AddSingleton<MeetupsApi>();
		builder.Services.AddSingleton<PlacesApi>();
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<CreateAccountViewModel>();
		builder.Services.AddTransient<HomeViewModel>();
		builder.Services.AddTransient<GroupsListViewModel>();
		builder.Services.AddTransient<GroupDetailViewModel>();
		builder.Services.AddTransient<CreateGroupViewModel>();
		builder.Services.AddTransient<EditGroupViewModel>();
		builder.Services.AddTransient<CreateMeetupViewModel>();
		builder.Services.AddTransient<EditMeetupViewModel>();
		builder.Services.AddTransient<InviteMemberViewModel>();
		builder.Services.AddTransient<InvitationDetailViewModel>();
		builder.Services.AddTransient<PendingInvitationsViewModel>();
		builder.Services.AddTransient<ProfileViewModel>();
		builder.Services.AddTransient<ChangePasswordViewModel>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<CreateAccountPage>();
		builder.Services.AddTransient<HomePage>();
		builder.Services.AddTransient<GroupsListPage>();
		builder.Services.AddTransient<GroupDetailPage>();
		builder.Services.AddTransient<CreateGroupPage>();
		builder.Services.AddTransient<EditGroupPage>();
		builder.Services.AddTransient<CreateMeetupPage>();
		builder.Services.AddTransient<EditMeetupPage>();
		builder.Services.AddTransient<InviteMemberPage>();
		builder.Services.AddTransient<InvitationDetailPage>();
		builder.Services.AddTransient<PendingInvitationsPage>();
		builder.Services.AddTransient<ProfilePage>();
		builder.Services.AddTransient<ChangePasswordPage>();
		builder.Services.AddTransient<DevInfoViewModel>();
		builder.Services.AddTransient<DevInfoPage>();

		return builder.Build();
	}
}
