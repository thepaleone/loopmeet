using LoopMeet.App.Features.Auth.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LoopMeet.App.Features.Auth.Views;

public partial class StartupGatePage : ContentPage
{
    public StartupGatePage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<StartupGateViewModel>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = (BindingContext as StartupGateViewModel)?.ResolveAsync();
    }
}
