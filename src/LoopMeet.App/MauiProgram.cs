using LoopMeet.App.Features.Auth;
using LoopMeet.App.Features.Auth.ViewModels;
using LoopMeet.App.Features.Auth.Views;
using LoopMeet.App.Features.Groups.ViewModels;
using LoopMeet.App.Features.Groups.Views;
using LoopMeet.App.Features.Invitations.ViewModels;
using LoopMeet.App.Features.Invitations.Views;
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

#if DEBUG
		builder.Logging.AddDebug();
#endif

		var isDebug = false;
#if DEBUG
		isDebug = true;
#endif

		var apiBaseUrl = "https://api.loopmeet.example.com";
		var supabaseUrl = "https://cswfsnikasaorexwhsas.supabase.co";
		var supabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImNzd2ZzbmlrYXNhb3JleHdoc2FzIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NzE4OTA2NzMsImV4cCI6MjA4NzQ2NjY3M30.ENWIbaz-dQ-qaCag53EHlHQVdY9Tm7ZpfmVhqjTNIf8";
		if (isDebug)
		{
			supabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZS1kZW1vIiwicm9sZSI6ImFub24iLCJleHAiOjE5ODM4MTI5OTZ9.CRXP1A7WOeoJeXxjNni43kdQwgnWNReilDMblYTn_I0";
			if (DeviceInfo.Platform == DevicePlatform.Android)
			{
				apiBaseUrl = "http://10.0.2.2:5001";
				supabaseUrl = "http://10.0.2.2:54321";
			}
			else
			{
				apiBaseUrl = "http://localhost:5001";
				supabaseUrl = "http://localhost:54321";
			}
		}

		var config = new AppConfig
		{
			ApiBaseUrl = Environment.GetEnvironmentVariable("LOOPMEET_API_BASE_URL") ?? apiBaseUrl,
			SupabaseUrl = Environment.GetEnvironmentVariable("LOOPMEET_SUPABASE_URL") ?? supabaseUrl,
			SupabaseAnonKey = Environment.GetEnvironmentVariable("LOOPMEET_SUPABASE_ANON_KEY") ?? supabaseAnonKey
		};

		builder.Services.AddSingleton(config);
		builder.Services.AddSingleton(_ => new Client(config.SupabaseUrl, config.SupabaseAnonKey, new SupabaseOptions
		{
			AutoConnectRealtime = false,
			AutoRefreshToken = true,
			SessionHandler = new MauiSessionPersistence()
		}));
		builder.Services.AddSingleton<AuthService>();
		builder.Services.AddSingleton<AuthCoordinator>();
		builder.Services.AddTransient<ApiAuthHandler>();
		builder.Services.AddLoopMeetApi<IGroupsApi>(config);
		builder.Services.AddLoopMeetApi<IInvitationsApi>(config);
		builder.Services.AddLoopMeetApi<IUsersApi>(config);
		builder.Services.AddSingleton<GroupsApi>();
		builder.Services.AddSingleton<InvitationsApi>();
		builder.Services.AddSingleton<UsersApi>();
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<CreateAccountViewModel>();
		builder.Services.AddTransient<GroupsListViewModel>();
		builder.Services.AddTransient<GroupDetailViewModel>();
		builder.Services.AddTransient<CreateGroupViewModel>();
		builder.Services.AddTransient<EditGroupViewModel>();
		builder.Services.AddTransient<InviteMemberViewModel>();
		builder.Services.AddTransient<InvitationDetailViewModel>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<CreateAccountPage>();
		builder.Services.AddTransient<GroupsListPage>();
		builder.Services.AddTransient<GroupDetailPage>();
		builder.Services.AddTransient<CreateGroupPage>();
		builder.Services.AddTransient<EditGroupPage>();
		builder.Services.AddTransient<InviteMemberPage>();
		builder.Services.AddTransient<InvitationDetailPage>();

		return builder.Build();
	}
}
