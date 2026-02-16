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
}
