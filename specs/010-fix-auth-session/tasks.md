# Tasks: Reliable Sign-In Sessions & Startup Check

**Input**: Design documents from `/specs/010-fix-auth-session/`
**Prerequisites**: plan.md (loaded), spec.md (4 user stories: P1–P4), research.md (root causes A1–C2, decisions D1–D9), data-model.md (session state machine, INV-1..4), contracts/session-lifecycle-contract.md (§1–§9), quickstart.md (17-row matrix)

**Tests**: Included — the Meetloop Constitution (Principle II) makes tests a required deliverable, and plan.md §Technical Context enumerates the unit-test surface. Pure logic (classifier, policy, handler retry) gets real unit tests; platform-coupled wiring gets source-surface tests (established repo pattern); native lifecycle/navigation is validated via the quickstart matrix.

**Organization**: Tasks are grouped by user story. US1 (session continuity) is the MVP. US2 rides on foundational + login fixes; US3 (sign-out) and US4 (startup gate) each own one coordinator method.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1–US4 per spec.md. Setup/Foundational/Polish phases have no story label.

## Path Conventions

Feature lives entirely in the .NET MAUI client:

- App source: `src/LoopMeet.App/`
- Session module (new): `src/LoopMeet.App/Features/Auth/Session/`
- Tests: `tests/LoopMeet.App.Tests/`

---

## Phase 1: Setup (Environment Prerequisites)

**Purpose**: Out-of-source operator checks that gate correct behavior of everything below.

- [ ] T001 Verify Supabase session settings per quickstart.md §"Operator check": Dashboard → Authentication → Sessions → **Time-box user sessions** = never, **Inactivity timeout** = never, refresh-token rotation enabled. Record current JWT expiry value (needed for quickstart timing).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The session module's pure core, the single-token-source cleanup (D1), and DI wiring — every user story depends on these.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [ ] T002 [P] Create `src/LoopMeet.App/Features/Auth/Session/SignOutReason.cs`: enum `SignOutReason { UserInitiated, SessionRejected, SessionRejectedWithUnsavedInput }` plus static `SignOutNotices.For(SignOutReason)` returning the notice strings from data-model.md §4 (null for `UserInitiated`).
- [ ] T003 [P] Create `src/LoopMeet.App/Features/Auth/Session/SessionFailureClassifier.cs`: pure static `Classify(Exception ex) → SessionFailureKind { Definitive, Transient }` implementing every row of contract §3 exactly — `GotrueException.Reason` ∈ {ExpiredRefreshToken, InvalidRefreshToken, NoSessionFound} or token-endpoint HTTP 400/401/403 → Definitive; Offline/timeout/`HttpRequestException`/5xx/unknown → Transient (fail-safe default Transient per FR-004a).
- [ ] T004 [P] Create `src/LoopMeet.App/Features/Auth/Session/SessionRenewalPolicy.cs` per contract §4: `bool ShouldAttempt(DateTimeOffset nowUtc, DateTimeOffset? lastSuccessUtc, DateTimeOffset? tokenExpiryUtc)` — attempt when outside the 30 s debounce window OR token expired/within final 1/5 lifetime. All time injected; no `DateTime.Now`.
- [ ] T005 [P] Create `tests/LoopMeet.App.Tests/Features/Auth/Session/SessionFailureClassifierTests.cs`: one `[Theory]` case per contract §3 table row, including the fail-safe default (arbitrary exception → Transient).
- [ ] T006 [P] Create `tests/LoopMeet.App.Tests/Features/Auth/Session/SessionRenewalPolicyTests.cs`: boundary tests — inside/outside 30 s window, exactly at final-1/5 lifetime, expired token, null expiry, null lastSuccess.
- [ ] T007 Refactor `src/LoopMeet.App/Features/Auth/AuthService.cs` to the single token source (D1, contract §9): delete `AccessTokenKey`, `_accessToken`, `SaveAccessToken`, `IsJwtExpired`/`TryGetJwtExpiry` fallback logic; `GetAccessToken()` returns `_client.Auth.CurrentSession?.AccessToken` only; keep sign-in methods setting nothing beyond the Gotrue client state. Add one-time cleanup `Preferences.Default.Remove("loopmeet.auth.access_token")` at client init. Keep `GetCurrentUserId()` working off `GetAccessToken()`. NOTE: leave `RestoreSessionAsync` present but mark its expired-session branch for the T012 fix (US1) — do not fix it here.
- [ ] T008 Create `src/LoopMeet.App/Features/Auth/Session/SessionCoordinator.cs` skeleton implementing `ISessionTokenSource` (contract §1–§2): constructor deps (`Supabase.Client`, `AuthService`, `UserProfileCache`, `ILogger<SessionCoordinator>`), method stubs `ResolveStartupAsync`, `EnsureFreshSessionAsync`, `SignOutAsync` (minimal working version: clear Gotrue session + profile cache + navigate `//login`), `GetAccessToken()` delegating to the Gotrue session, single-flight `Task` field + `SessionRenewalPolicy` instance. Define `RenewalTrigger`, `RenewalOutcome`, `StartupResolution` types per contract §1. Emit structured `ILogger` events for every transition (INV-4).
- [ ] T009 Register the module in `src/LoopMeet.App/MauiProgram.cs`: `SessionCoordinator` as Singleton, also as `ISessionTokenSource`; change `ApiAuthHandler` registration to consume `ISessionTokenSource` (constructor updated in T014).

