using System.ComponentModel;
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
        if (BindingContext is GroupDetailViewModel viewModel)
        {
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (BindingContext is not GroupDetailViewModel viewModel)
        {
            return;
        }

        // Shell re-delivers these parameters when a page pushed on top of this one
        // (meetup detail, edit) is popped. Each fresh open builds a new page and view
        // model, so only a group change resets the list; coming back keeps Past.
        if (query.TryGetValue("groupId", out var groupIdValue)
            && Guid.TryParse(groupIdValue?.ToString(), out var groupId)
            && groupId != viewModel.GroupId)
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

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GroupDetailViewModel.IsPastSelected))
        {
            MoveMeetupListSwitchThumb(animate: true);
        }
    }

    private void OnMeetupListSwitchSizeChanged(object? sender, EventArgs e) =>
        MoveMeetupListSwitchThumb(animate: false);

    /// <summary>Sizes the thumb to half the track and slides it under the selected label.</summary>
    private void MoveMeetupListSwitchThumb(bool animate)
    {
        var half = MeetupListSwitchTrack.Width / 2;
        if (half <= 0 || BindingContext is not GroupDetailViewModel viewModel)
        {
            return;
        }

        MeetupListSwitchThumb.WidthRequest = half;
        var target = viewModel.IsPastSelected ? half : 0;
        if (animate)
        {
            MeetupListSwitchThumb.CancelAnimations();
            _ = MeetupListSwitchThumb.TranslateToAsync(target, 0, 180, Easing.CubicInOut);
        }
        else
        {
            MeetupListSwitchThumb.TranslationX = target;
        }
    }
}
