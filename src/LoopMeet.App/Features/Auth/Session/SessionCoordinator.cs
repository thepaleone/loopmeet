using LoopMeet.App.Features.Home.Models;
using LoopMeet.App.Services;
using LoopMeet.App.Services.Notifications;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
// Inside the ...Auth.Session namespace the plain name "Session" binds to the
// namespace itself, so the Gotrue type needs an alias.
using GotrueSession = Supabase.Gotrue.Session;
using SupabaseClient = Supabase.Client;

namespace LoopMeet.App.Features.Auth.Session;

/// <summary>
/// Single owner of the session lifecycle (contract §1): bounded startup
/// resolution, renewal on active use, and the one-and-only sign-out path
/// (INV-2). The Gotrue-persisted session is the sole credential store.
/// </summary>
public sealed class SessionCoordinator : ISessionTokenSource
{
    private const string LoginRoute = "//login";
    private const string LegacyAccessTokenKey = "loopmeet.auth.access_token";
    private static readonly TimeSpan StartupBudget = TimeSpan.FromSeconds(5);

    private readonly SupabaseClient _client;
    private readonly MauiSessionPersistence _sessionPersistence;
    private readonly UserProfileCache _userProfileCache;
    private readonly SessionNoticeState _noticeState;
    private readonly OneSignalIdentityService _oneSignalIdentityService;
    private readonly ILogger<SessionCoordinator> _logger;
    private readonly SessionRenewalPolicy _renewalPolicy = new();
    private readonly object _renewalGate = new();
    private Task<RenewalOutcome>? _renewalInFlight;
    private DateTimeOffset? _lastSuccessfulCheckUtc;

    // Holds the persisted session on an offline launch (FR-011a) where the
    // Gotrue client could not restore it. Cleared on the first successful
    // renewal; never a second persisted store.
    private GotrueSession? _offlineFallbackSession;

    public SessionCoordinator(
        SupabaseClient client,
        MauiSessionPersistence sessionPersistence,
        UserProfileCache userProfileCache,
        SessionNoticeState noticeState,
        OneSignalIdentityService oneSignalIdentityService,
        ILogger<SessionCoordinator> logger)
    {
        _client = client;
        _sessionPersistence = sessionPersistence;
        _userProfileCache = userProfileCache;
        _noticeState = noticeState;
        _oneSignalIdentityService = oneSignalIdentityService;
        _logger = logger;

        // One-time migration: the raw access-token copy is gone (D1); the
        // Gotrue session JSON is the only credential store.
        Preferences.Default.Remove(LegacyAccessTokenKey);
    }

    public string? GetAccessToken() =>
        _client.Auth.CurrentSession?.AccessToken ?? _offlineFallbackSession?.AccessToken;

    public Task<RenewalOutcome> RefreshForRetryAsync() =>
        EnsureFreshSessionAsync(RenewalTrigger.ApiUnauthorized);

    public Task<RenewalOutcome> EnsureFreshSessionAsync(RenewalTrigger trigger)
    {
        lock (_renewalGate)
        {
            if (_renewalInFlight is not null)
            {
                return _renewalInFlight;
            }

            var session = _client.Auth.CurrentSession ?? _offlineFallbackSession;
            if (string.IsNullOrWhiteSpace(session?.RefreshToken))
            {
                _logger.LogInformation("Renewal skipped ({Trigger}): no session to renew.", trigger);
                return Task.FromResult(RenewalOutcome.Skipped);
            }

            // A server 401 is authoritative (D9) — bypass the debounce for it.
            if (trigger != RenewalTrigger.ApiUnauthorized
                && !_renewalPolicy.ShouldAttempt(DateTimeOffset.UtcNow, _lastSuccessfulCheckUtc, ExpiryUtc(session)))
            {
                return Task.FromResult(RenewalOutcome.StillValid);
            }

            _renewalInFlight = RenewSessionAsync(trigger);
            return _renewalInFlight;
        }
    }

