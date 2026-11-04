using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Auth.Session;
using LoopMeet.App.Features.Home.Models;
using LoopMeet.App.Services;
using LoopMeet.App.Services.Auth;
using Microsoft.Extensions.Logging;
using Refit;

namespace LoopMeet.App.Features.Account.ViewModels;

/// <summary>
/// The scheduled-deletion screen (016 US3). Only Restore account cancels the deletion: signing in,
/// a session refresh or Sign out leave it scheduled.
/// </summary>
public sealed partial class PendingDeletionViewModel : ObservableObject, IQueryAttributable
{
    private readonly UsersApi _usersApi;
    private readonly AccountDeletionGate _gate;
    private readonly SessionCoordinator _sessionCoordinator;
    private readonly UserProfileCache _userProfileCache;
    private readonly AuthSessionService _authSessionService;
    private readonly ILogger<PendingDeletionViewModel> _logger;

    private DateTimeOffset? _deletesAt;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private bool _isRestoreAvailable = true;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _showError;

    public PendingDeletionViewModel(
        UsersApi usersApi,
        AccountDeletionGate gate,
        SessionCoordinator sessionCoordinator,
        UserProfileCache userProfileCache,
        AuthSessionService authSessionService,
        ILogger<PendingDeletionViewModel> logger)
    {
        _usersApi = usersApi;
        _gate = gate;
        _sessionCoordinator = sessionCoordinator;
        _userProfileCache = userProfileCache;
        _authSessionService = authSessionService;
        _logger = logger;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("deletesAt", out var value) && value is DateTimeOffset deletesAt)
        {
            _deletesAt = deletesAt;
        }

        ShowPending();
    }

    /// <summary>Refreshes the status: past the deletion date the account reads as purging and can't be restored.</summary>
    public async Task LoadAsync()
    {
        try
        {
            var status = await _usersApi.GetAccountDeletionAsync();
            _deletesAt = status.DeletesAt ?? _deletesAt;
            if (status.Status == "purging")
            {
                ShowPurging("Your account is being deleted.");
            }
            else
            {
                ShowPending();
            }
        }
        catch (Exception ex)
        {
            // The date from the 403 is still shown; Restore reports a refusal if it's too late.
            _logger.LogWarning(ex, "Failed to load the account deletion status.");
        }
    }

    [RelayCommand]
    private async Task RestoreAsync()
    {
        if (IsBusy || !IsRestoreAvailable)
        {
            return;
        }

        IsBusy = true;
        ShowError = false;
        try
        {
            await _usersApi.RestoreAccountAsync();
            _gate.Reset();
            await FinishSignInAsync();
            await Shell.Current.GoToAsync(SignedInTabs.HomeShellPath);
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogWarning("Account restore refused: deletion in progress.");
            ShowPurging("Your account can no longer be restored.");
            await Shell.Current.DisplayAlertAsync("Account deletion", "Your account can no longer be restored.", "OK");
            _gate.Reset();
            await _sessionCoordinator.SignOutAsync(SignOutReason.UserInitiated);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Account restore failed.");
            ErrorMessage = "Couldn't restore your account. Check your connection and try again.";
            ShowError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SignOutAsync()
    {
        _gate.Reset();
        await _sessionCoordinator.SignOutAsync(SignOutReason.UserInitiated);
    }

    // Sign-in stopped at the 403 before caching the profile and registering the device, so finish
    // that now. Best effort: the account is already restored.
    private async Task FinishSignInAsync()
    {
        try
        {
            _userProfileCache.SetCachedProfile(await _usersApi.GetProfileSummaryAsync());
            await _authSessionService.HandleSuccessfulSignInAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Post-restore sign-in setup failed; continuing to home.");
        }
    }

    private void ShowPending()
    {
        IsRestoreAvailable = true;
        Message = _deletesAt is { } deletesAt
            ? $"Your account is scheduled for deletion on {deletesAt.ToLocalTime():D}."
            : "Your account is scheduled for deletion.";
    }

    private void ShowPurging(string message)
    {
        IsRestoreAvailable = false;
        Message = message;
    }
}
