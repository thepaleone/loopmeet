using System.Collections.ObjectModel;
using System.Net.Http;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Auth;
using LoopMeet.App.Features.Groups.Models;
using LoopMeet.App.Features.Safety.Models;
using LoopMeet.App.Services;
using Microsoft.Extensions.Logging;
using Refit;

namespace LoopMeet.App.Features.Safety.ViewModels;

/// <summary>
/// Report a concern (feature 023 US1). The rules the user notices live in the MAUI-free
/// ReportDraft, ReportFallbackMail and SafetyErrorText, which are unit tested.
/// </summary>
public sealed partial class ReportConcernViewModel : ObservableObject
{
    private readonly GroupsApi _groupsApi;
    private readonly SafetyApi _safetyApi;
    private readonly AuthService _authService;
    private readonly ILogger<ReportConcernViewModel> _logger;
    private Guid? _preselectedGroupId;
    private Guid? _preselectedUserId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    [NotifyCanExecuteChangedFor(nameof(SubmitReportCommand))]
    private GroupSummary? _selectedGroup;

    [ObservableProperty]
    private ReportPersonOption? _selectedPerson;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    [NotifyCanExecuteChangedFor(nameof(SubmitReportCommand))]
    private ReportReasonOption? _selectedReason;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit), nameof(RemainingText))]
    [NotifyCanExecuteChangedFor(nameof(SubmitReportCommand))]
    private string _details = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    [NotifyCanExecuteChangedFor(nameof(SubmitReportCommand))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFormVisible))]
    private bool _isLoaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsFormVisible))]
    private bool _hasNoGroups;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _showError;

    [ObservableProperty]
    private bool _showEmailFallback;

    public ReportConcernViewModel(GroupsApi groupsApi, SafetyApi safetyApi, AuthService authService, ILogger<ReportConcernViewModel> logger)
    {
        _groupsApi = groupsApi;
        _safetyApi = safetyApi;
        _authService = authService;
        _logger = logger;
    }

    public ObservableCollection<GroupSummary> Groups { get; } = new();

    public ObservableCollection<ReportPersonOption> People { get; } = new();

    public IReadOnlyList<ReportReasonOption> Reasons { get; } = ReportReasons.All;

    public bool IsFormVisible => IsLoaded && !HasNoGroups;

    public string RemainingText => ReportDraft.Remaining(Details);

    public bool CanSubmit => !IsBusy && ReportDraft.CanSubmit(SelectedGroup?.Id, SelectedReason?.Code, Details);

    /// <summary>From Group Detail's member actions: the group and person to select once loaded.</summary>
    public void Preselect(Guid? groupId, Guid? userId)
    {
        _preselectedGroupId = groupId;
        _preselectedUserId = userId;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (IsLoaded || IsBusy)
        {
            return;
        }

        IsBusy = true;
        ShowError = false;
        try
        {
            var groups = await _groupsApi.GetGroupsAsync();
            var all = groups.Owned.Concat(groups.Member).OrderBy(group => group.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Groups.Clear();
                foreach (var group in all)
                {
                    Groups.Add(group);
                }

                HasNoGroups = Groups.Count == 0;
                IsLoaded = true;
                SelectedGroup = Groups.FirstOrDefault(group => group.Id == _preselectedGroupId);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load groups for the report form.");
            ErrorMessage = "Couldn't load your groups. Check your connection and try again.";
            ShowError = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnSelectedGroupChanged(GroupSummary? value)
    {
        People.Clear();
        SelectedPerson = null;
        if (value is not null)
        {
            _ = LoadPeopleAsync(value.Id);
        }
    }

    private async Task LoadPeopleAsync(Guid groupId)
    {
        try
        {
            var detail = await _groupsApi.GetGroupAsync(groupId);
            var self = _authService.GetCurrentUserId();
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (SelectedGroup?.Id != groupId)
                {
                    return;
                }

                People.Clear();
                People.Add(new ReportPersonOption(null, ReportPersonOption.GroupItself));
                foreach (var member in detail.Members.Where(member => member.UserId != self).OrderBy(member => member.DisplayName, StringComparer.CurrentCultureIgnoreCase))
                {
                    People.Add(new ReportPersonOption(member.UserId, member.DisplayName));
                }

                // The group itself is selected unless Group Detail chose a person, so the choice is always visible.
                SelectedPerson = People.FirstOrDefault(person => person.UserId is not null && person.UserId == _preselectedUserId) ?? People[0];
                _preselectedUserId = null;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load members of {GroupId} for the report form.", groupId);
            ErrorMessage = "Couldn't load this group's members. Check your connection and try again.";
            ShowError = true;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitReportAsync()
    {
        IsBusy = true;
        ShowError = false;
        ShowEmailFallback = false;
        try
        {
            await _safetyApi.SubmitReportAsync(new ReportRequest
            {
                GroupId = SelectedGroup!.Id,
                ReportedUserId = SelectedPerson?.UserId,
                Reason = SelectedReason!.Code,
                Details = ReportDraft.Trimmed(Details)
            });

            await Shell.Current.DisplayAlertAsync(
                "Report sent",
                "Thank you. Our safety team reviews every report, and may email you if they need more information.",
                "OK");
            await Shell.Current.GoToAsync("..");
        }
        catch (ApiException ex) when (!SafetyErrorText.OffersEmailFallback((int)ex.StatusCode))
        {
            _logger.LogWarning("Report refused with {StatusCode}.", (int)ex.StatusCode);
            ErrorMessage = SafetyErrorText.Message(SafetyErrorText.CodeFrom(ex.Content));
            ShowError = true;
        }
        catch (Exception ex) when (ex is ApiException or HttpRequestException or TaskCanceledException)
        {
            // Nothing typed is lost: the form keeps its fields, and the email route sends the same content.
            _logger.LogWarning(ex, "Report couldn't be sent; offering email.");
            ErrorMessage = "Your report couldn't be sent. You can send it by email instead.";
            ShowError = true;
            ShowEmailFallback = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SendByEmailAsync()
    {
        var mail = ReportFallbackMail.Compose(
            SelectedGroup?.Name ?? string.Empty,
            SelectedPerson?.UserId is null ? null : SelectedPerson.Name,
            SelectedReason?.Label ?? string.Empty,
            Details,
            $"{AppInfo.Current.VersionString} ({AppInfo.Current.BuildString})");

        if (Email.Default.IsComposeSupported)
        {
            try
            {
                await Email.Default.ComposeAsync(new EmailMessage
                {
                    To = [mail.To],
                    Subject = mail.Subject,
                    Body = mail.Body,
                    BodyFormat = EmailBodyFormat.PlainText
                });
                return;
            }
            catch (FeatureNotSupportedException ex)
            {
                _logger.LogWarning(ex, "Mail composer unavailable; showing the address.");
            }
        }

        await ShowAddressAsync();
    }

    [RelayCommand]
    private Task CopyAddressAsync() => Clipboard.Default.SetTextAsync(ReportFallbackMail.SafetyAddress);

    private async Task ShowAddressAsync()
    {
        var copy = await Shell.Current.DisplayAlertAsync(
            "Email your report",
            $"No mail app is set up on this device. Send your report to {ReportFallbackMail.SafetyAddress}.",
            "Copy address",
            "Close");
        if (copy)
        {
            await Clipboard.Default.SetTextAsync(ReportFallbackMail.SafetyAddress);
        }
    }
}
