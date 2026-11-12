# Tasks: App Store Release Build (TestFlight)

**Input**: Design documents from `/specs/021-app-store-release/`
**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/build-ios-release.md](contracts/build-ios-release.md), [quickstart.md](quickstart.md)

**Tests**: required by the constitution (Principle II). They are written first and must fail before the change they guard. Native and script behavior is covered by source inspection (`CLAUDE.md`) plus the quickstart device checks.

**Organization**: one phase per user story. US1 and US2 are P1 and share the new test class, so the class is created in Phase 2.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: the user story the task belongs to (US1–US4)

---

## Phase 1: Setup

**Purpose**: make CI run the App tests when the files they read change. The 015 tests already depend on this, and so will 021's.

- [ ] T001 In `.github/scripts/ci-scope.test.sh`, change the "deploy script and staging deploy workflow" case. Add cases expecting `app_tests` for `deploy/build-android-release.sh`, `deploy/build-ios-release.sh` and `.gitignore`, while `deploy/deploy-android.sh` and `.github/workflows/deploy-staging.yml` still select nothing. Run `bash .github/scripts/ci-scope.test.sh` and confirm the new cases fail.
- [ ] T002 In `.github/scripts/ci-scope.sh`, map `deploy/build-*-release.sh` and `.gitignore` to `select_keys app_tests`, placed before the "selects nothing" branch. Remove `.gitignore` from that branch, and update the header comment that lists what `LoopMeet.App.Tests` reads. Re-run `ci-scope.test.sh` until it passes.

---

## Phase 2: Foundational

**Purpose**: the shared test class that US1 and US2 extend.

- [ ] T003 Create `tests/LoopMeet.App.Tests/Platforms/IosAppStoreReleaseTests.cs`, modeled on `AndroidPlayReleaseTests`:
  - a class summary naming feature 021;
  - a `ReadSource` helper that finds the repo root;
  - a plist `ValueFor(dict, key)` helper like the one in `AppleSceneLifecycleTests`;
  - constants for `Platforms/iOS/Info.plist`, `Platforms/iOS/Resources/PrivacyInfo.xcprivacy`, `Platforms/iOS/Entitlements.Release.plist`, `LoopMeet.App.csproj` and `deploy/build-ios-release.sh`.

**Checkpoint**: the test project builds.

---

## Phase 3: User Story 1 - A maintainer produces an uploadable build with one command (Priority: P1) 🎯 MVP

**Goal**: `deploy/build-ios-release.sh -b <n>` writes an App Store–signed `.ipa` to `artifacts/ios/<n>/`. It signs with the identity pinned by SHA-1, and it refuses bad inputs before building ([contract](contracts/build-ios-release.md)).

**Independent Test**: run the script with an unset variable, a bad `-b` and a wrong profile, and check that each stops with the contract's message. Then run it with the real identity and profile, and check that the `.ipa`'s `CFBundleVersion` equals `<n>` and its signed `aps-environment` is `production`.

### Tests for User Story 1 ⚠️ write first, confirm they fail

- [ ] T004 [US1] In `IosAppStoreReleaseTests.cs`, add `ReleaseScript_SignsWithPinnedIdentityAndProfileFromEnvironment`. It asserts the script contains:
  - `-f net10.0-ios`, `-r ios-arm64` and `-p:ApplicationVersion="$BUILD_NUMBER"`;
  - `-p:CodesignKey="$LOOPMEET_IOS_SIGNING_IDENTITY"` and `-p:CodesignProvision=`;
  - the output folder `artifacts/ios/$BUILD_NUMBER`;
  - no `RunAOTCompilation=false` and no `PublishTrimmed=false` (research D2).
- [ ] T005 [US1] In `IosAppStoreReleaseTests.cs`, add `ReleaseScript_ChecksEveryContractConditionBeforeBuilding`. For each of the nine pre-build checks in `contracts/build-ios-release.md`, it asserts the distinguishing text appears before the first line that runs `dotnet` (the script runs `dotnet clean` before `dotnet publish`):
  - the positive-integer test;
  - both variable names;
  - the 40-hexadecimal-character pattern;
  - `security find-identity -v -p codesigning`;
  - `security cms -D`;
  - `io.loopmeet.app`;
  - `ProvisionedDevices`;
  - `aps-environment`;
  - `ExpirationDate`;
  - `DeveloperCertificates`.

  It also asserts the post-build checks of `CFBundleVersion` and of `aps-environment` through `codesign`.
- [ ] T006 [P] [US1] In `IosAppStoreReleaseTests.cs`, add `Gitignore_ExcludesAppleSigningAssetsAndReleaseOutput`. It asserts `.gitignore` has the lines `*.mobileprovision`, `*.p12`, `*.cer`, `*.p8` and `artifacts/`.

### Implementation for User Story 1

