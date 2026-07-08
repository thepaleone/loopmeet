using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LoopMeet.App.Features.Auth.Models;
using LoopMeet.App.Features.Auth.Session;
using LoopMeet.App.Features.Home.Models;
using LoopMeet.App.Features.Profile.Models;
using LoopMeet.App.Services;
using Microsoft.Extensions.Logging;
using Refit;
using LoopMeet.App.Services.Auth;

namespace LoopMeet.App.Features.Auth.ViewModels;

public sealed partial class LoginViewModel : ObservableObject
{
    private static readonly TimeSpan PostSignInSetupTimeout = TimeSpan.FromSeconds(10);

    private readonly AuthService _authService;
    private readonly AuthCoordinator _authCoordinator;
    private readonly UsersApi _usersApi;
    private readonly UserProfileCache _userProfileCache;
    private readonly AuthSessionService _authSessionService;
    private readonly SessionNoticeState _sessionNoticeState;
    private readonly ILogger<LoginViewModel> _logger;

    // Cancels a provider sign-in whose native sheet was abandoned (FR-007):
    // the awaited callback may never fire, and it must not hold IsBusy hostage.
    private CancellationTokenSource? _providerSignInCts;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _showError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSessionEndedNotice))]
    private string? _sessionEndedNotice;

    public bool HasSessionEndedNotice => !string.IsNullOrEmpty(SessionEndedNotice);

    public bool ShowAppleSignIn =>
#if IOS || MACCATALYST
        true;
#else
        false;
