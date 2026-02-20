using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Groups.Models;
using LoopMeet.App.Features.Invitations.Models;
using LoopMeet.App.Services;
using Microsoft.Extensions.Logging;

namespace LoopMeet.App.Features.Groups.ViewModels;

public sealed partial class GroupsListViewModel : ObservableObject
{
    private readonly GroupsApi _groupsApi;
    private readonly InvitationsApi _invitationsApi;
    private readonly ILogger<GroupsListViewModel> _logger;

    public ObservableCollection<GroupSummary> OwnedGroups { get; } = new();
    public ObservableCollection<GroupSummary> MemberGroups { get; } = new();
    public ObservableCollection<InvitationSummary> PendingInvitations { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _showEmptyState;

    public GroupsListViewModel(
        GroupsApi groupsApi,
        InvitationsApi invitationsApi,
        ILogger<GroupsListViewModel> logger)
    {
        _groupsApi = groupsApi;
        _invitationsApi = invitationsApi;
        _logger = logger;
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
        }
        finally
        {
            IsBusy = false;
        }

        await LoadCoreAsync();
    }

    [RelayCommand]
    private async Task DeclineInvitationAsync(InvitationSummary? invitation)
    {
        if (invitation is null || IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _invitationsApi.DeclineInvitationAsync(invitation.Id);
        }
        finally
        {
            IsBusy = false;
        }

        await LoadCoreAsync();
    }

    [RelayCommand]
    private Task ShowInvitationDetailsAsync(InvitationSummary? invitation)
    {
        if (invitation is null)
        {
            return Task.CompletedTask;
        }

        var sentAt = invitation.CreatedAt?.ToLocalTime().ToString("g") ?? "Unknown";
        var owner = FormatPerson(invitation.OwnerName, invitation.OwnerEmail);
        var sender = FormatPerson(invitation.SenderName, invitation.SenderEmail);

        return Shell.Current.DisplayAlert(
            "Invitation",
            $"Group: {invitation.GroupName}\nOwner: {owner}\nInvited by: {sender}\nSent: {sentAt}",
            "Close");
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

        await LoadCoreAsync();
    }

    private async Task LoadCoreAsync()
    {
        IsBusy = true;
        try
        {
            _logger.LogInformation("Loading groups list...");
            var response = await _groupsApi.GetGroupsAsync();
            OwnedGroups.Clear();
            MemberGroups.Clear();
            PendingInvitations.Clear();

            if (response?.Owned is not null)
            {
                foreach (var group in response.Owned)
                {
                    OwnedGroups.Add(group);
                }
            }

            if (response?.Member is not null)
            {
                foreach (var group in response.Member)
                {
                    MemberGroups.Add(group);
                }
            }

            if (response?.PendingInvitations is not null)
            {
                foreach (var invitation in response.PendingInvitations)
                {
                    PendingInvitations.Add(invitation);
                }
            }

            ShowEmptyState = OwnedGroups.Count == 0 && MemberGroups.Count == 0;

            _logger.LogInformation(
                "Groups list loaded. Owned: {OwnedCount}, Member: {MemberCount}, Invitations: {InvitationCount}",
                OwnedGroups.Count,
                MemberGroups.Count,
                PendingInvitations.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load groups list.");
            throw;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string FormatPerson(string name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return email;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return name;
        }

        return $"{name} ({email})";
    }
}
