# Tasks: Sign in with Apple

**Input**: Design documents from `/specs/009-apple-signin/`
**Prerequisites**: plan.md (loaded), spec.md (3 user stories: P1/P2/P3), research.md (9 decisions), data-model.md (no schema changes), contracts/apple-signin-contract.md (4 typed contracts), quickstart.md (10-row validation matrix)

**Tests**: One test task is included for the cross-platform command surface (explicitly called out in plan.md §"Test surface" and research.md §9). The native AuthenticationServices flow itself is not unit-testable off an iOS host; validation is via the quickstart manual matrix.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3). Setup/Foundational/Polish phases have no story label.

## Path Conventions

This feature lives entirely in the .NET MAUI client. Paths are repo-relative:

- App source: `src/LoopMeet.App/`
- App tests: `tests/LoopMeet.App.Tests/`
- Apple platform code: `src/LoopMeet.App/Platforms/iOS/` (entitlements) and `src/LoopMeet.App/Features/Auth/Platforms/Apple/` (new helper)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: One-time, out-of-source prerequisites that don't ship in the repo but must exist for any in-source work to validate.

- [ ] T001 Confirm Apple Developer Portal capability is enabled for App ID `io.loopmeet.app`: Certificates, Identifiers & Profiles → Identifiers → `io.loopmeet.app` → enable **Sign in with Apple**. Regenerate both the iOS App Development profile and the Ad Hoc / App Store profile, download, and install via double-click. See `specs/009-apple-signin/quickstart.md` § "Apple Developer Portal one-time setup".
- [ ] T002 Confirm Supabase Apple provider configuration: dashboard → Authentication → Providers → Apple is **Enabled**, Service ID + Team ID + Key ID populated, `.p8` private key uploaded. See `specs/009-apple-signin/quickstart.md` § "Verify the Supabase side".

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: In-source assets needed by all user stories — entitlement keys (without which iOS rejects the build at install time) and the cross-platform nonce helper.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T003 [P] Add `com.apple.developer.applesignin` entitlement to `src/LoopMeet.App/Platforms/iOS/Entitlements.Debug.plist`. Insert `<key>com.apple.developer.applesignin</key><array><string>Default</string></array>` as a sibling of the existing `aps-environment` key. See `specs/009-apple-signin/contracts/apple-signin-contract.md` § 6.
- [X] T004 [P] Add `com.apple.developer.applesignin` entitlement to `src/LoopMeet.App/Platforms/iOS/Entitlements.Release.plist` with identical content as T003.
- [X] T005 [P] Implement cross-platform nonce helper at `src/LoopMeet.App/Features/Auth/AppleAuthNonce.cs`: `internal static class AppleAuthNonce` with `Generate()` returning `(string Raw, string Hashed)` where `Raw` is `RandomNumberGenerator.GetBytes(32)` URL-safe base64-encoded and `Hashed` is the lowercase-hex SHA-256 of `Raw`. No `#if` guard needed — file uses only `System.Security.Cryptography`. See `specs/009-apple-signin/contracts/apple-signin-contract.md` § 3.

**Checkpoint**: Foundation ready — user story implementation can begin.

---

## Phase 3: User Story 1 - Sign in with Apple as a New User (Priority: P1) 🎯 MVP

**Goal**: A first-time Apple-platform user can tap **Continue with Apple**, complete the native sheet, land on Home with a LoopMeet account created from their Apple identity, and remain signed in across app restarts.

**Independent Test**: Install on iPhone with no existing LoopMeet account → tap Sign in with Apple → complete native prompt with real email shared → confirm Home page → force-quit and relaunch → confirm session persisted (no login prompt). Repeats with Hide My Email selected. Matches `quickstart.md` rows 1, 2, 3.

### Implementation for User Story 1