#endif

    public LoginViewModel(
        AuthService authService,
        AuthCoordinator authCoordinator,
        UsersApi usersApi,
        UserProfileCache userProfileCache,
        AuthSessionService authSessionService,
        SessionNoticeState sessionNoticeState,
        ILogger<LoginViewModel> logger)
    {
        _authService = authService;
        _authCoordinator = authCoordinator;
        _usersApi = usersApi;
        _userProfileCache = userProfileCache;
        _authSessionService = authSessionService;
        _sessionNoticeState = sessionNoticeState;
        _logger = logger;
    }

    /// <summary>Consume-once read of the session-ended notice (contract §6a).</summary>
    public void RefreshSessionNotice()
    {
        var notice = SignOutNotices.For(_sessionNoticeState.TakePending());
        if (notice is not null)
        {
            SessionEndedNotice = notice;
        }
    }

    /// <summary>Called from LoginPage.OnDisappearing so an abandoned provider sheet never wedges a later attempt.</summary>
    public void CancelPendingProviderSignIn()
    {
        _providerSignInCts?.Cancel();
    }

    [RelayCommand]
    private void DismissSessionNotice()
    {
        SessionEndedNotice = null;
    }

    /// <summary>
    /// Post-sign-in setup (OneSignal, permissions, device sync) is bounded so it
    /// can never hold the sign-in flow hostage; sign-in itself already succeeded.
    /// </summary>
    private async Task RunPostSignInSetupBoundedAsync()
    {
        var setup = _authSessionService.HandleSuccessfulSignInAsync();
        try
        {
            if (await Task.WhenAny(setup, Task.Delay(PostSignInSetupTimeout)) != setup)
            {
                _logger.LogWarning("Post-sign-in setup exceeded {Timeout}s; navigating to home while it finishes in the background.", PostSignInSetupTimeout.TotalSeconds);
                _ = setup.ContinueWith(
                    t => _logger.LogWarning(t.Exception, "Background post-sign-in setup failed."),
                    TaskContinuationOptions.OnlyOnFaulted);
                return;
            }

            await setup;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Post-sign-in setup failed; continuing to home.");
        }
    }

    [RelayCommand]
    private async Task SignInAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ShowError = false;
        ErrorMessage = string.Empty;
        try
        {
            _logger.LogInformation("Attempting sign-in for {Email}", Email);
            var session = await _authService.SignInWithEmailAsync(Email, Password);
            if (!string.IsNullOrWhiteSpace(session.AccessToken))
            {
                await CacheProfileSummaryAsync();
                await RunPostSignInSetupBoundedAsync();
                await Shell.Current.GoToAsync(SignedInTabs.HomeShellPath);
                return;
            }

            _logger.LogWarning("Sign-in failed for {Email}: empty access token.", Email);
            ShowError = true;
            ErrorMessage = "That login did not work. Please try again or create an account.";
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Sign-in failed for {Email}: network error.", Email);
            ShowError = true;
            ErrorMessage = "We could not reach the server. Please check your connection and try again.";
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Sign-in timed out for {Email}.", Email);
            ShowError = true;
            ErrorMessage = "The request timed out. Please try again.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sign-in failed for {Email}.", Email);
            ShowError = true;

            var message = ex.Message ?? string.Empty;
            if (message.Contains("invalid login credentials", StringComparison.OrdinalIgnoreCase))
            {
                ErrorMessage = "That login did not work. Please try again or create an account.";
            }
            else
            {
                ErrorMessage = "Something unexpected happened. Please try again.";
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task NavigateToCreateAccountAsync()
    {
        return _authCoordinator.NavigateToCreateAccountAsync(null, null, null, false);
    }

    [RelayCommand]
    private async Task SignInWithGoogleAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ShowError = false;
        ErrorMessage = string.Empty;
        _providerSignInCts?.Cancel();
        _providerSignInCts = new CancellationTokenSource();
        var cancellation = _providerSignInCts.Token;
        try
        {
            _logger.LogInformation("Starting Google sign-in.");
            var authResult = await _authService.SignInWithGoogleAsync().WaitAsync(cancellation);
            if (string.IsNullOrWhiteSpace(authResult.AccessToken))
            {
                ShowError = true;
                ErrorMessage = "Google sign-in did not complete. Please try again.";
                return;
            }

            var profile = await TryGetProfileAsync();
            if (profile is not null)
            {
                if (!string.IsNullOrWhiteSpace(authResult.AvatarUrl) && string.IsNullOrWhiteSpace(profile.AvatarUrl))
                {
                    _ = _usersApi.UpdateProfileAsync(new UserProfileUpdateRequest
                    {
                        DisplayName = profile.DisplayName,
                        SocialAvatarUrl = authResult.AvatarUrl
                    });
                }

                await CacheProfileSummaryAsync();
                await RunPostSignInSetupBoundedAsync();
                await Shell.Current.GoToAsync(SignedInTabs.HomeShellPath);
                return;
            }

            await TryCreateProfileFromOAuthAsync(authResult);
            await CacheProfileSummaryAsync();
            await RunPostSignInSetupBoundedAsync();
            await Shell.Current.GoToAsync(SignedInTabs.HomeShellPath);
        }
        catch (OperationCanceledException)
        {
            // Abandoned mid-flow (backgrounded native sheet or a fresh attempt
            // superseding this one): silent reset, matching the cancel convention.
            _logger.LogInformation("Google sign-in attempt cancelled or abandoned.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google sign-in failed.");
            ShowError = true;
            ErrorMessage = "Google sign-in failed. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }

#if IOS || MACCATALYST
    [RelayCommand]
    private async Task SignInWithAppleAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        ShowError = false;
        ErrorMessage = string.Empty;
        _providerSignInCts?.Cancel();
        _providerSignInCts = new CancellationTokenSource();
        var cancellation = _providerSignInCts.Token;
        try
        {
            _logger.LogInformation("Starting Apple sign-in.");
            var authResult = await _authService.SignInWithAppleAsync().WaitAsync(cancellation);
            if (string.IsNullOrWhiteSpace(authResult.AccessToken))
            {
                ShowError = true;
                ErrorMessage = "Apple sign-in did not complete. Please try again.";
                return;
            }

            var profile = await TryGetProfileAsync();
            if (profile is not null)
            {
                if (!string.IsNullOrWhiteSpace(authResult.AvatarUrl) && string.IsNullOrWhiteSpace(profile.AvatarUrl))
                {
                    _ = _usersApi.UpdateProfileAsync(new UserProfileUpdateRequest
                    {
                        DisplayName = profile.DisplayName,
                        SocialAvatarUrl = authResult.AvatarUrl
                    });
                }

                await CacheProfileSummaryAsync();
                await RunPostSignInSetupBoundedAsync();
                await Shell.Current.GoToAsync(SignedInTabs.HomeShellPath);
                return;
            }

            await TryCreateProfileFromOAuthAsync(authResult);
            await CacheProfileSummaryAsync();
            await RunPostSignInSetupBoundedAsync();
            await Shell.Current.GoToAsync(SignedInTabs.HomeShellPath);
        }
        catch (OperationCanceledException)
        {
            // Abandoned mid-flow: silent reset, matching the cancel convention.
            _logger.LogInformation("Apple sign-in attempt cancelled or abandoned.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apple sign-in failed.");
            ShowError = true;
            ErrorMessage = "Apple sign-in failed. Please try again.";
        }
        finally
        {
            IsBusy = false;
        }
    }
#endif

    private async Task<bool> TryCreateProfileFromOAuthAsync(OAuthSignInResult authResult)
    {
        if (string.IsNullOrWhiteSpace(authResult.Email))
        {
            return false;
        }

        try
        {
            await _usersApi.UpsertProfileAsync(new UserProfileRequest
            {
                DisplayName = authResult.DisplayName ?? string.Empty,
                Email = authResult.Email,
                Phone = authResult.Phone,
                Password = string.Empty,
                SocialAvatarUrl = authResult.AvatarUrl
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create profile after Google sign-in.");
            ShowError = true;
            ErrorMessage = "We could not finish setting up your account. Please try again.";
            return false;
        }
    }

    private async Task<LoopMeet.App.Features.Auth.Models.UserProfileResponse?> TryGetProfileAsync()
    {
        try
        {
            return await _usersApi.GetProfileAsync();
        }
        catch (ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    private async Task CacheProfileSummaryAsync()
    {
        var profile = await _usersApi.GetProfileSummaryAsync();
        _userProfileCache.SetCachedProfile(profile);
    }
}