- [ ] T007 [P] [US1] In `.gitignore`, add an "Apple signing assets and release output (feature 021)" block with `*.mobileprovision`, `*.p12` and `*.cer`. `*.p8` and `artifacts/` are already there. T006 passes.
- [ ] T008 [US1] Create `deploy/build-ios-release.sh` (zsh, `set -euo pipefail`, executable), following the structure of `deploy/build-android-release.sh`:
  - **Header:** usage, both variables and how to find the SHA-1, and why there's no AOT or trimming workaround (research D2).
  - **`-b` parsing:** with `getopts`.
  - **Pre-build checks 1–9:** in contract order, each with its contract message, exiting 1. To decode the profile, write `security cms -D -i` output to a temporary file and read fields with `plutil -extract … raw`. For the certificate check, decode each `DeveloperCertificates` entry and compare its `openssl x509 -noout -fingerprint -sha1` with the identity, ignoring case and colons.
  - **Profile install:** copy the profile as `<UUID>.mobileprovision` into both `~/Library/MobileDevice/Provisioning Profiles/` and `~/Library/Developer/Xcode/UserData/Provisioning Profiles/` (Xcode 16 and later), creating either folder if it's missing (research D3).
  - **Build:** `dotnet clean`, then the `dotnet publish` from research D1.
  - **Post-build checks:** unzip the `.ipa` to a temporary directory. Compare `CFBundleVersion` with `-b`, and check that `codesign -d --entitlements :- Payload/*.app` shows `aps-environment` = `production`.
  - **Output:** print the `.ipa` path.

  T004 and T005 pass.
- [ ] T009 [US1] Manually check the guards. Run the script with:
  1. no `-b`;
  2. `-b 0`;
  3. `LOOPMEET_IOS_SIGNING_IDENTITY` unset;
  4. the certificate's name in place of its SHA-1;
  5. a development profile (the one in `~/Library/MobileDevice/Provisioning Profiles`).

  Each must stop before `dotnet` runs, with its contract message. Record the results in the tasks log below.

**Checkpoint**: the script builds a signed `.ipa` with the real identity and profile. This needs the profile from quickstart setup step 3.

---

## Phase 4: User Story 2 - The build passes App Store Connect checks and Beta App Review (Priority: P1)

**Goal**: the build processes with no ITMS warnings and no export-compliance prompt, and Release allows no plain HTTP.

**Independent Test**: `IosAppStoreReleaseTests` passes. An uploaded build shows "Ready to Submit" or "Ready for internal testing" with no compliance question and no warning email.

### Tests for User Story 2 ⚠️ write first, confirm they fail

- [ ] T010 [P] [US2] In `IosAppStoreReleaseTests.cs`, add `PrivacyManifest_DeclaresEveryRequiredReasonApi`. It parses `PrivacyInfo.xcprivacy` and asserts:
  - exactly the four categories with their reasons: FileTimestamp `C617.1`, SystemBootTime `35F9.1`, DiskSpace `E174.1` and UserDefaults `CA92.1`;
  - `NSPrivacyTracking` is `false`;
  - the file contains no `<!--` inside the `NSPrivacyAccessedAPITypes` array, so no entry is commented out.
- [ ] T011 [P] [US2] In `IosAppStoreReleaseTests.cs`, add `InfoPlist_DeclaresExemptEncryptionAndAllowsNoPlainHttp`. It asserts:
  - `ITSAppUsesNonExemptEncryption` is `<false/>`;
  - the root dict has no `NSAppTransportSecurity` key;
  - the raw file doesn't contain `NSAllowsArbitraryLoads` or `NSExceptionAllowsInsecureHTTPLoads`.
- [ ] T012 [P] [US2] In `IosAppStoreReleaseTests.cs`, add `ReleaseEntitlements_UseProductionPushAndSignInWithApple`. It asserts `Entitlements.Release.plist` has `aps-environment` = `production` and `com.apple.developer.applesignin` = [`Default`]. It also asserts the csproj maps `CodesignEntitlements` to `Platforms/iOS/Entitlements.Release.plist` under `'$(Configuration)' != 'Debug'` for device builds.

### Implementation for User Story 2

- [ ] T013 [P] [US2] In `src/LoopMeet.App/Platforms/iOS/Resources/PrivacyInfo.xcprivacy`:
  - replace the commented-out UserDefaults block with a real `NSPrivacyAccessedAPICategoryUserDefaults` / `CA92.1` entry;
  - add `NSPrivacyTracking` `<false/>`;
  - change the template's header comment to explain that the app's `Preferences` is backed by `NSUserDefaults`.

  T010 passes.
- [ ] T014 [P] [US2] In `src/LoopMeet.App/Platforms/iOS/Info.plist`, add `ITSAppUsesNonExemptEncryption` `<false/>` and remove the whole `NSAppTransportSecurity` dict (research D5 and D6). T011 passes.
- [ ] T015 [US2] Run the full App suite (`dotnet test tests/LoopMeet.App.Tests`) and the iOS simulator build (`dotnet build src/LoopMeet.App -f net10.0-ios -r iossimulator-arm64`). Both pass, with no new warnings. T012 needs no code change: the files already comply, and the test pins them.

**Checkpoint**: US1 and US2 together produce an uploadable, review-ready build.

