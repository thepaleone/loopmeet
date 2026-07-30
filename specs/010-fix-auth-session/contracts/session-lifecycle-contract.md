# Contract: Session Lifecycle (010-fix-auth-session)

**Date**: 2026-07-08. Interfaces are defined before implementation per Constitution VI. All types live in `LoopMeet.App.Features.Auth.Session` unless noted.

## 1. `SessionCoordinator` (new, singleton)

```csharp
public sealed class SessionCoordinator
{
    /// Resolves the startup destination within the 5s bound (FR-008/FR-009/FR-011a).
    /// Never throws; always returns a definite destination.
    public Task<StartupResolution> ResolveStartupAsync(CancellationToken ct = default);

    /// Ensures the session is fresh (refreshing if expired or in its final 1/5 lifetime).
    /// Single-flight + debounced per SessionRenewalPolicy. Transient failures keep the
    /// session (FR-004a). Returns the outcome; performs the forced sign-out itself on
    /// definitive rejection.
    public Task<RenewalOutcome> EnsureFreshSessionAsync(RenewalTrigger trigger);

    /// The ONLY sign-out entry point (INV-2). Steps 1-3 of the clearing checklist are
    /// unconditional; never throws.
    public Task SignOutAsync(SignOutReason reason);
}

public enum RenewalTrigger { AppForegrounded, ApiUnauthorized, StartupRevalidation }
public enum RenewalOutcome { StillValid, Renewed, TransientFailureKeptSession, DefinitivelyRejectedSignedOut, Skipped }
public sealed record StartupResolution(string Route /* "//home" | "//login" */, SignOutReason? Notice);
```

**Behavioral contract**:

- `ResolveStartupAsync`: persisted session valid → `//home` (revalidate in background via `EnsureFreshSessionAsync(StartupRevalidation)`); expired + refresh token → one refresh attempt bounded to 5 s total; on success → `//home`; on definitive rejection → full sign-out, `//login` with `SessionRejected` notice; on transient failure/timeout with a cached session → `//home` optimistically (FR-011a); no session → `//login`, no notice.
- `EnsureFreshSessionAsync` MUST be safe to call concurrently from any thread; callers awaiting a skipped/deduplicated call receive the in-flight call's outcome.
- `SignOutAsync` MUST complete steps 1–3 (Gotrue local session, `UserProfileCache`, OneSignal `Logout()`) even when the server revoke (step 4) throws, MUST record the reason in `SessionNoticeState` (§6a) before navigating, and MUST emit one structured log event with the reason.
- **Thread affinity**: `SignOutAsync` and `EnsureFreshSessionAsync` may be invoked from background threads (`ApiAuthHandler`, `Window.Resumed`). All Shell navigation and any `Shell.Current`/`CurrentPage` access (including the `IHasUnsavedInput` inspection) MUST be dispatched via `MainThread.InvokeOnMainThreadAsync`. Non-UI work (token refresh, preference clearing) stays off the main thread.

## 2. `ISessionTokenSource` (new — seam for ApiAuthHandler testability)

```csharp
public interface ISessionTokenSource
{
    string? GetAccessToken();                       // current Gotrue session token only; no fallbacks
    Task<RenewalOutcome> RefreshForRetryAsync();    // delegates to EnsureFreshSessionAsync(ApiUnauthorized)
}
```

Implemented by `SessionCoordinator`. Consumers: `ApiAuthHandler` (production), test fakes.

## 3. `SessionFailureClassifier` (new, pure static)

```csharp
public enum SessionFailureKind { Definitive, Transient }
public static SessionFailureKind Classify(Exception ex);
```

| Input | Kind |
| --- | --- |
| `GotrueException.Reason` ∈ { `ExpiredRefreshToken`, `InvalidRefreshToken`, `NoSessionFound` } | `Definitive` |
| `GotrueException` with token-endpoint HTTP 400/401/403 | `Definitive` |
| `GotrueException.Reason` = `Offline` | `Transient` |
| `HttpRequestException`, `TaskCanceledException`/timeout, HTTP 5xx, `Reason.Unknown` with transport error | `Transient` |
| Anything else | `Transient` (fail-safe: never force-sign-out on ambiguity, per FR-004a) |

## 4. `SessionRenewalPolicy` (new, pure)

