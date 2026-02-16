using LoopMeet.App.Features.Groups.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LoopMeet.App.Features.Groups.Views;

public partial class GroupsListPage : ContentPage
{
    public GroupsListPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<GroupsListViewModel>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is GroupsListViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }
}
