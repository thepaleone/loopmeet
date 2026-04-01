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

    [ObservableProperty]
    private string _groupName = string.Empty;

    [ObservableProperty]
    private Guid _groupId;

    [ObservableProperty]
    private Guid _ownerUserId;

    [ObservableProperty]
    private bool _isOwner;

    [ObservableProperty]
    private bool _hasMeetups;

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

    [RelayCommand]
    private async Task OpenLocationAsync(MeetupSummary? meetup)
    {
        if (meetup is null || !meetup.HasLocation || meetup.Latitude is null || meetup.Longitude is null)
        {
            return;
        }

        try
        {
            await Map.Default.OpenAsync(meetup.Latitude.Value, meetup.Longitude.Value,
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
            Meetups.Remove(meetup);
            HasMeetups = Meetups.Count > 0;
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

        // Load meetups
        try
        {
            var meetupsResponse = await _meetupsApi.GetGroupMeetupsAsync(GroupId);
            Meetups.Clear();
            foreach (var meetup in meetupsResponse.Meetups)
            {
                Meetups.Add(meetup);
            }
            HasMeetups = Meetups.Count > 0;
        }
        catch
        {
            HasMeetups = false;
        }
    }
}