**Checkpoint**: Pure core tested and green; coordinator skeleton compiles; app still behaves as before.

---

## Phase 3: User Story 1 — Stay Signed In While Actively Using the App (Priority: P1) 🎯 MVP

**Goal**: Active users are never signed out by elapsed time: expired restores refresh instead of destroying the refresh token (root cause A1), foregrounding refreshes proactively (A2), and a mid-session 401 is transparently refresh-retried (A4). Transient failures never end the session (FR-004a).

**Independent Test**: quickstart.md rows 1–4 — background past JWT expiry and foreground (silent renewal), multi-day usage, mid-session expiry with pull-to-refresh (transparent retry), rapid background/foreground cycling (≤ 1 renewal attempt).

### Implementation for User Story 1

- [ ] T010 [US1] Implement `SessionCoordinator.EnsureFreshSessionAsync(RenewalTrigger)` in `src/LoopMeet.App/Features/Auth/Session/SessionCoordinator.cs`: consult `SessionRenewalPolicy`; single-flight (concurrent callers await the same in-flight task, INV-1); refresh via `_client.Auth.RefreshToken()`; on exception classify with `SessionFailureClassifier` — Transient → keep session, return `TransientFailureKeptSession`; Definitive → `await SignOutAsync(SessionRejected)`, return `DefinitivelyRejectedSignedOut`. Log every outcome with trigger.
- [ ] T011 [US1] Implement `ISessionTokenSource.RefreshForRetryAsync()` on the coordinator delegating to `EnsureFreshSessionAsync(RenewalTrigger.ApiUnauthorized)` (contract §2).
- [ ] T012 [US1] Fix `AuthService.RestoreSessionAsync` in `src/LoopMeet.App/Features/Auth/AuthService.cs` (root cause A1): when the persisted session is expired, attempt `_client.Auth.RefreshToken()` instead of `SignOut()`; classify failures — Transient → return the cached session optimistically (FR-011a groundwork); Definitive → destroy local session and return null. Never call `SignOut()` from the restore path.
- [ ] T013 [US1] Hook app foregrounding in `src/LoopMeet.App/App.xaml.cs` per contract §8: in `CreateWindow`, subscribe `window.Resumed += (_, _) => _ = coordinator.EnsureFreshSessionAsync(RenewalTrigger.AppForegrounded);` (resolve coordinator from DI; fire-and-forget, exceptions handled inside the coordinator).
- [ ] T014 [US1] Rewrite `src/LoopMeet.App/Services/ApiAuthHandler.cs` per contract §5: constructor takes `ISessionTokenSource`; attach bearer; on 401 and not-yet-retried → `RefreshForRetryAsync()`; if `Renewed`/`StillValid`, build a genuine clone of the request (an `HttpRequestMessage` cannot be re-sent: copy method, URI, headers, and options; buffer the original content bytes + content headers and wrap in new `ByteArrayContent`), mark it retried via `HttpRequestOptions`, attach the new token, and send once; otherwise return the 401. Handler never navigates, never clears state.
- [ ] T015 [P] [US1] Create `tests/LoopMeet.App.Tests/Features/Auth/Session/ApiAuthHandlerRetryTests.cs` using a scripted inner `HttpMessageHandler` fake and an `ISessionTokenSource` fake: (a) 401 → refresh Renewed → retried once with new token → 200; (b) 401 → `DefinitivelyRejectedSignedOut` → no retry, 401 returned; (c) 200 → no refresh call; (d) second 401 after retry → returned as-is (exactly one retry).
- [ ] T016 [P] [US1] Create `tests/LoopMeet.App.Tests/Features/Auth/Session/SessionSurfaceTests.cs` (source-surface, repo pattern per `AppleSignInCommandSurfaceTests`): assert `AuthService.cs` restore path contains `RefreshToken` and does NOT contain `Auth.SignOut()` outside the sign-out method (regression: A1); assert `App.xaml.cs` contains `Resumed` + `EnsureFreshSessionAsync`; assert `AuthService.cs` no longer contains `loopmeet.auth.access_token` except the one-time cleanup line.
- [ ] T017 [US1] Run `dotnet test LoopMeet.slnx -c Debug -p:SkipMaciOSTargets=true` — all new and prior tests pass.
- [ ] T018 [US1] Device validation: quickstart.md rows **1–4** on iPhone (use the lowered-JWT staging tip). All four must pass before declaring US1 complete.

