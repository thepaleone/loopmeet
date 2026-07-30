# Research: Reliable Sign-In Sessions & Startup Check

**Feature**: 010-fix-auth-session | **Date**: 2026-07-08
**Sources**: codebase archaeology (file:line references below), Supabase.Gotrue 6.0.3 package surface (`~/.nuget/packages/supabase.gotrue/6.0.3`), Supabase auth documentation.

## 1. Root-cause analysis of the reported bugs

### Bug A — intermittent mid-session / multi-day sign-outs

| # | Cause | Evidence |
| --- | --- | --- |
| A1 | `RestoreSessionAsync` destroys the refresh token when the persisted access token is expired: `if (session is null \|\| session.Expired()) { ... await _client.Auth.SignOut(); }` — instead of refreshing. Any restore > 1 access-token lifetime after the last refresh permanently ends the session. | `src/LoopMeet.App/Features/Auth/AuthService.cs:159-190` |
| A2 | Gotrue auto-refresh (`AutoRefreshToken = true`) is an in-process `System.Threading.Timer` that fires at 4/5 of token lifetime (`TokenRefresh.GetInterval`). Suspended mobile apps don't run timers, so backgrounding past expiry guarantees an expired token on resume; nothing refreshes on foreground. | Gotrue 6.0.3 `TokenRefresh` XML docs; no lifecycle hooks in `App.xaml.cs` |
| A3 | Dual token stores drift: `AuthService` keeps a raw copy at `loopmeet.auth.access_token` (written only on explicit sign-in/restore) while Gotrue persists the full session at `loopmeet.auth.session` (updated on every auto-refresh). `GetAccessToken()` prefers `CurrentSession` but falls back to the possibly-stale raw copy. | `AuthService.cs:15,192-195,209-217`; `MauiSessionPersistence.cs:10` |
| A4 | `ApiAuthHandler` attaches whatever token it gets, expired or not, with no refresh or retry; the 401 then surfaces to screens. | `src/LoopMeet.App/Services/ApiAuthHandler.cs:15-24` |

### Bug B — cannot sign back in until force-quit

| # | Cause | Evidence |
| --- | --- | --- |
| B1 | 401 redirects in Groups/Invitations navigate to `//login` **without** signing out — Gotrue `CurrentSession`, `loopmeet.auth.session`, and `loopmeet.auth.access_token` all survive. The singleton client's stale state then shadows the next sign-in (`GetAccessToken()` returns `CurrentSession?.AccessToken` first). | `GroupsListViewModel.cs:122-134`, `PendingInvitationsViewModel.cs:73-78`, `AuthService.cs:194` |
| B2 | `LoginViewModel` sign-in commands `await _authSessionService.HandleSuccessfulSignInAsync()` (OneSignal init, permission prompts, device sync) *before* navigating, inside the `IsBusy` guard — a hung OneSignal/network call leaves the command busy until the page instance is discarded. | `LoginViewModel.cs` sign-in paths; `AuthSessionService.cs:33-54` |
| B3 | `SignOutAsync` propagates exceptions from `_client.Auth.SignOut()` (a server call): offline sign-out throws, skipping local cleanup steps ordered after it. | `AuthService.cs:50-56`, `ProfileViewModel.cs:93-112` |

### Bug C — login screen flash at launch

| # | Cause | Evidence |
| --- | --- | --- |
| C1 | `LoginPage` is the first `ShellContent`, so Shell renders it immediately on cold launch. | `AppShell.xaml:15-18` |
| C2 | The signed-in decision runs afterward in `async void AppShell.OnAppearing`, and blocks on `RestoreSessionAsync` **plus a profile HTTP round-trip** before `GoToAsync("//home")` — seconds of visible login UI for a signed-in user. | `AppShell.xaml.cs:40-94` |

## 2. Decisions

### D1 — Session source of truth: Gotrue persisted session only