- [X] T006 [US1] Implement `src/LoopMeet.App/Features/Auth/Platforms/Apple/AppleAuthCredentialProvider.cs` as an `internal static class` exposing `Task<ASAuthorizationAppleIDCredential?> RequestCredentialAsync(string hashedNonce)`. The entire file MUST be wrapped in `#if IOS || MACCATALYST` so it does not appear in non-Apple build outputs. Construct `ASAuthorizationAppleIDProvider`, request scopes `[Email, FullName]`, set `request.Nonce = hashedNonce`, present via `ASAuthorizationController.PerformRequests()`, bridge the `ASAuthorizationControllerDelegate` `DidComplete` / `DidFail` callbacks to a `TaskCompletionSource<ASAuthorizationAppleIDCredential?>`. Map `DidFail` with `ASAuthorizationError.Canceled` to `SetResult(null)`; map other errors to `SetException`. See `specs/009-apple-signin/contracts/apple-signin-contract.md` § 2.
- [X] T007 [US1] Add `public async Task<OAuthSignInResult> SignInWithAppleAsync()` to `src/LoopMeet.App/Features/Auth/AuthService.cs`. Inside `#if IOS || MACCATALYST`: call `AppleAuthNonce.Generate()`, await `AppleAuthCredentialProvider.RequestCredentialAsync(hashedNonce)`, return `new OAuthSignInResult()` when null (user cancel), otherwise decode `credential.IdentityToken` to UTF-8 string, call `_client.Auth.SignInWithIdToken(Constants.Provider.Apple, idToken, rawNonce)`, run `SaveAccessToken(_accessToken)`, and build the `OAuthSignInResult` populating `DisplayName` from `credential.FullName` (falling back to `GetUserDisplayName(user)`), `Email` from `credential.Email` (falling back to `user?.Email` then `TryGetJwtClaim(_accessToken, "email")`), and `Phone`/`AvatarUrl` to `null`. Outside the `#if` block: `throw new PlatformNotSupportedException(...)`. See `specs/009-apple-signin/contracts/apple-signin-contract.md` § 1 for exact signature and error contract.
- [X] T008 [US1] Modify `src/LoopMeet.App/Features/Auth/ViewModels/LoginViewModel.cs`: (a) add a `public bool ShowAppleSignIn => true;` getter wrapped in `#if IOS || MACCATALYST` (with the non-Apple branch returning `false`); (b) add a `[RelayCommand] private async Task SignInWithAppleAsync()` method wrapped in `#if IOS || MACCATALYST` whose body mirrors `SignInWithGoogleAsync` line-for-line, substituting `_authService.SignInWithAppleAsync()` for the service call and `"Apple"` for `"Google"` in log messages and error text. The command name `SignInWithAppleCommand` is auto-generated by `[RelayCommand]`. See `specs/009-apple-signin/contracts/apple-signin-contract.md` § 4.
- [X] T009 [US1] Modify `src/LoopMeet.App/Features/Auth/Views/LoginPage.xaml`: insert a `<Button Text="Continue with Apple" IsVisible="{Binding ShowAppleSignIn}" Command="{Binding SignInWithAppleCommand}">…</Button>` immediately after the existing **Continue with Google** button (around line 32), with the same `IsBusy → IsEnabled=False` `DataTrigger` as the other buttons. See `specs/009-apple-signin/contracts/apple-signin-contract.md` § 5.
- [X] T010 [P] [US1] Add `tests/LoopMeet.App.Tests/Features/Auth/AppleSignInCommandSurfaceTests.cs`. Use source-text assertions (matching the pattern at `tests/LoopMeet.App.Tests/Services/Notifications/NotificationPermissionServiceTests.cs`): read `src/LoopMeet.App/Features/Auth/ViewModels/LoginViewModel.cs`, assert it contains `"#if IOS || MACCATALYST"`, `"SignInWithAppleAsync"`, and `"ShowAppleSignIn"`. Read `src/LoopMeet.App/Features/Auth/Platforms/Apple/AppleAuthCredentialProvider.cs`, assert it contains `"#if IOS || MACCATALYST"`, `"ASAuthorizationAppleIDProvider"`, and `"RequestCredentialAsync"`. Read `src/LoopMeet.App/Features/Auth/AppleAuthNonce.cs`, assert it contains `"RandomNumberGenerator"` and `"SHA256"`. See `specs/009-apple-signin/plan.md` § "Test surface".

### Validation for User Story 1