    public async Task SignOutAsync(SignOutReason reason)
    {
        await SignOutCoreAsync(reason);
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (Shell.Current is not null)
            {
                await Shell.Current.GoToAsync(LoginRoute);
            }
        });
    }

    public async Task<StartupResolution> ResolveStartupAsync(CancellationToken cancellationToken = default)
    {
        var startedUtc = DateTimeOffset.UtcNow;
        // Snapshot before InitializeAsync: a transient refresh failure inside
        // Gotrue's own restore must not cost us the persisted refresh token.
        var snapshot = _sessionPersistence.LoadSession();

        // Supabase.Client.InitializeAsync only calls RetrieveSessionAsync, which
        // no-ops when CurrentSession is null — the persisted session is adopted
        // only by this explicit call. Without it every cold start runs tokenless.
        _client.Auth.LoadSession();

        Exception? initializeFailure = null;
        try
        {
            var initialize = _client.InitializeAsync();
            if (await Task.WhenAny(initialize, Task.Delay(StartupBudget, cancellationToken)) == initialize)
            {
                await initialize;
            }
            else
            {
                _logger.LogWarning("Startup session check exceeded its {Budget}s budget; taking the offline path.", StartupBudget.TotalSeconds);
            }
        }
        catch (Exception ex)
        {
            initializeFailure = ex;
        }

        var session = _client.Auth.CurrentSession;
        if (session is not null && !session.Expired())
        {
            _logger.LogInformation("SessionRestored: startup resolved to home.");
            _ = EnsureFreshSessionAsync(RenewalTrigger.StartupRevalidation);
            return new StartupResolution(SignedInTabs.HomeShellPath, null);
        }

        if (snapshot is null)
        {
            _logger.LogInformation("Startup resolved to login: no persisted session.");
            return new StartupResolution(LoginRoute, null);
        }

        if (initializeFailure is not null
            && SessionFailureClassifier.Classify(initializeFailure) == SessionFailureKind.Definitive)
        {
            _logger.LogInformation("RenewalRejected: startup restore definitively rejected.");
            await SignOutCoreAsync(SignOutReason.SessionRejected);
            return new StartupResolution(LoginRoute, SignOutReason.SessionRejected);
        }

        // Gotrue's RetrieveSessionAsync destroys expired or unrefreshable
        // sessions (swallowing the error), so restore the snapshot and make one
        // bounded, authoritative renewal attempt of our own.
        _sessionPersistence.SaveSession(snapshot);
        _offlineFallbackSession = snapshot;

        var remaining = StartupBudget - (DateTimeOffset.UtcNow - startedUtc);
        var outcome = await RenewWithinAsync(remaining, cancellationToken);
        switch (outcome)
        {
            case RenewalOutcome.Renewed:
                return new StartupResolution(SignedInTabs.HomeShellPath, null);
            case RenewalOutcome.DefinitivelyRejectedSignedOut:
                return new StartupResolution(LoginRoute, SignOutReason.SessionRejected);
            default:
                // Transient: cached session, server unreachable → home (FR-011a);
                // the first successful renewal re-adopts it into the client.
                _logger.LogWarning("Startup could not validate the cached session; resolving to home unvalidated (FR-011a).");
                return new StartupResolution(SignedInTabs.HomeShellPath, null);
        }
    }

    private async Task<RenewalOutcome> RenewWithinAsync(TimeSpan budget, CancellationToken cancellationToken)
    {
        var renewal = EnsureFreshSessionAsync(RenewalTrigger.StartupRevalidation);
        if (budget > TimeSpan.Zero
            && await Task.WhenAny(renewal, Task.Delay(budget, cancellationToken)) == renewal)
        {
            return await renewal;
        }

        // Renewal still running past the budget: resolve now, let it finish in
        // the background (single-flight makes a later trigger await the same task).
        return RenewalOutcome.TransientFailureKeptSession;
    }

    private async Task<RenewalOutcome> RenewSessionAsync(RenewalTrigger trigger)
    {
        // Captured up front: SetSession destroys the persisted session before it
        // refreshes, so a failure would otherwise lose the stored refresh token.
        var session = _client.Auth.CurrentSession ?? _offlineFallbackSession;
        try
        {
            if (session is not { AccessToken: not null, RefreshToken: not null })
            {
                _logger.LogInformation("Renewal skipped ({Trigger}): session disappeared before the attempt.", trigger);
                return RenewalOutcome.Skipped;
            }

            // The forced SetSession is the one renewal that works regardless of
            // access-token expiry; Gotrue's parameterless RefreshToken() throws a
            // false-definitive ExpiredRefreshToken for an expired access token.
            await _client.Auth.SetSession(session.AccessToken, session.RefreshToken, forceAccessTokenRefresh: true);

            _offlineFallbackSession = null;
            _lastSuccessfulCheckUtc = DateTimeOffset.UtcNow;
            _logger.LogInformation("SessionRenewed ({Trigger}).", trigger);
            return RenewalOutcome.Renewed;
        }
        catch (Exception ex)
        {
            if (SessionFailureClassifier.Classify(ex) == SessionFailureKind.Transient)
            {
                // Undo SetSession's eager local destroy: keep the credentials
                // durable and in reach of the next attempt (FR-004a).
                _sessionPersistence.SaveSession(session!);
                _offlineFallbackSession = session;
                _logger.LogWarning(ex, "RenewalTransientFailure ({Trigger}): session kept.", trigger);
                return RenewalOutcome.TransientFailureKeptSession;
            }

            _logger.LogError(ex, "RenewalRejected ({Trigger}): signing out.", trigger);
            await SignOutAsync(SignOutReason.SessionRejected);
            return RenewalOutcome.DefinitivelyRejectedSignedOut;
        }
        finally
        {
            lock (_renewalGate)
            {
                _renewalInFlight = null;
            }
        }
    }

    /// <summary>
    /// The clearing checklist (data-model §3). Local clearing is unconditional:
    /// a failed server revoke can never leave credentials behind.
    /// </summary>
    private async Task SignOutCoreAsync(SignOutReason reason)
    {
        var effectiveReason = reason;
        if (reason == SignOutReason.SessionRejected)
        {
            effectiveReason = await MainThread.InvokeOnMainThreadAsync(() =>
                Shell.Current?.CurrentPage?.BindingContext is IHasUnsavedInput { HasUnsavedInput: true }
                    ? SignOutReason.SessionRejectedWithUnsavedInput
                    : reason);
        }

        try
        {
            // Best-effort server revoke; also clears the client's session on success.
            await _client.Auth.SignOut();
        }
        catch (Exception ex)
        {
            // The in-memory Gotrue session may survive here; the persisted store
            // is destroyed below and any fresh sign-in replaces it.
            _logger.LogWarning(ex, "Server-side sign-out revoke failed; local clearing proceeds.");
        }
        finally
        {
            _sessionPersistence.DestroySession();
        }

        _offlineFallbackSession = null;
        _lastSuccessfulCheckUtc = null;
        _userProfileCache.Clear();

        try
        {
            await _oneSignalIdentityService.LogoutAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OneSignal identity logout failed during sign-out.");
        }

        _noticeState.Pending = effectiveReason == SignOutReason.UserInitiated ? null : effectiveReason;
        _logger.LogInformation("SignedOut {Reason}.", effectiveReason);
    }

    private static DateTimeOffset? ExpiryUtc(GotrueSession session)
    {
        var expiresAt = session.ExpiresAt();
        return expiresAt == default ? null : new DateTimeOffset(expiresAt, TimeSpan.Zero);
    }
}
