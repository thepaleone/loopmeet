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

		var config = new AppConfig
		{
			ApiBaseUrl = Environment.GetEnvironmentVariable("LOOPMEET_API_BASE_URL") ?? "https://localhost:5001",
			SupabaseUrl = Environment.GetEnvironmentVariable("LOOPMEET_SUPABASE_URL") ?? string.Empty,
			SupabaseAnonKey = Environment.GetEnvironmentVariable("LOOPMEET_SUPABASE_ANON_KEY") ?? string.Empty
		};

		builder.Services.AddSingleton(config);
		builder.Services.AddSingleton(_ => new Client(config.SupabaseUrl, config.SupabaseAnonKey, new SupabaseOptions
		{
			AutoConnectRealtime = false
		}));
		builder.Services.AddSingleton<AuthService>();
		builder.Services.AddSingleton<AuthCoordinator>();
		builder.Services.AddLoopMeetApi<IGroupsApi>(config);
		builder.Services.AddLoopMeetApi<IInvitationsApi>(config);
		builder.Services.AddSingleton<GroupsApi>();
		builder.Services.AddSingleton<InvitationsApi>();
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<CreateAccountViewModel>();
		builder.Services.AddTransient<GroupsListViewModel>();
		builder.Services.AddTransient<GroupDetailViewModel>();
		builder.Services.AddTransient<CreateGroupViewModel>();
		builder.Services.AddTransient<EditGroupViewModel>();
		builder.Services.AddTransient<InviteMemberViewModel>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<CreateAccountPage>();
		builder.Services.AddTransient<GroupsListPage>();
		builder.Services.AddTransient<GroupDetailPage>();
		builder.Services.AddTransient<CreateGroupPage>();
		builder.Services.AddTransient<EditGroupPage>();
		builder.Services.AddTransient<InviteMemberPage>();

		return builder.Build();
	}
}
