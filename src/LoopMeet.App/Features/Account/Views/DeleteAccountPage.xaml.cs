using System.ComponentModel;
using LoopMeet.App.Features.Account.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LoopMeet.App.Features.Account.Views;

public partial class DeleteAccountPage : ContentPage
{
    public DeleteAccountPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<DeleteAccountViewModel>();
        if (BindingContext is DeleteAccountViewModel viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is DeleteAccountViewModel { IsLoaded: false } viewModel)
        {
            await viewModel.LoadCommand.ExecuteAsync(null);
        }
    }

    // Once the deletion is scheduled the only way out is OK, which signs out.
    protected override bool OnBackButtonPressed() =>
        BindingContext is DeleteAccountViewModel { IsScheduled: true } || base.OnBackButtonPressed();

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DeleteAccountViewModel.IsScheduled) && sender is DeleteAccountViewModel { IsScheduled: true })
        {
            Shell.SetBackButtonBehavior(this, new BackButtonBehavior
            {
                IsEnabled = false,
                IsVisible = false
            });
        }
    }
}