**Decision**: The Gotrue session JSON (`loopmeet.auth.session`, via existing `MauiSessionPersistence`) is the single credential store. Remove `loopmeet.auth.access_token` (`AccessTokenKey`), `_accessToken` fallbacks, `SaveAccessToken`, and `IsJwtExpired`-based fallback restore. One-time cleanup removes the legacy key at startup.
**Rationale**: A3/B1 come directly from two stores drifting. Gotrue already persists every refresh through the session handler; the raw copy can only ever be equal or stale.
**Alternatives considered**: Keeping the raw key synced via `AddStateChangedListener` — rejected: still two stores, more wiring, no benefit; the fallback path (valid raw JWT with no Gotrue session) only masks broken restores.

### D2 — Renewal trigger: refresh on foreground + on-demand, keep the Gotrue timer

**Decision**: Keep `AutoRefreshToken = true` (covers long foreground sessions) and add `SessionCoordinator.EnsureFreshSessionAsync(trigger)` called from the MAUI `Window.Resumed` lifecycle event and from the 401 retry path (D5). It refreshes via `_client.Auth.RefreshToken()` when the session is expired or within its final 1/5 lifetime, matching Gotrue's own refresh margin.
**Rationale**: FR-002 names foregrounding explicitly; the suspended-timer gap (A2) is exactly what this closes. Refreshing slightly early avoids racing requests against expiry.
**Alternatives considered**: Disabling the Gotrue timer and owning all refresh — rejected: loses free coverage for long-lived foreground sessions; more surface to test. Platform background-fetch APIs — rejected: spec assumption explicitly excludes renewal while the app is closed.

### D3 — Failure classification per FR-004a: `GotrueException.Reason`

**Decision**: A pure `SessionFailureClassifier` maps renewal/restore failures: `Reason.ExpiredRefreshToken`, `Reason.InvalidRefreshToken`, `Reason.NoSessionFound`, and HTTP 400/401/403 from the token endpoint → **Definitive** (forced sign-out); `Reason.Offline`, timeouts, `HttpRequestException`, 5xx, and `Reason.Unknown` with transport errors → **Transient** (keep session, retry on next trigger).
**Rationale**: Gotrue 6.0.3 exposes exactly this taxonomy (`FailureHint.Reason` includes `Offline`, `ExpiredRefreshToken`, `InvalidRefreshToken`); classification becomes a deterministic, unit-testable function — the heart of FR-004a.
**Alternatives considered**: Retry-count strike-out — rejected in clarification session (Q1). String-matching exception messages — rejected: brittle; the enum exists.

### D4 — Startup gate: new first Shell route with bounded resolution

**Decision**: Add `StartupGatePage` (ActivityIndicator + "Checking your session…" text) as the first `ShellContent` (route `startup`). Its viewmodel calls `SessionCoordinator.ResolveStartupAsync()`: load persisted session → if valid, go `//home` immediately and revalidate/refresh in background; if expired-with-refresh-token, attempt refresh bounded at **5 s** (clarification Q3) → success `//home`, definitive rejection `//login`, transient/timeout → **`//home` on cached session** per FR-011a (no cached session → `//login`). `AppShell.OnAppearing` loses all session logic; the profile-summary fetch moves to a background task after navigation (it currently blocks the redirect, C2).
**Rationale**: Kills the flash structurally (the wrong screen is never rendered, FR-009) and gives the required status state (FR-008). Bounded wait satisfies SC-005.
**Alternatives considered**: Overlay/loading veil on LoginPage — rejected: login UI still constructed/shown underneath; fragile. Deciding before `MainPage` assignment in `App.CreateWindow` — rejected: async work before first window violates MAUI startup expectations and shows a blank screen with no status text (fails FR-008).

### D5 — Centralized session-end detection: refresh-and-retry in `ApiAuthHandler`

**Decision**: `ApiAuthHandler` becomes the single 401 authority: on a 401 response it asks the coordinator for one refresh (`EnsureFreshSessionAsync(ApiUnauthorized)`), retries the request once with the new token, and — only if refresh is **definitively** rejected — triggers `SessionCoordinator.SignOutAsync(SessionEnded)`. Screens' ad-hoc 401 handling is removed: Groups/Invitations redirects deleted; Home/Profile stop masking (FR-005) since the handler now guarantees routing.
**Rationale**: Every Refit client already flows through this handler (`ApiClient.AddLoopMeetApi<T>`), so it is the one choke point that makes FR-003 true by construction rather than by convention.
**Alternatives considered**: Shared base viewmodel with `HandleApiException` — rejected: still per-screen opt-in, exactly the pattern that caused the inconsistency. Polly retry policies — rejected: new dependency for one bounded retry (Constitution IV).