**Checkpoint**: The core bug is fixed — sessions survive backgrounding, multi-day use, and transient failures.

---

## Phase 4: User Story 2 — Always Able to Sign Back In Immediately (Priority: P2)

**Goal**: A sign-out is never a dead end: no stale singleton state shadows a fresh sign-in (fixed foundationally by T007/T008), and a hung or abandoned attempt can't wedge the login screen (root cause B2, FR-007).

**Independent Test**: quickstart.md rows 5–7 — logout→re-sign-in with all three providers first-try, re-sign-in after server-side revoke, abandoned OAuth attempt doesn't block a subsequent email sign-in.

### Implementation for User Story 2

- [ ] T019 [US2] Fix the busy-guard hostage in `src/LoopMeet.App/Features/Auth/ViewModels/LoginViewModel.cs` (B2 + FR-007, both halves): (a) post-success setup — in all three sign-in commands (email, Google, Apple), wrap `_authSessionService.HandleSuccessfulSignInAsync()` with a bounded timeout (e.g., `Task.WhenAny` + 10 s) so it can never hold `IsBusy` indefinitely; navigation to `//home` proceeds when sign-in itself succeeded even if setup timed out (log a warning); (b) abandoned provider flow — the Google/Apple provider `await` itself can hang forever when the user backgrounds mid-native-sheet and the callback never fires: link each provider sign-in to a `CancellationTokenSource` cancelled from the page's `OnDisappearing` (and on a fresh sign-in tap), map `TaskCanceledException`/`OperationCanceledException` to a silent reset (no error toast, matching the cancel convention). Confirm every command resets `IsBusy` in `finally` on all paths including cancellation (US2 acceptance scenario 4).
- [ ] T020 [US2] Extend `tests/LoopMeet.App.Tests/Features/Auth/Session/SessionSurfaceTests.cs` (created by T016 — if implementing US2 before US1, create the file first with only these assertions): assert `LoginViewModel.cs` bounds `HandleSuccessfulSignInAsync` (contains the timeout construct) in all three sign-in commands and contains the provider-flow `CancellationTokenSource` wiring from T019(b).
- [ ] T021 [US2] Device validation: quickstart.md rows **5–7** (all three providers cycle, post-revoke re-sign-in, abandoned-attempt non-blocking). Requires US1 merged for the revoke row to route correctly.

**Checkpoint**: Sign-out → immediate sign-in works first-try with every provider, no force-quit.

---

## Phase 5: User Story 3 — Consistent, Complete Sign-Out Everywhere (Priority: P3)

**Goal**: One sign-out path (INV-2) that always fully clears local state — Gotrue session, profile cache, OneSignal identity — even offline (root cause B3), fires from every screen identically (FR-003), never masked by empty states (FR-005), with the session-ended/unsaved-input notice (clarifications Q1/Q4).