- [ ] T011 [US1] On a physical iPhone signed into an Apple ID, perform `quickstart.md` matrix rows **1** (first-time user, real email shared), **2** (first-time user, Hide My Email), and **3** (returning user, app relaunch). All three must pass before declaring US1 complete.
- [X] T012 [US1] Run `dotnet test LoopMeet.slnx -c Debug -p:SkipMaciOSTargets=true` from repo root. T010 must pass; all 69 prior tests must still pass (70 total).

**Checkpoint**: User Story 1 is independently functional and testable.

---

## Phase 4: User Story 2 - Link an Apple Identity to an Existing Account (Priority: P2)

**Goal**: A user who already has a LoopMeet account (registered via email or Google) signs in with Apple using a matching verified email and lands in the *existing* account rather than a duplicate. After that, all three providers reach the same account.

**Independent Test**: Pre-create an email-registered account; sign out; sign in with Apple using a matching Apple ID. Confirm same account opens (same groups, same display name). Matches `quickstart.md` rows 4, 5, 8.

### Validation for User Story 2

**No code is added in this phase.** Account linking is performed server-side by Supabase's `SignInWithIdToken` when the Apple identity token's `email` claim matches an existing verified user's email. The viewmodel's existing `TryGetProfileAsync` vs `TryCreateProfileFromOAuthAsync` branch (introduced for Google in US1's modifications and inherited via line-for-line mirroring) handles the "match → open existing, no-match → create new" decision. US2 is therefore enabled by US1 and validated by these manual matrix runs:

- [ ] T013 [US2] Perform `quickstart.md` matrix row **4**: pre-create a LoopMeet account via email/password with `joel@example.com`. Sign out. Sign in with Apple using an Apple ID whose verified email is `joel@example.com`. Confirm the existing account opens (groups, meetups, display name intact) — not a new empty account.
- [ ] T014 [US2] Perform `quickstart.md` matrix row **5**: pre-create a LoopMeet account via Google sign-in. Sign out. Sign in with Apple using the same verified email as the Google account. Confirm the existing Google-registered account opens. Sign out, sign in via email/password reset (if applicable), Google, and Apple in turn — all three open the same account.
- [ ] T015 [US2] Perform `quickstart.md` matrix row **8**: from an account previously linked to Apple, sign out, sign in with Apple again. Apple does not re-share email/name; confirm the user lands in the same existing account via the stable Apple `sub` claim (not a new account).

**Checkpoint**: User Story 2 is independently functional and testable.

---

## Phase 5: User Story 3 - Apple Sign-In Is Hidden on Non-Apple Platforms (Priority: P3)

**Goal**: Android and Windows binaries contain no Apple sign-in UI, service classes, or related strings (per spec FR-002 and the spec's "where possible" binary-level invisibility clause).

**Independent Test**: Build Android Release and Windows Release; open the login screen on each; visually confirm no Apple button; grep the built artifacts for Apple-specific type names and confirm zero matches. Matches `quickstart.md` rows 9, 10.

### Validation for User Story 3

- [ ] T016 [P] [US3] Build Android Release via `./deploy/deploy-android.sh -c Release` and launch on a physical device or emulator. Open the login screen. Confirm only **Sign In**, **Continue with Google**, and **Create Account** buttons are present. No Apple button visible, no greyed-out element, nothing reachable via tab order. Matches `quickstart.md` row 9.
- [ ] T017 [P] [US3] After T016, inspect the Android APK at `src/LoopMeet.App/bin/Release/net10.0-android/io.loopmeet.app.apk`: run `strings src/LoopMeet.App/bin/Release/net10.0-android/io.loopmeet.app.apk | grep -iE 'ASAuthorization|AppleAuthCredentialProvider|AppleIDProvider'` and assert zero matches. (The string "Continue with Apple" may appear because XAML markup is shipped on all platforms; that's acceptable per the spec's "where possible" wording.) Matches `quickstart.md` row 10.
- [ ] T018 [P] [US3] Optional, only if a Windows host is available: build `dotnet build src/LoopMeet.App/LoopMeet.App.csproj -c Release -f net10.0-windows10.0.19041.0`, launch, confirm Apple button is absent on the login screen.

**Checkpoint**: User Story 3 is independently functional and testable.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validate the documented edge-case behaviors (cancel, error) and prepare the PR.

- [ ] T019 Perform `quickstart.md` matrix row **6** (mid-flow cancellation): tap **Continue with Apple**, then tap **Cancel** on the native sheet. Confirm the login screen returns to its initial state with no error toast, the button is re-enabled, and other sign-in methods remain available.
- [ ] T020 Perform `quickstart.md` matrix row **7** (Apple service unreachable): toggle airplane mode mid-flow, then tap **Continue with Apple**. Confirm a humane error message is shown (mirroring the Google flow's wording), the login screen stays intact, and other methods remain available.
- [ ] T021 Open a pull request titled "feat(auth): Sign in with Apple on iOS / MacCatalyst", body summarizing the change set, the 10-row test plan checklist from `quickstart.md`, and explicit confirmation that all of T011–T020 passed.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No code deps; T001 and T002 can run in parallel.
- **Phase 2 (Foundational)**: depends only on Phase 1's Apple Developer Portal step (T001) for the entitlement to actually take effect on-device. T003 / T004 / T005 are mutually independent within Phase 2.
- **Phase 3 (US1)**: depends on Phase 2 completion. Within Phase 3, the implementation chain is T006 → T007 → T008 → T009 (each layer consumes the one below). T010 (tests) only depends on the *file paths* of T006/T008 existing — it asserts on file contents, so it can be authored in parallel with T009. T011 (manual validation) requires T009 done and the device to have the regenerated provisioning profile from T001. T012 (test run) requires T010.
- **Phase 4 (US2)**: depends on US1 (T006–T009) being merged into the build the device runs.
- **Phase 5 (US3)**: depends on US1 (T008 specifically — the `ShowAppleSignIn` property compile-time gating is what produces the absence on non-Apple).
- **Phase 6 (Polish)**: depends on US1.

### Within-Phase Parallelism

- **Phase 2**: T003 ∥ T004 ∥ T005 (all `[P]`).
- **Phase 3**: T010 can run in parallel with T009 (different files; tests are source-text assertions).
- **Phase 5**: T016 ∥ T017 ∥ T018 (different platforms / different inspection methods).

### Story-Level Parallelism

US2 (Phase 4) and US3 (Phase 5) and Polish (Phase 6) can run concurrently once US1 lands, since they touch disjoint validation surfaces (US2 = real-device account-linking, US3 = Android/Windows build inspection, Polish = iOS edge cases).

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 (T001 + T002) — environment prerequisites.
2. Complete Phase 2 (T003 + T004 + T005) — entitlements and nonce helper.
3. Complete Phase 3 (T006 → T009 sequential, T010 in parallel with T009, then T011 + T012) — Apple sign-in functional for new users.
4. Demo: install on iPhone, tap **Continue with Apple**, confirm new account creation and session persistence.

That ships the App Store compliance line (Sign in with Apple is offered on Apple platforms) and the primary user value. US2 and US3 are validation phases that confirm the implementation already satisfies the remaining acceptance scenarios.

### Incremental Delivery

After the MVP demo, run US2 validation (T013 + T014 + T015) on a separate test account to confirm cross-provider account linking. Then US3 validation (T016 + T017, optionally T018) to confirm the non-Apple binary inspection. Finally Polish (T019 + T020) to confirm cancel/error edge cases. Open the PR (T021).

### Parallel Team Strategy

- **Engineer A** owns T006 → T007 → T008 → T009 (the implementation chain).
- **Engineer B** owns T003 + T004 + T005 (entitlements + nonce helper, in parallel with A) and T010 (test file, in parallel with A's later steps).
- **QA / a second engineer** owns the validation tasks T011–T020 on a real device once the implementation is merged.

---

## Notes

- All `[P]` tasks target distinct files or distinct validation environments; none rewrite the same line of the same file.
- The Apple-platform-only files (`AppleAuthCredentialProvider.cs`, the Apple-sections of `AuthService.cs` and `LoginViewModel.cs`) are guarded by `#if IOS || MACCATALYST` per the spec's binary-invisibility requirement on non-Apple platforms.
- The Supabase Apple OAuth provider configuration is documented as already complete (spec assumption) and is not part of any task in this list — the verification step T002 is included to catch the rare case where someone removed it.
- Sensitive credential files (`.p8` private key, `.mobileprovision`) MUST remain on the developer's local machine and never be committed.
