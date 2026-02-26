using LoopMeet.App.Features.Home.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LoopMeet.App.Features.Home.Views;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<HomeViewModel>();
    }
}
