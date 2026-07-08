using LoopMeet.App.Features.Auth.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LoopMeet.App.Features.Auth.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<LoginViewModel>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        (BindingContext as LoginViewModel)?.RefreshSessionNotice();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        (BindingContext as LoginViewModel)?.CancelPendingProviderSignIn();
    }
}
