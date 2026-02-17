using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Groups.Models;
using LoopMeet.App.Features.Invitations.Models;
using LoopMeet.App.Services;

namespace LoopMeet.App.Features.Groups.ViewModels;

public sealed partial class GroupsListViewModel : ObservableObject
{
    private readonly GroupsApi _groupsApi;
    private readonly InvitationsApi _invitationsApi;

    public ObservableCollection<GroupSummary> OwnedGroups { get; } = new();
    public ObservableCollection<GroupSummary> MemberGroups { get; } = new();
    public ObservableCollection<InvitationSummary> PendingInvitations { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _showEmptyState;

    public GroupsListViewModel(GroupsApi groupsApi, InvitationsApi invitationsApi)
    {
        _groupsApi = groupsApi;
        _invitationsApi = invitationsApi;
    }

    [RelayCommand]
    private async Task AcceptInvitationAsync(InvitationSummary? invitation)
    {
        if (invitation is null || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _invitationsApi.AcceptInvitationAsync(invitation.Id);
            await LoadAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task CreateGroupAsync()
    {
        return Shell.Current.GoToAsync("create-group");
    }

    [RelayCommand]
    private Task OpenGroupAsync(GroupSummary? group)
    {
        if (group is null)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync("group-detail", new Dictionary<string, object>
        {
            ["groupId"] = group.Id
        });
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var response = await _groupsApi.GetGroupsAsync();
            OwnedGroups.Clear();
            MemberGroups.Clear();
            PendingInvitations.Clear();

            foreach (var group in response.Owned)
            {
                OwnedGroups.Add(group);
            }

            foreach (var group in response.Member)
            {
                MemberGroups.Add(group);
            }

            foreach (var invitation in response.PendingInvitations)
            {
                PendingInvitations.Add(invitation);
            }

            ShowEmptyState = OwnedGroups.Count == 0 && MemberGroups.Count == 0;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
