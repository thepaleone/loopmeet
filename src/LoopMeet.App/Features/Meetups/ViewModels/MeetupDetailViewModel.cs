using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Auth;
using LoopMeet.App.Features.Meetups.Models;
using LoopMeet.App.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace LoopMeet.App.Features.Meetups.ViewModels;

/// <summary>
/// Read-only view of one meetup. Loads itself from (groupId, meetupId) alone so
/// it behaves identically however it was reached — Home card, Group Detail card,
/// or a future deep link — and always shows current values rather than whatever
/// a caller happened to be holding.
/// </summary>
public sealed partial class MeetupDetailViewModel : ObservableObject
{
    private readonly MeetupsApi _meetupsApi;
    private readonly AuthService _authService;
    private readonly ILogger<MeetupDetailViewModel> _logger;

    private Guid _groupId;
    private Guid _meetupId;
    private MeetupSummary? _meetup;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _dateTimeDisplay = string.Empty;

    [ObservableProperty]
    private string _locationDisplay = string.Empty;

    [ObservableProperty]
    private string _groupName = string.Empty;

    [ObservableProperty]
    private string _organizerDisplay = string.Empty;

    [ObservableProperty]
    private bool _canOpenLocation;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isLoaded;

    [ObservableProperty]
    private bool _isNotFound;

    [ObservableProperty]
    private bool _hasError;

    /// <summary>
    /// Whether the current user owns this meetup's group — the only input to
    /// showing the edit control. Not the entry point, and not who created the
    /// meetup: organizing one grants no edit access.
    /// </summary>
    [ObservableProperty]
    private bool _isOwner;

    public MeetupDetailViewModel(
        MeetupsApi meetupsApi,
        AuthService authService,
        ILogger<MeetupDetailViewModel> logger)
    {
        _meetupsApi = meetupsApi;
        _authService = authService;
        _logger = logger;
    }

    public void ApplyParameters(Guid groupId, Guid meetupId)
    {
        _groupId = groupId;
        _meetupId = meetupId;
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (_groupId == Guid.Empty || _meetupId == Guid.Empty)
        {
            return;
        }

        IsLoading = true;
        IsLoaded = false;
        IsNotFound = false;
        HasError = false;
        try
        {
            var response = await _meetupsApi.GetGroupMeetupsAsync(_groupId);
            var meetup = response.Meetups.FirstOrDefault(item => item.Id == _meetupId);
            if (meetup is null)
            {
                // Deleted, or now in the past — both lists are upcoming-only.
                // IsOwner stays false so no edit path is offered for a meetup
                // that may not exist.
                _meetup = null;
                IsOwner = false;
                IsNotFound = true;
                _logger.LogInformation("Meetup {MeetupId} in group {GroupId} is no longer listed.", _meetupId, _groupId);
                return;
            }

            Apply(meetup);
            IsLoaded = true;
        }
        catch (Exception ex)
        {
            _meetup = null;
            IsOwner = false;
            HasError = true;
            _logger.LogError(ex, "Failed to load meetup {MeetupId} in group {GroupId}.", _meetupId, _groupId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task OpenLocationAsync()
    {
        if (_meetup is not { CanOpenLocation: true } meetup)
        {
            return;
        }

        try
        {
            await Map.Default.OpenAsync(
                meetup.Latitude!.Value,
                meetup.Longitude!.Value,
                new MapLaunchOptions { Name = meetup.PlaceName ?? string.Empty });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not open maps for meetup {MeetupId}.", _meetupId);
        }
    }

    [RelayCommand]
    private Task EditAsync()
    {
        if (!IsOwner || _meetup is null)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync("edit-meetup", new Dictionary<string, object>
        {
            ["groupId"] = _groupId,
            ["meetupId"] = _meetupId
        });
    }

    private void Apply(MeetupSummary meetup)
    {
        _meetup = meetup;
        Title = meetup.Title;
        DateTimeDisplay = meetup.DateTimeDisplay;
        LocationDisplay = meetup.LocationDisplay;
        GroupName = string.IsNullOrWhiteSpace(meetup.GroupName) ? "Unknown group" : meetup.GroupName;
        OrganizerDisplay = meetup.OrganizerDisplay;
        CanOpenLocation = meetup.CanOpenLocation;

        var currentUserId = _authService.GetCurrentUserId();
        IsOwner = currentUserId.HasValue
            && meetup.GroupOwnerUserId != Guid.Empty
            && currentUserId.Value == meetup.GroupOwnerUserId;
    }
}
