using LoopMeet.App.Features.Account.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LoopMeet.App.Features.Account.Views;

public partial class PendingDeletionPage : ContentPage
{
    public PendingDeletionPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<PendingDeletionViewModel>();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is PendingDeletionViewModel viewModel)
        {
            await viewModel.LoadAsync();
        }
    }

    // The only ways out are Restore account and Sign out.
    protected override bool OnBackButtonPressed() => true;
}