### D6 — One sign-out path, resilient offline

**Decision**: `SessionCoordinator.SignOutAsync(SignOutReason)` is the only sign-out entry point (ProfileViewModel logout and forced sign-out both call it). Order: (1) clear local Gotrue session state + persisted session (always, wrapped so a failed server revoke cannot skip it), (2) clear `UserProfileCache`, (3) OneSignal `Logout()` (currently never called — device stays linked to the prior user), (4) attempt server-side revoke best-effort, (5) navigate `//login`, passing the reason so LoginPage can show the session-ended notice (including the unsaved-input message per clarification Q4).
**Rationale**: FR-004 requires full clearing on *every* end-of-session; B3 shows the current path can throw before clearing anything; OneSignal identity is personal data that currently survives sign-out (Privacy & Safety section of spec).
**Alternatives considered**: Keeping `AuthService.SignOutAsync` as the entry point — rejected: navigation and notice presentation don't belong in `AuthService`, and the coordinator already owns lifecycle transitions.

### D7 — Session ceiling & sliding expiration: Supabase refresh-token semantics, no client-side timer math

**Decision**: Rely on Supabase's refresh-token model for FR-001: refresh tokens are single-use, rotated on every refresh, and do not expire by default — each renewal issues a fresh pair, giving sliding expiration for free. Deliverable includes a documented **operator check** (quickstart) that the Supabase project has session time-boxing and inactivity-timeout disabled (both off by default).
**Rationale**: Matches the spec assumption ("maximum the provider supports, extended via sliding renewal"); no numeric ceiling exists to encode client-side.
**Alternatives considered**: Raising the access-token JWT expiry server-side — rejected: unnecessary once renewal works, and longer-lived access tokens weaken revocation.

### D8 — Debounce for rapid foreground/background cycles

**Decision**: `SessionRenewalPolicy` (pure class) gates `EnsureFreshSessionAsync`: skip if a renewal is already in flight (single-flight via shared `Task`) or if the last successful check was < 30 s ago and the token isn't in its final 1/5 lifetime.
**Rationale**: Spec edge case (rapid switching must not stack renewals or conflicting navigation); single-flight also serializes the 401-retry path against the foreground path.
**Alternatives considered**: No debounce (rely on Gotrue idempotence) — rejected: refresh tokens are single-use; concurrent refreshes can race and one loser gets `InvalidRefreshToken` — a false definitive rejection that would force sign-out. This makes single-flight *correctness*, not just efficiency.

### D9 — Clock skew

**Decision**: No special handling beyond D2/D5: expiry checks use Gotrue's `Session.Expired()` for *proactive* refresh only; the *authoritative* signal is the server's 401 → refresh-and-retry path, which works regardless of device clock. A wildly wrong clock at worst causes an extra refresh round-trip.
**Rationale**: Server-validated behavior satisfies "behave sensibly" (spec edge case) with zero added complexity.
**Alternatives considered**: NTP/server-time offset tracking — rejected: complexity without a failing scenario once 401-retry exists.

## 3. Test surface (Constitution II)

- **Unit-testable (deterministic, no MAUI/Gotrue host)**: `SessionFailureClassifier` (D3), `SessionRenewalPolicy` (D8), `ApiAuthHandler` refresh-retry behavior via `HttpMessageHandler` fake + `ISessionTokenSource` fake (D5), `SignOutReason` → notice-text mapping (D6).
- **Source-surface tests** (established repo pattern, cf. `AppleSignInCommandSurfaceTests`): startup gate is first ShellContent, `AppShell.OnAppearing` no longer calls `RestoreSessionAsync`, legacy `AccessTokenKey` removed, OneSignal logout present in sign-out path.
- **Manual device matrix** ([quickstart.md](./quickstart.md)): backgrounding past token expiry, multi-day usage, offline launch, flash checks, per-screen forced sign-out, immediate re-sign-in with all three providers, rapid foreground cycling, unsaved-input notice.