---

## Phase 5: User Story 3 - Testers install from TestFlight and the app works as on Android (Priority: P1)

**Goal**: a TestFlight install works on a physical iPhone and iPad, closing 013 rows 6–8 and 016 row 13.

**Independent Test**: [quickstart.md](quickstart.md) device checks 1–11.

- [ ] T016 [US3] Maintainer, one-time setup:
  - **Developer portal:** check the App ID capabilities, pick the certificate SHA-1, and create the App Store profile.
  - **OneSignal:** check the production push credentials.
  - **App Store Connect:** create the app record, enter App Information, Test Information and the demo account, and create the "Team" and "Invited" groups.
- [ ] T017 [US3] Maintainer: run `deploy/build-ios-release.sh -b 1`, upload with Transporter, and add the build to "Team". Record it in the quickstart's Uploads table.
- [ ] T018 [US3] Maintainer: run the quickstart device checks 1–11 on a TestFlight install (iPhone, and iPad where marked), and record the results in `specs/021-app-store-release/quickstart.md`.
- [ ] T019 [US3] Mark the closed rows in the earlier quickstarts, citing 021 and the build number:
  - `specs/013-uiscene-lifecycle/quickstart.md`: row 6 on iPhone and iPad, and rows 7–8;
  - `specs/016-account-deletion/quickstart.md`: row 13.
- [ ] T020 [US3] Maintainer: add the build to "Invited", submit it for Beta App Review, and record the outcome in the Uploads table (SC-002).

---

## Phase 6: User Story 4 - App Store privacy answers match the privacy policy (Priority: P2)

**Goal**: one table answers App Store Connect's App Privacy questions, kept in step with the policy.

**Independent Test**: every data type in `web/data-safety.md` and in the policy at `web/public/privacy/index.html` has an App Store row or a "Not collected" entry in `web/app-privacy.md`.

- [ ] T021 [P] [US4] Create `web/app-privacy.md`, the App Store counterpart of `web/data-safety.md`. It needs:
  - Apple's definitions: collected, linked, tracking;
  - the data-type table from research D8, with policy sections;
  - the "Not collected" list;
  - Tracking: No, with no App Tracking Transparency prompt;
  - a "Keep in step" section.
- [ ] T022 [US4] In `web/data-safety.md`, update the "Keep in step" section: a policy change also updates `web/app-privacy.md` and the App Store Connect App Privacy answers.
- [ ] T023 [US4] Run `deno test --allow-read=web,specs/016-account-deletion web/` and `deno lint web/` to confirm the web suite is unaffected.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T024 [P] In `CLAUDE.md`:
  - add a 021 line to Recent Changes;
  - add gotchas:
    - the iOS release signs by SHA-1 fingerprint (`LOOPMEET_IOS_SIGNING_IDENTITY`), because certificate names can repeat;
    - `web/app-privacy.md` changes with the privacy policy and `data-safety.md`;
    - new required-reason APIs go in `PrivacyInfo.xcprivacy`, and its test pins the list;
  - add `deploy/build-ios-release.sh` under Real commands.

  Edit by hand: `update-agent-context.sh` damages this file.
- [ ] T025 Run every suite (Api, App, Core, Infrastructure, `deno test` for `web/` and `supabase/functions/`, and `ci-scope.test.sh`) plus the Android Debug build, to confirm FR-011. Record the counts in the log below.
- [ ] T026 Update `~/.claude` memory (`project_store_release_roadmap`) with the TestFlight status after T017 and T020.

---

## Dependencies & Execution Order

- **Phase 1 (T001–T002):** independent of everything else. Do it first so CI covers the later changes.
- **Phase 2 (T003):** blocks the test tasks in US1 and US2.
- **US1 (T004–T009) and US2 (T010–T015):** the code is independent of each other. Both write to `IosAppStoreReleaseTests.cs`, so their test tasks run one after another, while the implementation tasks touch different files and can run in parallel.
- **US3 (T016–T020):** needs US1 and US2 merged, or at least built from this branch, and the maintainer's portal and App Store Connect setup. T020 follows T018.
- **US4 (T021–T023):** independent. It must be done before T016's App Privacy answers are entered.
- **Polish (T024–T026):** after US1, US2 and US4. T026 follows US3.

## Parallel Example

```text
After T003:
  T004, T005 (script tests, one file, in sequence)  ‖  T021 (web/app-privacy.md)
Then:
  T007 (.gitignore)  ‖  T013 (PrivacyInfo.xcprivacy)  ‖  T014 (Info.plist)  ‖  T008 (script)
```

## Implementation Strategy

1. **Automatable code, one PR:** Phases 1–2, US1, US2, US4 and T024–T025.
2. **Maintainer steps after merge:** US3 T016–T018. Internal testing is the MVP: SC-001 is met once a TestFlight build installs on an iPhone.
3. **External testing:** T020, then a docs PR with the recorded results (T019 and the Uploads table), as 017 did.

## Execution Log

| Task | Date | Notes |
| --- | --- | --- |
| | | |
