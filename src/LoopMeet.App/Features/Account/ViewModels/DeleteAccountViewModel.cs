using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Account.Models;
using LoopMeet.App.Features.Auth.Session;
using LoopMeet.App.Services;
using Microsoft.Extensions.Logging;

namespace LoopMeet.App.Features.Account.ViewModels;

public sealed partial class DeleteAccountViewModel : ObservableObject
{
    private readonly UsersApi _usersApi;
    private readonly SessionCoordinator _sessionCoordinator;
    private readonly ILogger<DeleteAccountViewModel> _logger;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    private string _confirmation = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanDelete))]
    private bool _isLoaded;

    [ObservableProperty]
    private bool _hasOwnedGroups;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFormVisible))]
    private bool _isScheduled;

    [ObservableProperty]
    private string _scheduledMessage = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _showError;

    public DeleteAccountViewModel(
        UsersApi usersApi,
        SessionCoordinator sessionCoordinator,
        ILogger<DeleteAccountViewModel> logger)
    {
        _usersApi = usersApi;
        _sessionCoordinator = sessionCoordinator;
        _logger = logger;
    }

    public ObservableCollection<OwnedGroupSummary> OwnedGroups { get; } = new();

    public bool IsFormVisible => !IsScheduled;

    public bool CanDelete => IsLoaded && !IsBusy && AccountDeletionConfirmation.IsConfirmed(Confirmation);

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ShowError = false;
        try
        {
            var preview = await _usersApi.GetAccountDeletionPreviewAsync();
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                OwnedGroups.Clear();
                foreach (var group in preview.OwnedGroups)
                {
                    OwnedGroups.Add(group);
                }

                HasOwnedGroups = OwnedGroups.Count > 0;
                IsLoaded = true;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load the account deletion preview.");
            ErrorMessage = "Couldn't load what will be deleted. Check your connection and try again.";
            ShowError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync()
    {
        if (!CanDelete)
        {
            return;
        }

        IsBusy = true;
        ShowError = false;
        try
        {
            var status = await _usersApi.RequestAccountDeletionAsync(Confirmation);
            var deletesAt = status.DeletesAt ?? DateTimeOffset.Now;
            ScheduledMessage = $"Your account will be permanently deleted on {deletesAt.ToLocalTime():D}. "
                + "Sign in before then if you change your mind.";
            IsScheduled = true;
        }
        catch (Exception ex)
        {
            // The account stays signed in and unchanged, so the user can simply try again.
            _logger.LogError(ex, "Failed to request account deletion.");
            ErrorMessage = "Couldn't schedule the deletion. Check your connection and try again.";
            ShowError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task AcknowledgeAsync() => _sessionCoordinator.SignOutAsync(SignOutReason.UserInitiated);

    [RelayCommand]
    private Task CancelAsync() => Shell.Current.GoToAsync("..");
}