**Independent Test**: quickstart.md rows 8–10 — identical forced sign-out behavior from Home/Groups/Invitations/Profile, full local clear verified, unsaved-input notice on forced sign-out mid-form.

### Implementation for User Story 3

- [ ] T022 [US3] Complete `SessionCoordinator.SignOutAsync(SignOutReason)` in `src/LoopMeet.App/Features/Auth/Session/SessionCoordinator.cs` per data-model.md §3 checklist: (1) clear Gotrue local session + persisted JSON unconditionally (wrap server revoke separately), (2) `UserProfileCache.Clear()`, (3) OneSignal `Logout()` (currently never called anywhere), (4) best-effort server revoke in its own try/catch, (5) set `SessionNoticeState.Pending` from the reason and navigate `//login`. Navigation and any `Shell.Current` access MUST be dispatched via `MainThread.InvokeOnMainThreadAsync` — this method is invoked from `ApiAuthHandler` on background threads (contract §1). Method never throws; logs `SignedOut {reason}`.
- [ ] T023 [US3] Create `src/LoopMeet.App/Features/Auth/Session/IHasUnsavedInput.cs` (marker interface, `bool HasUnsavedInput { get; }`) and implement it on the create/edit form viewmodels (`CreateGroupViewModel`, `EditGroupViewModel`, `CreateMeetupViewModel`, `EditMeetupViewModel`, `InviteMemberViewModel`); in the forced sign-out path, check `Shell.Current?.CurrentPage?.BindingContext is IHasUnsavedInput { HasUnsavedInput: true }` to select `SessionRejectedWithUnsavedInput` (clarification Q4). The `CurrentPage` inspection runs inside the same `MainThread.InvokeOnMainThreadAsync` block as T022's navigation (never from a background thread).
- [ ] T024 [US3] Rewire explicit logout in `src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs`: `LogoutAsync` calls `SessionCoordinator.SignOutAsync(SignOutReason.UserInitiated)` instead of `AuthService.SignOutAsync` + manual navigation; reduce `AuthService.SignOutAsync` to the local Gotrue-clear helper the coordinator calls for checklist step 1 (contract §9) — it no longer navigates, touches caches, or calls the server.
- [ ] T025 [P] [US3] Remove per-screen 401 handling per contract §7: delete the Unauthorized-redirect branches in `src/LoopMeet.App/Features/Groups/ViewModels/GroupsListViewModel.cs` and `src/LoopMeet.App/Features/Invitations/ViewModels/PendingInvitationsViewModel.cs` (the handler now owns 401s); genuine network errors keep their existing alerts.
- [ ] T026 [P] [US3] Stop masking session loss per FR-005: in `src/LoopMeet.App/Features/Home/ViewModels/HomeViewModel.cs` replace the bare `catch {}` with handling that surfaces non-auth errors and lets handler-driven sign-out routing occur; same for the swallow-all catch in `src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs` `LoadAsync`.
- [ ] T027 [US3] Show the notice: create `src/LoopMeet.App/Features/Auth/Session/SessionNoticeState.cs` per contract §6a (singleton holding `SignOutReason? Pending` with a consume-once `TakePending()`); add `SessionEndedNotice` (nullable string) to `src/LoopMeet.App/Features/Auth/ViewModels/LoginViewModel.cs` populated in `OnAppearing`/activation via `SignOutNotices.For(noticeState.TakePending())`; render as a dismissible banner in `src/LoopMeet.App/Features/Auth/Views/LoginPage.xaml` (verify style resource keys exist in Styles.xaml/Colors.xaml before use). Register `SessionNoticeState` as Singleton in `src/LoopMeet.App/MauiProgram.cs`.
- [ ] T028 [P] [US3] Create `tests/LoopMeet.App.Tests/Features/Auth/Session/SignOutNoticesTests.cs`: unit-test `SignOutNotices.For` mapping (data-model.md §4). Extend `SessionSurfaceTests.cs`: `SessionCoordinator.cs` contains `Logout` (OneSignal) and `UserProfileCache`; `GroupsListViewModel.cs`/`PendingInvitationsViewModel.cs` no longer contain `//login`; `ProfileViewModel.cs` routes logout through `SignOutAsync(SignOutReason.UserInitiated)`.
- [ ] T029 [US3] Run `dotnet test LoopMeet.slnx -c Debug -p:SkipMaciOSTargets=true` — full suite green.
- [ ] T030 [US3] Device validation: quickstart.md rows **8–10** (per-screen uniformity, full local clear incl. OneSignal, unsaved-input notice).

