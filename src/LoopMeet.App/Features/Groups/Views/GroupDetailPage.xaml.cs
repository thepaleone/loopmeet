using LoopMeet.App.Features.Groups.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LoopMeet.App.Features.Groups.Views;

public partial class GroupDetailPage : ContentPage, IQueryAttributable
{
    public GroupDetailPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<GroupDetailViewModel>();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (BindingContext is not GroupDetailViewModel viewModel)
        {
            return;
        }

        if (query.TryGetValue("groupId", out var groupIdValue)
            && Guid.TryParse(groupIdValue?.ToString(), out var groupId))
        {
            viewModel.GroupId = groupId;
            viewModel.ResetListMode();
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is GroupDetailViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }
    }

    /// <summary>Loads the next page of past meetups as the user nears the bottom.</summary>
    private void OnContentScrolled(object? sender, ScrolledEventArgs e)
    {
        if (BindingContext is not GroupDetailViewModel { IsPastSelected: true, HasLoadMoreError: false } viewModel)
        {
            return;
        }

        var distanceFromBottom = ContentScroll.ContentSize.Height - ContentScroll.Height - e.ScrollY;
        if (distanceFromBottom <= 300 && viewModel.LoadMorePastMeetupsCommand.CanExecute(null))
        {
            viewModel.LoadMorePastMeetupsCommand.Execute(null);
        }
    }
}
