using CommunityToolkit.Mvvm.ComponentModel;
using LoopMeet.App.Services;

namespace LoopMeet.App.Features.DevTools.ViewModels;

public sealed partial class DevInfoViewModel : ObservableObject
{
	[ObservableProperty]
	private string _apiBaseUrl = string.Empty;

	[ObservableProperty]
	private string _supabaseUrl = string.Empty;

	[ObservableProperty]
	private string _supabaseAnonKey = string.Empty;

	[ObservableProperty]
	private string _buildConfiguration = string.Empty;

	public DevInfoViewModel(AppConfig config)
	{
		ApiBaseUrl = config.ApiBaseUrl;
		SupabaseUrl = config.SupabaseUrl;
		SupabaseAnonKey = config.SupabaseAnonOrPublisableKey;

#if DEBUG
		BuildConfiguration = "Debug";
#elif STAGING
		BuildConfiguration = "Staging";
#else
		BuildConfiguration = "Release";
#endif
	}
}