**Checkpoint**: Session end is uniform, complete, and honest on every screen.

---

## Phase 6: User Story 4 — Clear Status While the App Checks Sign-In State on Launch (Priority: P4)

**Goal**: No login flash (root causes C1/C2): a startup gate page is the first thing rendered, resolves within 5 s, and navigates exactly once to `//home` or `//login` (FR-008/FR-009/FR-011a).

**Independent Test**: quickstart.md rows 11–14 — screen-recorded cold launches signed-in/signed-out (no wrong-screen frame), offline launch with cached session → home ≤ 5 s, offline signed-out → login ≤ 5 s.

### Implementation for User Story 4

- [ ] T031 [US4] Implement `SessionCoordinator.ResolveStartupAsync(CancellationToken)` per contract §1, absorbing the T012-fixed restore logic from `AuthService.RestoreSessionAsync` (which T034 deletes): `_client.InitializeAsync()` → persisted session valid → `//home` + background `EnsureFreshSessionAsync(StartupRevalidation)`; expired + refresh token → one refresh bounded to 5 s total (`CancellationTokenSource` timeout); Definitive rejection → full `SignOutAsync` clearing then `//login` + `SessionRejected` notice via `SessionNoticeState`; Transient/timeout with cached session → `//home` (FR-011a); no session → `//login`, no notice. Never throws.
- [ ] T032 [P] [US4] Create `src/LoopMeet.App/Features/Auth/Views/StartupGatePage.xaml` + `.xaml.cs` per contract §6: full-screen brand background, centered `ActivityIndicator` + `Label` "Checking your session…" (verify style/color resource keys exist before use); no buttons, no back navigation (`Shell.BackButtonBehavior` disabled).
- [ ] T033 [US4] Create `src/LoopMeet.App/Features/Auth/ViewModels/StartupGateViewModel.cs`: on appearing, `var r = await _coordinator.ResolveStartupAsync(); await Shell.Current.GoToAsync(r.Route);` — exactly one navigation per launch; pass `r.Notice` to the login notice state when non-null.
- [ ] T034 [US4] Update `src/LoopMeet.App/AppShell.xaml`: insert `<ShellContent Route="startup" ContentTemplate="{DataTemplate auth:StartupGatePage}" />` as the FIRST ShellContent (login remains second); update `src/LoopMeet.App/AppShell.xaml.cs`: remove all session logic from `OnAppearing` (RestoreSessionAsync call, profile fetch, redirect) keeping only route registration and DevTools visibility; move the profile-summary prefetch + `PostLoginNotificationRedirectService.ResumeAsync()` into a fire-and-forget task the coordinator/gate triggers after `//home` navigation. Then **delete `AuthService.RestoreSessionAsync`** — its logic now lives in `ResolveStartupAsync` (T031) and it has no remaining callers (Constitution I: no dead code).
- [ ] T035 [US4] Register `StartupGatePage` and `StartupGateViewModel` (Transient) in `src/LoopMeet.App/MauiProgram.cs`.
- [ ] T036 [P] [US4] Extend `tests/LoopMeet.App.Tests/Features/Auth/Session/SessionSurfaceTests.cs`: `AppShell.xaml` has `Route="startup"` before `Route="login"`; `AppShell.xaml.cs` contains neither `RestoreSessionAsync` nor `GetProfileSummaryAsync`; `StartupGateViewModel.cs` contains `ResolveStartupAsync`.
- [ ] T037 [US4] Run `dotnet test LoopMeet.slnx -c Debug -p:SkipMaciOSTargets=true` — full suite green.
- [ ] T038 [US4] Device validation: quickstart.md rows **11–14** with screen recordings (flash-free launches, offline resolutions ≤ 5 s).

