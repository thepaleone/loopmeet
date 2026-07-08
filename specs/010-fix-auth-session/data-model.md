# Data Model: Reliable Sign-In Sessions & Startup Check

**Feature**: 010-fix-auth-session | **Date**: 2026-07-08

No server-side or Supabase schema changes. All entities below are client-side state, mostly in-memory; the only persisted artifact is the Gotrue session JSON (existing mechanism).

## 1. User Session (client-side state machine)

The session's canonical representation is Supabase.Gotrue `Session` (access token, refresh token, expiry), persisted as JSON under `Preferences["loopmeet.auth.session"]` by the existing `MauiSessionPersistence`. The legacy raw copy `Preferences["loopmeet.auth.access_token"]` is **removed** (one-time cleanup on first launch after update).

### States

| State | Meaning | Token usable? |
| --- | --- | --- |
| `Validated` | Session confirmed against the server this launch (restore, refresh, or successful API call). | Yes |
| `CachedUnvalidated` | Persisted session loaded but server unreachable (offline launch per FR-011a); revalidation pending. | Yes (optimistic) |
| `RefreshPending` | A renewal is in flight (single-flight; concurrent triggers await the same task). | Yes (old token until replaced) |
| `Ended(reason)` | No session. Terminal until a new sign-in. | No |

### Transitions

| From | Trigger | To | Notes |
| --- | --- | --- | --- |
| (launch) | Persisted session valid | `Validated` | Navigate `//home` |
| (launch) | Persisted session expired → refresh succeeds ≤ 5 s | `Validated` | Navigate `//home` |
| (launch) | Refresh transient-fails / times out, cached session exists | `CachedUnvalidated` | Navigate `//home`; background revalidation (FR-011a) |
| (launch) | No persisted session, or refresh definitively rejected | `Ended(NoSession \| Rejected)` | Navigate `//login` |
| `Validated`/`CachedUnvalidated` | Foreground resume, token in final 1/5 lifetime or expired | `RefreshPending` | Debounced (30 s window) |
| `RefreshPending` | Refresh succeeds | `Validated` | Gotrue rotates refresh token; persistence handler saves |
| `RefreshPending` | Transient failure (`Offline`, timeout, 5xx) | previous state | Session kept (FR-004a); retry on next trigger |
| `RefreshPending` | Definitive rejection (`ExpiredRefreshToken`, `InvalidRefreshToken`, 400/401/403 from token endpoint) | `Ended(Rejected)` | Full sign-out (FR-004), notice shown |
| any signed-in | API 401 → one refresh+retry fails definitively | `Ended(Rejected)` | Via `ApiAuthHandler` (FR-003/FR-005) |
| any signed-in | User taps Log Out | `Ended(UserInitiated)` | Same full-clear path, no notice |

### Invariants

- **INV-1**: At most one renewal attempt in flight at any moment (single-flight) — required because Supabase refresh tokens are single-use; a raced second refresh yields a false `InvalidRefreshToken`.
- **INV-2**: `Ended` is reached **only** through `SessionCoordinator.SignOutAsync(reason)`; no other code clears credentials or navigates to `//login` for session reasons.
- **INV-3**: Transient failures never transition to `Ended` (FR-004a).
- **INV-4**: Every transition emits a structured log event with the trigger and (for `Ended`) the reason.

## 2. Sign-In Attempt

Represents one attempt via email/password, Google, or Apple (existing `LoginViewModel` commands). Changes:

| Field/behavior | Current | New |
| --- | --- | --- |
| Busy guard | `IsBusy` held across post-sign-in setup (OneSignal init etc.) — a hang blocks all future attempts (bug B2) | Post-sign-in setup (`AuthSessionService.HandleSuccessfulSignInAsync`) is bounded by a timeout and cannot hold the sign-in command hostage; navigation proceeds independently (FR-007) |
| Abandoned attempt | Interrupted OAuth/Apple flow can leave `IsBusy` stuck for the page's lifetime | Cancel/interrupt always resets to idle in `finally`; new attempts never blocked by a prior one (FR-007) |
| Precondition | Stale Gotrue `CurrentSession` may shadow the new sign-in (bug B1) | INV-2 guarantees clean state at login screen |

## 3. Sign-Out Clearing Checklist (FR-004)

Executed by `SessionCoordinator.SignOutAsync` in order; steps 1–3 are unconditional (server revoke failure cannot skip them):

| # | What | Store |
| --- | --- | --- |
| 1 | Gotrue client session + persisted JSON | in-memory + `loopmeet.auth.session` |
| 2 | Profile cache | `loopmeet.profile.cache` (`UserProfileCache.Clear()`) |
| 3 | OneSignal identity (`Logout()`) — currently never done | OneSignal SDK state |
| 4 | Server-side token revoke (best-effort, may fail offline) | Supabase |
| 5 | Navigate `//login` (+ notice when `reason != UserInitiated`) | Shell |

## 4. SignOutReason (new enum)

| Value | Notice on login screen |
| --- | --- |
| `UserInitiated` | none |
| `SessionRejected` | "Your session ended. Please sign in again." |
| `SessionRejectedWithUnsavedInput` | "Your session ended and unsaved changes were lost. Please sign in again." (clarification Q4) |

Detection of unsaved input: the forced sign-out path asks the current page's viewmodel (marker interface `IHasUnsavedInput`, implemented by the create/edit form viewmodels) — a simple boolean check, no draft persistence.
