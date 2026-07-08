using CommunityToolkit.Mvvm.ComponentModel;
using LoopMeet.App.Features.Auth.Session;
using LoopMeet.App.Features.Home.Models;
using LoopMeet.App.Services;
using LoopMeet.App.Services.Notifications;
using Microsoft.Extensions.Logging;
using Refit;

namespace LoopMeet.App.Features.Auth.ViewModels;

public sealed partial class StartupGateViewModel : ObservableObject
{
    private readonly SessionCoordinator _sessionCoordinator;
    private readonly UsersApi _usersApi;
    private readonly UserProfileCache _userProfileCache;
    private readonly PostLoginNotificationRedirectService _postLoginRedirectService;
    private readonly ILogger<StartupGateViewModel> _logger;
    private bool _resolved;

    [ObservableProperty]
    private string _statusText = "Checking your session…";

    public StartupGateViewModel(
        SessionCoordinator sessionCoordinator,
        UsersApi usersApi,
        UserProfileCache userProfileCache,
        PostLoginNotificationRedirectService postLoginRedirectService,
        ILogger<StartupGateViewModel> logger)
    {
        _sessionCoordinator = sessionCoordinator;
        _usersApi = usersApi;
        _userProfileCache = userProfileCache;
        _postLoginRedirectService = postLoginRedirectService;
        _logger = logger;
    }

    /// <summary>Exactly one navigation per launch (FR-009/SC-004).</summary>
    public async Task ResolveAsync()
    {
        if (_resolved)
        {
            return;
        }

        _resolved = true;
        var resolution = await _sessionCoordinator.ResolveStartupAsync();
        await Shell.Current.GoToAsync(resolution.Route);

        if (resolution.Route == SignedInTabs.HomeShellPath)
        {
            // Warm-up work that used to block the startup redirect (bug C2);
            // it must never delay or gate navigation.
            _ = RunSignedInWarmupAsync();
        }
    }

    private async Task RunSignedInWarmupAsync()
    {
        try
        {
            var profile = await _usersApi.GetProfileSummaryAsync();
            _userProfileCache.SetCachedProfile(profile);
        }
        catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Startup profile warm-up failed; cached profile retained.");
        }

        try
        {
            await _postLoginRedirectService.ResumeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Post-login notification redirect resume failed.");
        }
    }
}