**Checkpoint**: Launch shows one continuous checking state, then exactly one destination.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Edge-case validation, drive-by correctness fix, uniformity spot-check, and PR.

- [ ] T039 [P] Fix wrong Shell routes in `src/LoopMeet.App/Services/Notifications/NotificationService.cs`: `"//Home"` → `SignedInTabs.HomeShellPath`, `"//Login"` → `"//login"` (casing mismatch found during research; broken deep-link navigation can strand users, undermining SC-003).
- [ ] T040 Verify INV-4 observability: every coordinator transition logs a structured event (`SessionRestored`, `SessionRenewed`, `RenewalTransientFailure`, `RenewalRejected`, `SignedOut {reason}`) — walk quickstart.md §"Log verification" against a device run; add any missing events in `SessionCoordinator.cs`.
- [ ] T041 Device validation, edge cases: quickstart.md rows **15–16** (offline→online definitive rejection recovers; clock skew +2 h causes no sign-out).
- [ ] T042 Android parity spot-check: quickstart.md row **17** (repeat rows 1, 5, 11, 12 on Android) per FR-010.
- [ ] T043 Open a pull request titled "fix(auth): reliable sessions, uniform sign-out, startup session gate", body summarizing root causes A1–C2 and fixes, the 17-row quickstart matrix as a checklist, and explicit confirmation that T017/T029/T037 suites and all device validations passed.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: independent; T001 can run anytime before device validation.
- **Phase 2 (Foundational)**: T002–T006 are mutually independent `[P]`. T007 (AuthService cleanup) is independent of T002–T006. T008 depends on T002–T004 + T007; T009 depends on T008.
- **Phase 3 (US1)**: depends on Phase 2. Chain: T010 → T011 → T014 (handler consumes RefreshForRetryAsync); T012 and T013 independent of each other after T010; T015/T016 `[P]` once T014/T012 exist; T017 after all; T018 last.
- **Phase 4 (US2)**: T019 only needs Phase 2 + T009 and can be built in parallel with US1 implementation (different files). T020 extends `SessionSurfaceTests.cs`, which T016 (US1) creates — run T020 after T016, or create the file within T020 if US2 lands first. T021 device validation needs US1 merged.
- **Phase 5 (US3)**: T022 depends on Phase 2 skeleton; T023–T027 depend on T022; T025/T026/T028 `[P]` among themselves.
- **Phase 6 (US4)**: T031 depends on T010 (reuses EnsureFreshSessionAsync) and T022 (SignOutAsync for definitive rejection); T032/T033 `[P]` with T031; T034 depends on T032–T033; T035 after T034.
- **Phase 7 (Polish)**: T039 anytime `[P]`; T040–T043 after US1–US4.

### Story-Level Notes

- US1 is the MVP and fixes the highest-severity bug on its own.
- US2's implementation (T019) touches only `LoginViewModel.cs` and can be built in parallel with US1.
- US3 and US4 both complete coordinator methods; US4's T031 consumes US3's T022, so run US3 before (or merge them if one engineer owns both).

### Within-Phase Parallelism

- **Phase 2**: T002 ∥ T003 ∥ T004 ∥ T005 ∥ T006 ∥ T007.
- **Phase 3**: T012 ∥ T013 after T010; T015 ∥ T016 after T014.
- **Phase 5**: T025 ∥ T026 ∥ T028 after T022.
- **Phase 6**: T031 ∥ T032 (different files); T036 ∥ device prep.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1 (T001) + Phase 2 (T002–T009): pure core + tests + single token source + skeleton.
2. Phase 3 (T010–T018): restore fix, foreground refresh, 401 retry.
3. Demo: background the app past JWT expiry, foreground → still signed in; pull-to-refresh past expiry → transparent retry.

That alone eliminates the top complaint (unexpected sign-outs). Each later story is an independent increment: US2 (login resilience), US3 (uniform full sign-out + notices), US4 (startup gate).

### Sizing note

34 in-source tasks + 9 device-validation/ops tasks. The riskiest task is T014 (ApiAuthHandler rewrite — every API call flows through it); it has the densest unit coverage (T015) and lands inside the MVP where quickstart rows 1–4 exercise it end-to-end.
