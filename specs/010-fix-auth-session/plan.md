# Implementation Plan: Reliable Sign-In Sessions & Startup Check

**Branch**: `010-fix-auth-session` | **Date**: 2026-07-08 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/010-fix-auth-session/spec.md`

## Summary

Users intermittently lose their signed-in state, cannot sign back in without force-quitting, and see a login-screen flash on launch. Root causes identified in the current client (see [research.md](./research.md) §1):

1. `AuthService.RestoreSessionAsync` **destroys the refresh token** (`_client.Auth.SignOut()`) whenever the persisted session's access token is expired, instead of refreshing it — so any launch/restore more than ~1 access-token lifetime after the last refresh permanently ends the session.
2. Gotrue auto-refresh is an **in-process timer** (fires at 4/5 of token lifetime) that cannot fire while the app is suspended; nothing refreshes the token on app foregrounding.
3. **Two divergent token stores** (`loopmeet.auth.access_token` raw copy vs. Gotrue's `loopmeet.auth.session` JSON) drift apart, so a stale token can shadow a fresh one.
4. **Per-screen 401 handling is inconsistent** — Groups/Invitations redirect to `//login` *without signing out* (leaving stale state that breaks re-sign-in), while Home/Profile silently mask the condition with empty states.
5. **LoginPage is the first `ShellContent`**, so it paints on every cold launch before the async session check in `AppShell.OnAppearing` (which also blocks on a profile HTTP call) redirects to home — the flash.

Technical approach: introduce a single `SessionCoordinator` (new `Features/Auth/Session/` module) that owns session lifecycle end-to-end — bounded startup resolution behind a new startup-gate page (first Shell route), refresh-on-foreground via the MAUI window lifecycle, definitive-vs-transient failure classification per FR-004a (`GotrueException.Reason`), one-and-only-one full sign-out path (Gotrue + preferences + profile cache + OneSignal identity), and centralized 401 handling (refresh-and-retry once) inside the existing `ApiAuthHandler`. The Gotrue-persisted session becomes the single token source of truth; the raw access-token preference is removed.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (MAUI client). No backend or Supabase schema changes.
**Primary Dependencies**: Microsoft.Maui.Controls 10.0.70, Supabase 1.1.1 (Supabase.Gotrue 6.0.3: `TokenRefresh`, `GotrueException.Reason`, `Client.RefreshToken()`, `AddStateChangedListener`), CommunityToolkit.Mvvm 8.4.0, Refit.HttpClientFactory 10.1.6, OneSignalSDK.DotNet 6.1.8.
**Storage**: `Preferences.Default` — Gotrue session JSON under `loopmeet.auth.session` (via existing `MauiSessionPersistence`) becomes the *only* credential store; the legacy `loopmeet.auth.access_token` key is removed (with one-time cleanup). `UserProfileCache` (`loopmeet.profile.cache`) unchanged in shape, cleared on sign-out.
**Testing**: xUnit in `tests/LoopMeet.App.Tests` — pure-logic unit tests for failure classification, expiry/refresh decisions, and debounce; source-surface tests (established repo pattern) for platform-coupled wiring; manual device matrix in [quickstart.md](./quickstart.md).
**Target Platform**: iOS, MacCatalyst, Android, Windows (all four; behavior must be uniform per FR-010).
**Project Type**: Mobile app (existing .NET MAUI client, feature-folder structure).
**Performance Goals**: Startup session check resolves to a definite screen in ≤ 5 s, 100% of launches (SC-005); zero flash of the wrong screen (SC-004).
**Constraints**: Offline-capable — network absence must never be treated as an invalid session (FR-004a, FR-011a); no fixed session ceiling (FR-001, Supabase refresh-token sliding renewal); rapid foreground/background cycles must not stack renewal attempts (spec edge case).
**Scale/Scope**: 1 new module (~4 small classes), 1 new Shell page, modifications to `AuthService`, `ApiAuthHandler`, `AppShell`, `App`, 4 viewmodels (Home, Profile, GroupsList, PendingInvitations), `ProfileViewModel.LogoutAsync`; ~8–10 unit tests.

## Constitution Check

*GATE: evaluated against Meetloop Constitution v0.1.0 — before Phase 0 and re-checked after Phase 1.*

