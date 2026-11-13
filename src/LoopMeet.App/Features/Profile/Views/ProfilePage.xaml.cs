using LoopMeet.App.Features.Profile.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LoopMeet.App.Features.Profile.Views;

public partial class ProfilePage : ContentPage
{
    private bool? _twoColumns;
    private Window? _subscribedWindow;

    public ProfilePage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<ProfileViewModel>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ProfileViewModel viewModel)
        {
            // Directly, not inside LoadAsync: LoadCommand ignores calls while a load is running.
            viewModel.RefreshNotificationStatus();
            viewModel.LoadCommand.Execute(null);
        }

        // Returning from device Settings resumes the app without raising OnAppearing.
        if (Window is not null && _subscribedWindow is null)
        {
            _subscribedWindow = Window;
            _subscribedWindow.Resumed += OnWindowResumed;
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_subscribedWindow is not null)
        {
            _subscribedWindow.Resumed -= OnWindowResumed;
            _subscribedWindow = null;
        }
    }

    private void OnWindowResumed(object? sender, EventArgs e)
    {
        (BindingContext as ProfileViewModel)?.RefreshNotificationStatus();
    }

    /// <summary>
    /// Moves the header card beside the details from 800 points wide (feature 022). The same
    /// views change cells, so an unsaved display name survives a rotation.
    /// </summary>
    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width <= 0)
        {
            return;
        }

        var twoColumns = ProfileLayout.UsesTwoColumns(width);
        if (twoColumns == _twoColumns)
        {
            return;
        }

        _twoColumns = twoColumns;
        if (twoColumns)
        {
            ContentGrid.ColumnDefinitions = [new ColumnDefinition(new GridLength(2, GridUnitType.Star)), new ColumnDefinition(new GridLength(3, GridUnitType.Star))];
            ContentGrid.RowDefinitions = [new RowDefinition(GridLength.Auto)];
            Grid.SetRow(DetailsColumn, 0);
            Grid.SetColumn(DetailsColumn, 1);
        }
        else
        {
            ContentGrid.ColumnDefinitions = [new ColumnDefinition(GridLength.Star)];
            ContentGrid.RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto)];
            Grid.SetRow(DetailsColumn, 1);
            Grid.SetColumn(DetailsColumn, 0);
        }

        var maxWidth = ProfileLayout.MaxContentWidth(twoColumns);
        ContentGrid.MaximumWidthRequest = maxWidth;
        HeaderRow.MaximumWidthRequest = maxWidth;
    }
}
