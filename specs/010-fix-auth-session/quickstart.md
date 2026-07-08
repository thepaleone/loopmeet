# Quickstart: Validating Reliable Sign-In Sessions & Startup Check

**Feature**: 010-fix-auth-session | **Date**: 2026-07-08

## Prerequisites

- Physical iPhone (primary) plus at least one Android device/emulator for FR-010 uniformity spot-checks.
- A test account reachable via all three providers (email/password, Google, Apple).
- Supabase dashboard access for the operator check below.

## Operator check: Supabase session settings (one-time, D7)

Dashboard → Authentication → Sessions: confirm **Time-box user sessions** = never and **Inactivity timeout** = never (defaults). Refresh-token rotation stays enabled (default). JWT expiry can remain at its current value (~3600 s); no change needed — renewal, not token lifetime, provides session longevity.

## Fast token-expiry testing tip

Temporarily lower JWT expiry (Dashboard → Authentication → JWT expiry) to 300 s on the **staging** project so expiry-window rows below take minutes, not an hour. Restore afterward.

## Validation matrix

| # | Story | Scenario | Steps | Expected |
| --- | --- | --- | --- | --- |
| 1 | US1 | Foreground refresh after backgrounding past expiry | Sign in → background the app for > 1 JWT lifetime → foreground | Data screens work immediately; no login bounce; log shows `Renewed` on `AppForegrounded` |
| 2 | US1 | Multi-day usage | Use the app at least once daily for 3+ days (never signing out) | Never returned to login screen |
| 3 | US1 | Mid-session API expiry | With app foregrounded, wait past JWT expiry without resuming (defeats trigger), then pull-to-refresh a list | Request transparently retried after refresh; no visible error, no login bounce |
| 4 | US1 | Rapid background/foreground cycling | Switch away/back 5× within 10 s | Exactly ≤ 1 renewal attempt in logs (debounce); no navigation glitches |
| 5 | US2 | Immediate re-sign-in, all providers | Log out → sign in with email → log out → Google → log out → Apple | Each succeeds on first attempt, no force-quit needed (SC-002) |
| 6 | US2 | Re-sign-in after *forced* sign-out | Revoke the session server-side (Dashboard → Authentication → Users → sign out user) → trigger an API call → land on login | Signing back in immediately succeeds with any provider |
| 7 | US2 | Abandoned attempt doesn't block | Tap Google sign-in, background the app mid-flow, return, cancel; then sign in with email | Email sign-in unaffected (FR-007) |
| 8 | US3 | Uniform forced sign-out per screen | With a revoked session, trigger a load on Home, Groups, Invitations, Profile in turn (fresh revoke each time) | Identical behavior all four times: route to login + notice; no empty-state masking (SC-003) |
| 9 | US3 | Full local clear | After forced sign-out, inspect Preferences (dev tools) and OneSignal state | `loopmeet.auth.session`, `loopmeet.auth.access_token`, `loopmeet.profile.cache` absent; OneSignal identity logged out |
| 10 | US3 | Unsaved-input notice | Start filling Create Meetup form → revoke session server-side → save/submit | Routed to login with "unsaved changes were lost" notice (clarification Q4); no crash |
| 11 | US4 | Signed-in cold launch, no flash | Force-quit → relaunch signed in (screen-record) | Gate page ("Checking your session…") → home. Login never visible (SC-004) |
| 12 | US4 | Signed-out cold launch, no flash | Sign out → force-quit → relaunch (screen-record) | Gate page → login. Home never visible |
| 13 | US4 | Offline launch with cached session | Airplane mode → force-quit → relaunch | Gate resolves ≤ 5 s → home with cached/empty data (FR-011a); once connectivity returns, background revalidation succeeds; no sign-out |
| 14 | US4 | Offline launch, no session | Airplane mode, signed out → relaunch | Gate resolves ≤ 5 s → login |
| 15 | Edge | Definitive rejection while offline→online | Revoke session server-side while device is offline → bring device online → foreground | Next renewal definitively rejected → full sign-out + notice; recoverable (row 5 flow works) |
| 16 | Edge | Clock skew sanity | Set device clock +2 h manually → use the app | No sign-out; at worst one extra refresh round-trip (D9) |
| 17 | FR-010 | Android parity spot-check | Repeat rows 1, 5, 11, 12 on Android | Identical behavior |

## Automated checks

```bash
dotnet test LoopMeet.slnx -c Debug -p:SkipMaciOSTargets=true
```

New unit tests must cover: `SessionFailureClassifier` (every table row in contract §3), `SessionRenewalPolicy` boundaries (debounce window, final-1/5 lifetime), `ApiAuthHandler` single-retry semantics (401→refresh→retry; 401→definitive→no retry; non-401 untouched), and the regression case: *expired persisted session must trigger refresh, never `SignOut()`* (root cause A1).

## Log verification

Every row above should produce structured log lines from `SessionCoordinator` (INV-4): `SessionRestored`, `SessionRenewed`, `RenewalTransientFailure` (kept), `RenewalRejected` (signed out), `SignedOut {reason}`. Absence of a log line for a transition is a finding.