| Gate | Principle | Status | Notes |
| ------ | ----------- | -------- | ------- |
| G1 | I. Code Quality | PASS | Removes dead/duplicated logic (dual token stores, per-screen 401 branches) rather than adding parallel paths. |
| G2 | II. Tests Required | PASS | Classification/expiry/debounce logic extracted into pure, deterministic classes with unit tests; regression tests cover the restore-destroys-refresh-token bug. Native lifecycle + navigation validated via quickstart matrix (documented limitation, same as 009). |
| G3 | III. UX First | PASS | Spec defines 4 prioritized user stories with acceptance scenarios; error messages specified ("session ended" notice); startup loading state replaces flash. |
| G4 | IV. Simplicity | PASS | One coordinator replaces five scattered decision points; no new packages; no speculative abstraction — the only new interface (`ISessionTokenSource`) exists to make `ApiAuthHandler` unit-testable (two real consumers: production coordinator, test fake). |
| G5 | V. Modularity | PASS | New `Features/Auth/Session/` module with a single responsibility (session lifecycle); screens lose their ad-hoc auth logic instead of gaining more. No circular dependencies (coordinator → Gotrue client; handler → token source interface). |
| G6 | VI. Contract-First | PASS | [contracts/session-lifecycle-contract.md](./contracts/session-lifecycle-contract.md) defines the coordinator API, failure-classification table, startup-gate contract, and sign-out clearing checklist before implementation. |
| G7 | VII. Observability | PASS | Coordinator emits structured `ILogger` events for every lifecycle transition (restored, renewed, transient-failure-kept, definitive-rejection, signed-out + reason) — currently these paths log nothing or `Debug.WriteLine`. |

**Post-Phase-1 re-check (2026-07-08)**: All gates still PASS. Design added no projects, no packages, one interface with two consumers. No Complexity Tracking entries required.

## Project Structure

### Documentation (this feature)

```text
specs/010-fix-auth-session/
├── plan.md              # This file
├── research.md          # Phase 0 output — root causes + 9 decisions
├── data-model.md        # Phase 1 output — session state machine, sign-in attempt
├── quickstart.md        # Phase 1 output — device validation matrix
├── contracts/
│   └── session-lifecycle-contract.md   # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/LoopMeet.App/
├── App.xaml.cs                                  # MODIFIED: hook Window.Resumed → coordinator renewal
├── AppShell.xaml                                # MODIFIED: startup-gate page becomes first ShellContent
├── AppShell.xaml.cs                             # MODIFIED: OnAppearing session logic removed (moves to gate page)
├── MauiProgram.cs                               # MODIFIED: register SessionCoordinator, gate page/VM
├── Features/Auth/
│   ├── AuthService.cs                           # MODIFIED: fix RestoreSessionAsync (refresh, don't destroy);
│   │                                            #   remove raw-token store; token reads from Gotrue session only
│   ├── MauiSessionPersistence.cs                # unchanged (already correct)
│   ├── Session/                                 # NEW module — session lifecycle
│   │   ├── SessionCoordinator.cs                # NEW: startup resolve, foreground renewal, full sign-out
│   │   ├── SessionFailureClassifier.cs          # NEW: pure — GotrueException/HTTP → Definitive | Transient
│   │   ├── SessionRenewalPolicy.cs              # NEW: pure — needs-refresh + debounce decisions
│   │   ├── SignOutReason.cs                     # NEW: enum + session-ended notice text mapping
│   │   ├── SessionNoticeState.cs                # NEW: singleton consume-once notice hand-off (contract §6a)
│   │   └── IHasUnsavedInput.cs                  # NEW: marker for unsaved-input notice selection
│   └── Views/ (+ ViewModels/)
│       ├── StartupGatePage.xaml(.cs)            # NEW: indicator + "Checking your session…" status text
│       ├── StartupGateViewModel.cs              # NEW: drives ResolveStartupAsync, navigates once
│       └── LoginViewModel.cs                    # MODIFIED: show session-ended notice when applicable
├── Services/
│   └── ApiAuthHandler.cs                        # MODIFIED: on 401 → refresh+retry once → else forced sign-out
└── Features/{Home,Profile,Groups,Invitations}/ViewModels/
    └── (4 viewmodels)                           # MODIFIED: remove ad-hoc 401 redirects / empty-state masking

tests/LoopMeet.App.Tests/
└── Features/Auth/Session/
    ├── SessionFailureClassifierTests.cs         # NEW
    ├── SessionRenewalPolicyTests.cs             # NEW
    ├── ApiAuthHandlerRetryTests.cs              # NEW (HttpMessageHandler fake + ISessionTokenSource fake)
    └── SessionSurfaceTests.cs                   # NEW (source-surface: wiring assertions, repo pattern)
```

**Structure Decision**: Existing single-project MAUI feature-folder layout. Session lifecycle logic gets its own cohesive submodule `Features/Auth/Session/` per Constitution V; screens and handlers consume it through small explicit surfaces (`SessionCoordinator`, `ISessionTokenSource`).

## Complexity Tracking

No constitution violations — table intentionally empty.
