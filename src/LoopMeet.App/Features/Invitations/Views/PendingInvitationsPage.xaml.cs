using LoopMeet.App.Features.Invitations.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LoopMeet.App.Features.Invitations.Views;

public partial class PendingInvitationsPage : ContentPage
{
    private CancellationTokenSource? _shimmerCts;

    public PendingInvitationsPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<PendingInvitationsViewModel>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is PendingInvitationsViewModel viewModel)
        {
            viewModel.LoadCommand.Execute(null);
        }

        StartShimmer();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopShimmer();
    }

    private void StartShimmer()
    {
        StopShimmer();
        _shimmerCts = new CancellationTokenSource();
        var token = _shimmerCts.Token;

        _ = AnimateShimmer(InvitationsShimmer1, token);
        _ = AnimateShimmer(InvitationsShimmer2, token, 120);
    }

    private void StopShimmer()
    {
        if (_shimmerCts is null)
        {
            return;
        }

        _shimmerCts.Cancel();
        _shimmerCts = null;
    }

    private static async Task AnimateShimmer(VisualElement? element, CancellationToken token, int initialDelay = 0)
    {
        if (element is null)
        {
            return;
        }

        if (initialDelay > 0)
        {
            await Task.Delay(initialDelay, token);
        }

        while (!token.IsCancellationRequested)
        {
            element.TranslationX = -120;
            try
            {
                await element.TranslateToAsync(240, 0, 900, Easing.Linear);
                await Task.Delay(150, token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
