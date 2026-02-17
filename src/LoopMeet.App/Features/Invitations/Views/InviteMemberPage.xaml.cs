using System.Collections.Generic;
using LoopMeet.App.Features.Invitations.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LoopMeet.App.Features.Invitations.Views;

public partial class InviteMemberPage : ContentPage, IQueryAttributable
{
    public InviteMemberPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<InviteMemberViewModel>();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (BindingContext is not InviteMemberViewModel viewModel)
        {
            return;
        }

        if (query.TryGetValue("groupId", out var groupIdValue)
            && Guid.TryParse(groupIdValue?.ToString(), out var groupId))
        {
            viewModel.ApplyGroupId(groupId);
        }
    }
}
