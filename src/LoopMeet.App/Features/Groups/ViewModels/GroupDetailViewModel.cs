using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Auth;
using LoopMeet.App.Features.Groups.Models;
using LoopMeet.App.Features.Meetups.Models;
using LoopMeet.App.Services;
using Microsoft.Maui.ApplicationModel;

namespace LoopMeet.App.Features.Groups.ViewModels;

public sealed partial class GroupDetailViewModel : ObservableObject
{
    private readonly GroupsApi _groupsApi;
    private readonly MeetupsApi _meetupsApi;
    private readonly AuthService _authService;

    public ObservableCollection<GroupMember> Members { get; } = new();
    public ObservableCollection<MeetupSummary> Meetups { get; } = new();
    public ObservableCollection<MeetupSummary> PastMeetups { get; } = new();

    // Each list reloads on its own: only the selected one is refreshed when the
    // page reappears, and the other is marked stale so selecting it reloads it.
    private bool _upcomingLoaded;
    private bool _pastLoaded;
    private string? _pastNextCursor;

    [ObservableProperty]
    private string _groupName = string.Empty;

    [ObservableProperty]
    private Guid _groupId;

    [ObservableProperty]
    private Guid _ownerUserId;

    [ObservableProperty]
    private bool _isOwner;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowUpcomingEmpty))]
    private bool _hasMeetups;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUpcomingSelected), nameof(IsPastSelected), nameof(SelectedListLabel), nameof(ShowUpcomingEmpty), nameof(ShowPastEmpty), nameof(ShowLoadMoreError))]
    private MeetupListMode _listMode = MeetupListMode.Upcoming;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPastEmpty))]
    private bool _hasPastMeetups;

    [ObservableProperty]
    private bool _isLoadingMorePast;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPastEmpty), nameof(ShowLoadMoreError))]
    private bool _hasLoadMoreError;

    [ObservableProperty]
    private bool _isRefreshing;

    public bool IsUpcomingSelected => ListMode == MeetupListMode.Upcoming;
    public bool IsPastSelected => ListMode == MeetupListMode.Past;
    public string SelectedListLabel => IsPastSelected ? "Past Meetups" : "Upcoming Meetups";
    public bool ShowUpcomingEmpty => IsUpcomingSelected && _upcomingLoaded && !HasMeetups;
    public bool ShowPastEmpty => IsPastSelected && _pastLoaded && !HasPastMeetups && !HasLoadMoreError;
    public bool ShowLoadMoreError => IsPastSelected && HasLoadMoreError;

    public GroupDetailViewModel(GroupsApi groupsApi, MeetupsApi meetupsApi, AuthService authService)
    {
        _groupsApi = groupsApi;
        _meetupsApi = meetupsApi;
        _authService = authService;
    }

    [RelayCommand]
    private Task InviteMemberAsync()
    {
        if (GroupId == Guid.Empty || !IsOwner)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync("invite-member", new Dictionary<string, object>
        {
            ["groupId"] = GroupId
        });
    }

    [RelayCommand]
    private Task EditGroupAsync()
    {
        if (GroupId == Guid.Empty || !IsOwner)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync("edit-group", new Dictionary<string, object>
        {
            ["groupId"] = GroupId,
            ["groupName"] = GroupName
        });
    }

    [RelayCommand]
    private Task AddMeetupAsync()
    {
        if (GroupId == Guid.Empty || !IsOwner)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync("create-meetup", new Dictionary<string, object>
        {
            ["groupId"] = GroupId
        });
    }

    /// <summary>
    /// Opens the read-only details screen. Deliberately not owner-gated: every
    /// group member gets a response from tapping a meetup (FR-008), and editing
    /// is offered on the details screen only to the group's owner.
    /// </summary>
    [RelayCommand]
    private Task OpenMeetupDetailAsync(MeetupSummary? meetup)
    {
        if (meetup is null || GroupId == Guid.Empty)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync("meetup-detail", new Dictionary<string, object>
        {
            ["groupId"] = GroupId,
            ["meetupId"] = meetup.Id
        });
    }

    [RelayCommand]
    private async Task OpenLocationAsync(MeetupSummary? meetup)
    {
        if (meetup is not { CanOpenLocation: true })
        {
            return;
        }

        try
        {
            await Map.Default.OpenAsync(meetup.Latitude!.Value, meetup.Longitude!.Value,
                new MapLaunchOptions { Name = meetup.PlaceName ?? string.Empty });
        }
        catch
        {
            // Maps app not available
        }
    }

    [RelayCommand]
    private async Task DeleteMeetupAsync(MeetupSummary? meetup)
    {
        if (meetup is null || GroupId == Guid.Empty || !IsOwner)
        {
            return;
        }

        var confirmed = await Shell.Current.DisplayAlert(
            "Delete Meetup",
            $"Are you sure you want to delete \"{meetup.Title}\"?",
            "Delete",
            "Cancel");

        if (!confirmed) return;

        try
        {
            await _meetupsApi.DeleteMeetupAsync(GroupId, meetup.Id);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Meetups.Remove(meetup);
                PastMeetups.Remove(meetup);
                HasMeetups = Meetups.Count > 0;
                HasPastMeetups = PastMeetups.Count > 0;
            });
        }
        catch
        {
            await Shell.Current.DisplayAlert("Error", "Could not delete the meetup. Please try again.", "OK");
        }
    }

    [RelayCommand]
    private Task EditMeetupAsync(MeetupSummary? meetup)
    {
        if (meetup is null || GroupId == Guid.Empty || !IsOwner)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync("edit-meetup", new Dictionary<string, object>
        {
            ["groupId"] = GroupId,
            ["meetupId"] = meetup.Id
        });
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (GroupId == Guid.Empty)
        {
            return;
        }

        var group = await _groupsApi.GetGroupAsync(GroupId);
        GroupName = group.Name;
        OwnerUserId = group.OwnerUserId;
        var currentUserId = _authService.GetCurrentUserId();
        IsOwner = currentUserId.HasValue && currentUserId.Value == OwnerUserId;
        Members.Clear();
        foreach (var member in group.Members)
        {
            Members.Add(member);
        }

        await LoadSelectedListAsync();
    }

    /// <summary>
    /// Called when the page is navigated to (not when it reappears), so every
    /// fresh open starts on Upcoming (FR-002).
    /// </summary>
    public void ResetListMode()
    {
        ListMode = MeetupListMode.Upcoming;
        PastMeetups.Clear();
        HasPastMeetups = false;
        HasLoadMoreError = false;
        _pastNextCursor = null;
        _pastLoaded = false;
        _upcomingLoaded = false;
    }

    /// <summary>Switches lists without reloading the rest of the page (FR-005).</summary>
    [RelayCommand]
    private async Task SelectMeetupListAsync(MeetupListMode mode)
    {
        if (ListMode == mode)
        {
            return;
        }

        ListMode = mode;
        if (mode == MeetupListMode.Past ? !_pastLoaded : !_upcomingLoaded)
        {
            await LoadSelectedListAsync();
        }
    }

    /// <summary>Pull-to-refresh: reloads only the list being shown (FR-006).</summary>
    [RelayCommand]
    private async Task RefreshMeetupsAsync()
    {
        try
        {
            await LoadSelectedListAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// Appends the next page of past meetups. Also the retry action after a failed
    /// page, including a failed first page.
    /// </summary>
    [RelayCommand]
    private async Task LoadMorePastMeetupsAsync()
    {
        if (!IsPastSelected || GroupId == Guid.Empty)
        {
            return;
        }

        if (!_pastLoaded)
        {
            await LoadPastFirstPageAsync();
            return;
        }

        if (_pastNextCursor is null || IsLoadingMorePast)
        {
            return;
        }

        IsLoadingMorePast = true;
        HasLoadMoreError = false;
        try
        {
            var page = await _meetupsApi.GetPastMeetupsAsync(GroupId, _pastNextCursor);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var shown = PastMeetups.Select(item => item.Id).ToHashSet();
                foreach (var meetup in page.Meetups.Where(item => shown.Add(item.Id)))
                {
                    PastMeetups.Add(meetup);
                }

                _pastNextCursor = page.NextCursor;
                HasPastMeetups = PastMeetups.Count > 0;
            });
        }
        catch
        {
            HasLoadMoreError = true;
        }
        finally
        {
            IsLoadingMorePast = false;
        }
    }

    private Task LoadSelectedListAsync()
    {
        if (IsPastSelected)
        {
            _upcomingLoaded = false;
            return LoadPastFirstPageAsync();
        }

        _pastLoaded = false;
        return LoadUpcomingAsync();
    }

    private async Task LoadUpcomingAsync()
    {
        try
        {
            var meetupsResponse = await _meetupsApi.GetGroupMeetupsAsync(GroupId);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Meetups.Clear();
                foreach (var meetup in meetupsResponse.Meetups)
                {
                    Meetups.Add(meetup);
                }

                _upcomingLoaded = true;
                HasMeetups = Meetups.Count > 0;
                OnPropertyChanged(nameof(ShowUpcomingEmpty));
            });
        }
        catch
        {
            _upcomingLoaded = true;
            HasMeetups = false;
            OnPropertyChanged(nameof(ShowUpcomingEmpty));
        }
    }

    private async Task LoadPastFirstPageAsync()
    {
        HasLoadMoreError = false;
        try
        {
            var page = await _meetupsApi.GetPastMeetupsAsync(GroupId);
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                PastMeetups.Clear();
                foreach (var meetup in page.Meetups)
                {
                    PastMeetups.Add(meetup);
                }

                _pastNextCursor = page.NextCursor;
                _pastLoaded = true;
                HasPastMeetups = PastMeetups.Count > 0;
                OnPropertyChanged(nameof(ShowPastEmpty));
            });
        }
        catch
        {
            // Keep whatever is shown; the retry row offers the next attempt.
            HasLoadMoreError = true;
        }
    }
}
