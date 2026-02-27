using LoopMeet.App.Features.Profile.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LoopMeet.App.Features.Profile.Views;

public partial class ChangePasswordPage : ContentPage
{
    public ChangePasswordPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<ChangePasswordViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ChangePasswordViewModel viewModel)
        {
            await viewModel.LoadContextCommand.ExecuteAsync(null);
        }
    }
}
