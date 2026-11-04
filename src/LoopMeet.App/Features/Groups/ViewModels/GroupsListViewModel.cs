using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Groups.Models;
using LoopMeet.App.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Extensions.Logging;

namespace LoopMeet.App.Features.Groups.ViewModels;

public sealed partial class GroupsListViewModel : ObservableObject
{
    private readonly GroupsApi _groupsApi;
    private readonly ILogger<GroupsListViewModel> _logger;

    public ObservableCollection<GroupSummary> OwnedGroups { get; } = new();
    public ObservableCollection<GroupSummary> MemberGroups { get; } = new();

    [ObservableProperty]
    private bool _showEmptyState;

    [ObservableProperty]
    private bool _isLoadingOwnedGroups;

    [ObservableProperty]
    private bool _isLoadingMemberGroups;

    [ObservableProperty]
    private bool _isRefreshing;

    // The load in progress, shared by OnAppearing and pull to refresh: a pull mid-load waits for
    // that load instead of stopping the spinner straight away.
    private Task? _inFlightLoad;

    public GroupsListViewModel(
        GroupsApi groupsApi,
        ILogger<GroupsListViewModel> logger)
    {
        _groupsApi = groupsApi;
        _logger = logger;
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
    private Task LoadAsync() => LoadOnceAsync();

    /// <summary>Pull to refresh, as on group detail: the spinner stops however the load ends.</summary>
    [RelayCommand]
    private async Task RefreshAsync()
    {
        try
        {
            await LoadOnceAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private Task LoadOnceAsync()
    {
        if (_inFlightLoad is { IsCompleted: false })
        {
            return _inFlightLoad;
        }

        return _inFlightLoad = LoadCoreAsync();
    }

    private async Task LoadCoreAsync()
    {
        IsLoadingOwnedGroups = true;
        IsLoadingMemberGroups = true;
        try
        {
            _logger.LogInformation("Loading groups tab list...");
            var response = await _groupsApi.GetGroupsAsync();
            OwnedGroups.Clear();
            MemberGroups.Clear();

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

            ShowEmptyState = OwnedGroups.Count == 0 && MemberGroups.Count == 0;

            _logger.LogInformation(
                "Groups tab list loaded. Owned: {OwnedCount}, Member: {MemberCount}",
                OwnedGroups.Count,
                MemberGroups.Count);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            // Offline or timed out: keep whatever is listed so a pull to refresh can try again.
            _logger.LogError(ex, "Failed to load groups tab list: API unreachable or timed out.");
            await Shell.Current.DisplayAlertAsync(
                "Couldn't load groups",
                "Couldn't load your groups. Check your connection and try again.",
                "OK");
        }
        catch (Refit.ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Session end is handled centrally (refresh-retry, then forced
            // sign-out + routing by the SessionCoordinator) — no per-screen redirect.
            _logger.LogInformation(apiEx, "Groups load unauthorized; session handling owns the response.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load groups tab list.");
            await Shell.Current.DisplayAlertAsync(
                "Un Oh!",
                "Something went pear shaped while trying to load your groups. Please try again.",
                "OK");
        }
        finally
        {
            IsLoadingOwnedGroups = false;
            IsLoadingMemberGroups = false;
        }
    }

}
