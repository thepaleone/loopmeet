using LoopMeet.App.Features.Groups.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LoopMeet.App.Features.Groups.Views;

public partial class EditGroupPage : ContentPage, IQueryAttributable
{
    public EditGroupPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<EditGroupViewModel>();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (BindingContext is not EditGroupViewModel viewModel)
        {
            return;
        }

        if (query.TryGetValue("groupId", out var groupIdValue)
            && Guid.TryParse(groupIdValue?.ToString(), out var groupId))
        {
            query.TryGetValue("groupName", out var nameValue);
            viewModel.ApplyGroup(groupId, nameValue?.ToString());
        }
    }

}
