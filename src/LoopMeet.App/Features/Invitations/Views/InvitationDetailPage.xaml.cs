using LoopMeet.App.Features.Invitations.Models;
using LoopMeet.App.Features.Invitations.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LoopMeet.App.Features.Invitations.Views;

public partial class InvitationDetailPage : ContentPage, IQueryAttributable
{
    public InvitationDetailPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<InvitationDetailViewModel>();
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (BindingContext is not InvitationDetailViewModel viewModel)
        {
            return;
        }

        if (query.TryGetValue("invitation", out var invitationValue)
            && invitationValue is InvitationSummary invitation)
        {
            viewModel.ApplyInvitation(invitation);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        Shell.SetBackButtonBehavior(this, new BackButtonBehavior
        {
            IsEnabled = false,
            IsVisible = false
        });

        // Overlay.Opacity = 0;
        Card.Opacity = 0;
        Card.Scale = 0.96;

        await Task.WhenAll(
            // Overlay.FadeTo(1, 160, Easing.CubicOut),
            Card.FadeToAsync(1, 200, Easing.CubicOut),
            Card.ScaleToAsync(1, 200, Easing.CubicOut)
        );
    }
}
