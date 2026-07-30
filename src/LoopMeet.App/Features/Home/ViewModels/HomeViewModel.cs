using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Meetups.Models;
using LoopMeet.App.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;

namespace LoopMeet.App.Features.Home.ViewModels;

public sealed partial class HomeViewModel(
    UserProfileCache userProfileCache,
    MeetupsApi meetupsApi,
    ILogger<HomeViewModel> logger) : ObservableObject
{
    [ObservableProperty]
    private string _title = "Hello";

    [ObservableProperty]
    private string? _avatarUrl;

    [ObservableProperty]
    private bool _hasAvatar;

    [ObservableProperty]
    private string _userInitial = "";

    public ObservableCollection<MeetupSummary> UpcomingMeetups { get; } = new();

    [ObservableProperty]
    private bool _hasUpcomingMeetups;

    [ObservableProperty]
    private bool _showEmptyState;

    [RelayCommand]
    private async Task LoadAsync()
    {
        var cached = userProfileCache.GetCachedProfile();
        Title = cached is not null && !string.IsNullOrWhiteSpace(cached.DisplayName)
            ? $"Hello {cached.DisplayName}"
            : "Hello";
        AvatarUrl = cached?.AvatarUrl;
        HasAvatar = !string.IsNullOrWhiteSpace(AvatarUrl);
        UserInitial = cached?.DisplayName?.Length > 0
            ? cached.DisplayName[0].ToString().ToUpperInvariant()
            : "?";

        try
        {
            var meetupsResponse = await meetupsApi.GetUpcomingMeetupsAsync();
            UpcomingMeetups.Clear();
            foreach (var meetup in meetupsResponse.Meetups)
            {
                UpcomingMeetups.Add(meetup);
            }
            HasUpcomingMeetups = UpcomingMeetups.Count > 0;
            ShowEmptyState = !HasUpcomingMeetups;
        }
        catch (Refit.ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            // Session end is handled centrally (refresh-retry, then forced
            // sign-out + routing by the SessionCoordinator) — never masked
            // as an empty state (FR-005).
            logger.LogInformation(apiEx, "Home load unauthorized; session handling owns the response.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load upcoming meetups.");
            HasUpcomingMeetups = false;
            ShowEmptyState = true;
        }
    }

    [RelayCommand]
    private Task OpenMeetupDetailAsync(MeetupSummary? meetup)
    {
        if (meetup is null || meetup.GroupId == Guid.Empty)
        {
            return Task.CompletedTask;
        }

        return Shell.Current.GoToAsync("meetup-detail", new Dictionary<string, object>
        {
            ["groupId"] = meetup.GroupId,
            ["meetupId"] = meetup.Id
        });
    }

    [RelayCommand]
    private async Task OpenLocationAsync(MeetupSummary? meetup)
    {
        if (meetup is not { CanOpenLocation: true })
        {
            return;
        }

        try
        {
            await Map.Default.OpenAsync(meetup.Latitude!.Value, meetup.Longitude!.Value,
                new MapLaunchOptions { Name = meetup.PlaceName ?? string.Empty });
        }
        catch
        {
            // Maps app not available
        }
    }
}
