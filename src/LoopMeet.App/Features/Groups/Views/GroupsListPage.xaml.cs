using LoopMeet.App.Features.Groups.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace LoopMeet.App.Features.Groups.Views;

public partial class GroupsListPage : ContentPage
{
    private CancellationTokenSource? _shimmerCts;

    public GroupsListPage()
    {
        InitializeComponent();

        var services = Application.Current?.Handler?.MauiContext?.Services;
        BindingContext = services?.GetService<GroupsListViewModel>();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        Shell.SetBackButtonBehavior(this, new BackButtonBehavior
        {
            IsEnabled = false,
            IsVisible = false
        });

        if (BindingContext is GroupsListViewModel viewModel)
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
        _ = AnimateShimmer(OwnedShimmer1, token, 40);
        _ = AnimateShimmer(OwnedShimmer2, token, 160);
        _ = AnimateShimmer(OwnedShimmer3, token, 80);
        _ = AnimateShimmer(MemberShimmer1, token, 60);
        _ = AnimateShimmer(MemberShimmer2, token, 140);
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
            await element.TranslateTo(240, 0, 900, Easing.Linear);
            await Task.Delay(150, token);
        }
    }
}
