using LoopMeet.App.Features.DevTools.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LoopMeet.App.Features.DevTools.Views;

public partial class DevInfoPage : ContentPage
{
	public DevInfoPage()
	{
		InitializeComponent();

		var services = Application.Current?.Handler?.MauiContext?.Services;
		BindingContext = services?.GetService<DevInfoViewModel>();
	}
}
