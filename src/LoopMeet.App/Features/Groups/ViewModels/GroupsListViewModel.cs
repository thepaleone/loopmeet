using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Auth;
using LoopMeet.App.Features.Groups.Models;
using LoopMeet.App.Features.Invitations.Models;
using LoopMeet.App.Services;
using System.Diagnostics;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;
#if MACCATALYST
using ObjCRuntime;
using UIKit;
#endif

namespace LoopMeet.App.Features.Groups.ViewModels;

public sealed partial class GroupsListViewModel : ObservableObject
{
    private readonly AuthService _authService;
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

    [ObservableProperty]
    private bool _isLoadingInvitations;

    [ObservableProperty]
    private bool _isLoadingOwnedGroups;

    [ObservableProperty]
    private bool _isLoadingMemberGroups;

    public GroupsListViewModel(
        AuthService authService,
        GroupsApi groupsApi,
        InvitationsApi invitationsApi,
        ILogger<GroupsListViewModel> logger)
    {
        _authService = authService;
        _groupsApi = groupsApi;
        _invitationsApi = invitationsApi;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            await _authService.SignOutAsync();
        }
        finally
        {
            IsBusy = false;
        }

        await Shell.Current.GoToAsync("//login");
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

        return Shell.Current.GoToAsync("invitation-detail", new Dictionary<string, object>
        {
            ["invitation"] = invitation
        });
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
        IsLoadingInvitations = true;
        IsLoadingOwnedGroups = true;
        IsLoadingMemberGroups = true;
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
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to load groups list: API unavailable.");
            await ShowApiUnavailableAndQuitAsync();
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Failed to load groups list: request timed out.");
            await ShowApiUnavailableAndQuitAsync();
        }
        catch (Refit.ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            if ((apiEx.ReasonPhrase ?? "").Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(apiEx, "Failed to load groups list: unauthorized. Access token may be invalid or expired.");
                await MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync("//login"));
            }
            else
            {
                _logger.LogError(apiEx, "Failed to load groups list: API returned unauthorized. Reason: {ReasonPhrase}", apiEx.ReasonPhrase);
                await ShowApiUnavailableAndQuitAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load groups list.");
            await Shell.Current.DisplayAlertAsync(
                "Un Oh!", 
                "Something went pear shaped while trying to load your page. To try to fix this please try logging in again.",
                "OK");
            await MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync("//login"));
            
        }
        finally
        {
            IsBusy = false;
            IsLoadingInvitations = false;
            IsLoadingOwnedGroups = false;
            IsLoadingMemberGroups = false;
        }
    }

    private static Task ShowApiUnavailableAndQuitAsync()
    {
        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
#pragma warning disable CS0618
            await Shell.Current.DisplayAlertAsync(
                "Service Unavailable",
                "We could not contact the LoopMeet service. Please try again later.",
                "Quit");
#pragma warning restore CS0618

            Application.Current?.Quit();
#if MACCATALYST
            try
            {
                UIApplication.SharedApplication.PerformSelector(
                    new Selector("terminateWithSuccess"),
                    UIApplication.SharedApplication,
                    0);
            }
            catch
            {
            }
#endif
            Environment.Exit(0);
            await Task.Delay(200);
            Process.GetCurrentProcess().Kill(true);
        });
    }

}