```csharp
public sealed class SessionRenewalPolicy
{
    /// True when: no successful check within the last 30s debounce window,
    /// OR token is expired / within its final 1/5 lifetime.
    public bool ShouldAttempt(DateTimeOffset nowUtc, DateTimeOffset? lastSuccessUtc,
                              DateTimeOffset? tokenExpiryUtc);
}
```

Deterministic — all time injected; no `DateTime.Now` inside.

## 5. `ApiAuthHandler` (modified) — 401 contract

```text
send(request):
  attach bearer from ISessionTokenSource.GetAccessToken() if present
  response = inner.Send(request)
  if response is 401 AND request not already retried:
      outcome = await RefreshForRetryAsync()
      if outcome in { Renewed, StillValid }:
          re-clone request, attach new token, send once more; return that response
      # DefinitivelyRejectedSignedOut: coordinator already performed sign-out + navigation
  return response
```

- Exactly one retry per request (marker via `HttpRequestOptions`).
- Handler itself never navigates and never clears state — it only consults the token source (single responsibility; INV-2 stays with the coordinator).
- `HttpRequestMessage` cannot be re-sent: the retry MUST build a genuine clone (copy method, URI, headers, options; buffer original content bytes + content headers into a new `ByteArrayContent`). Existing Refit usage (JSON `StringContent`) is safely bufferable.

## 6. Startup gate (new UI contract)

- `AppShell.xaml`: first `ShellContent` becomes `Route="startup"` → `StartupGatePage`; `login` remains a registered route. `AppShell.OnAppearing` contains **no session logic**.
- `StartupGatePage`: full-screen, brand background, centered `ActivityIndicator` + `Label` "Checking your session…" (FR-008). No buttons, no back navigation.
- `StartupGateViewModel`: on appearing, `var r = await coordinator.ResolveStartupAsync(); await Shell.Current.GoToAsync(r.Route)` — exactly one navigation per launch (FR-009/SC-004). The profile-summary prefetch currently in `AppShell.OnAppearing` moves to a fire-and-forget task after `//home` navigation.
- `LoginViewModel` (modified): exposes `SessionEndedNotice` (nullable string) populated on appearing via `SignOutNotices.For(SessionNoticeState.TakePending())` (§6a); rendered as a dismissible banner on LoginPage.

## 6a. `SessionNoticeState` (new, singleton — notice hand-off channel)

```csharp
public sealed class SessionNoticeState
{
    /// Set by SessionCoordinator (SignOutAsync / ResolveStartupAsync) before navigating to //login.
    public SignOutReason? Pending { get; set; }

    /// Consume-once read: returns Pending and clears it, so the banner
    /// shows exactly once and never resurfaces on later visits to the login screen.
    public SignOutReason? TakePending();
}
```

Registered as Singleton in `MauiProgram.cs`. This is the *only* channel for session-ended notices — no Shell query parameters, no static fields.

## 7. Screen de-authorization cleanup (modified)

Removed per FR-003 (the handler + coordinator now own all of it):

| File | Removal |
| --- | --- |
| `GroupsListViewModel.cs` | 401 branch redirecting to `//login` |
| `PendingInvitationsViewModel.cs` | 401 branch redirecting to `//login` |
| `HomeViewModel.cs` | bare `catch {}` that masks 401 as empty state (FR-005) |
| `ProfileViewModel.cs` | swallow-all catch for the same reason; logout now calls `SessionCoordinator.SignOutAsync(UserInitiated)` |

## 8. App lifecycle wiring (modified)

`App.xaml.cs` / window creation: subscribe `window.Resumed += (_,_) => _ = coordinator.EnsureFreshSessionAsync(RenewalTrigger.AppForegrounded);` — fire-and-forget by design (UI must not block on resume); errors handled inside the coordinator.

## 9. Versioning / migration notes

- One-time startup migration: delete legacy `Preferences["loopmeet.auth.access_token"]`. Existing signed-in users keep their `loopmeet.auth.session` and are unaffected.
- `AuthService` public surface shrinks: `GetAccessToken` reads the Gotrue session only; `SignOutAsync` reduces to the local Gotrue-clear helper the coordinator calls (checklist step 1); `RestoreSessionAsync` is fixed in place during US1 (interim, still called from `AppShell.OnAppearing`) and **deleted in US4** once `ResolveStartupAsync` absorbs it — no dead code ships (Constitution I). All callers are in-repo; no external consumers.
