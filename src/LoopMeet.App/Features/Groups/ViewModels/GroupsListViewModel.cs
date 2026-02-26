using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Auth;
using LoopMeet.App.Features.Groups.Models;
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
    private readonly ILogger<GroupsListViewModel> _logger;

    public ObservableCollection<GroupSummary> OwnedGroups { get; } = new();
    public ObservableCollection<GroupSummary> MemberGroups { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _showEmptyState;

    [ObservableProperty]
    private bool _isLoadingOwnedGroups;

    [ObservableProperty]
    private bool _isLoadingMemberGroups;

    public GroupsListViewModel(
        AuthService authService,
        GroupsApi groupsApi,
        ILogger<GroupsListViewModel> logger)
    {
        _authService = authService;
        _groupsApi = groupsApi;
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
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to load groups tab list: API unavailable.");
            await ShowApiUnavailableAndQuitAsync();
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Failed to load groups tab list: request timed out.");
            await ShowApiUnavailableAndQuitAsync();
        }
        catch (Refit.ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            if ((apiEx.ReasonPhrase ?? "").Contains("Unauthorized", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(apiEx, "Failed to load groups tab list: unauthorized. Access token may be invalid or expired.");
                await MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync("//login"));
            }
            else
            {
                _logger.LogError(apiEx, "Failed to load groups tab list: API returned unauthorized. Reason: {ReasonPhrase}", apiEx.ReasonPhrase);
                await ShowApiUnavailableAndQuitAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load groups tab list.");
            await Shell.Current.DisplayAlertAsync(
                "Un Oh!", 
                "Something went pear shaped while trying to load your groups. To try to fix this please try logging in again.",
                "OK");
            await MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync("//login"));
            
        }
        finally
        {
            IsBusy = false;
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
#if !IOS
            Process.GetCurrentProcess().Kill(true);
#endif
        });
    }

}
