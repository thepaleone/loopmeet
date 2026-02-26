using System.Collections.ObjectModel;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Invitations.Models;
using LoopMeet.App.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using Refit;

namespace LoopMeet.App.Features.Invitations.ViewModels;

public sealed partial class PendingInvitationsViewModel : ObservableObject
{
    private readonly InvitationsApi _invitationsApi;
    private readonly ILogger<PendingInvitationsViewModel> _logger;

    public ObservableCollection<InvitationSummary> PendingInvitations { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _showEmptyState;

    public PendingInvitationsViewModel(
        InvitationsApi invitationsApi,
        ILogger<PendingInvitationsViewModel> logger)
    {
        _invitationsApi = invitationsApi;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        IsLoading = true;
        try
        {
            PendingInvitations.Clear();
            var response = await _invitationsApi.GetInvitationsAsync();
            if (response?.Invitations is not null)
            {
                foreach (var invitation in response.Invitations)
                {
                    PendingInvitations.Add(invitation);
                }
            }

            ShowEmptyState = PendingInvitations.Count == 0;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to load pending invitations: API unavailable.");
            await ShowLoadErrorAsync("We could not contact the LoopMeet service. Please try again later.");
            ShowEmptyState = PendingInvitations.Count == 0;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Failed to load pending invitations: request timed out.");
            await ShowLoadErrorAsync("The request timed out. Please try again.");
            ShowEmptyState = PendingInvitations.Count == 0;
        }
        catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogError(apiEx, "Failed to load pending invitations: unauthorized.");
            await MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync("//login"));
            ShowEmptyState = PendingInvitations.Count == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load pending invitations.");
            await ShowLoadErrorAsync("Something went wrong while loading invitations. Please try again.");
            ShowEmptyState = PendingInvitations.Count == 0;
        }
        finally
        {
            IsLoading = false;
            IsBusy = false;
        }
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

        await LoadAsync();
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

        await LoadAsync();
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

    private static Task ShowLoadErrorAsync(string message)
    {
        return MainThread.InvokeOnMainThreadAsync(() =>
            Shell.Current.DisplayAlertAsync("Unable to Load Invitations", message, "OK"));
    }
}
