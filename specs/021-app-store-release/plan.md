# Implementation Plan: App Store Release Build (TestFlight)

**Branch**: `021-app-store-release` | **Date**: 2026-11-12 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/021-app-store-release/spec.md`

## Summary

Get LoopMeet into TestFlight internal testing, then external testing, the iOS counterpart of 015.

**Deliverables:**
- **Release script:** `deploy/build-ios-release.sh -b <n>`. It builds an App Store–signed `.ipa` with `dotnet publish`, pins the signing identity by SHA-1, checks the provisioning profile against it before building, and verifies the build number and production push entitlement afterwards.
- **App changes for App Store Connect:**
  - Add the `UserDefaults` reason to the privacy manifest.
  - Declare that the app uses no non-exempt encryption.
  - Remove the unused plain-HTTP exceptions.
- **`web/app-privacy.md`:** App Store's App Privacy answers, kept in step with the privacy policy.
- **Quickstart:** App Store Connect and TestFlight setup, plus device checks. The device checks also close the rows left open in 013 and 016.

## Technical Context

**Language/Version**:

- zsh for the release script, like `build-android-release.sh`.
- XML property lists.
- C# 13 / .NET 10 for the source-inspection tests.

**Primary Dependencies**:

- .NET for iOS workload 27.2.10000 (Xcode 27.2), Microsoft.Maui.Controls 10.0.120.
- macOS `security`, `codesign`, `plutil` and `openssl`.
- Transporter or `xcrun altool` for upload.
- No new NuGet or npm packages.

**Storage**: None. No schema changes.

**Testing**:

- xUnit source-inspection tests in `LoopMeet.App.Tests`.
- `bash .github/scripts/ci-scope.test.sh`.
- Quickstart device checks on a TestFlight install.

**Target Platform**: iOS 15.0+ on iPhone (`ios-arm64`). iPad is out of scope: `UIDeviceFamily` is iPhone only, and iPads run the app in compatibility mode (research D11). Mac Catalyst is out of scope.

**Project Type**: Mobile app (MAUI) plus release tooling.

**Performance Goals**: n/a. The Release build keeps full AOT (research D2).

**Constraints**:

- No secrets or machine-specific paths in the repo.
- The Android build and every existing test must be unaffected.
- TestFlight builds expire after 90 days.

**Scale/Scope**:

- One script.
- Three app files: two plists, and the privacy manifest. The project already maps Release builds to the Release entitlements.
- One test class.
- Two documentation tables.
- CI scope mapping.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
| --- | --- | --- |
| I. Code Quality | Pass | The commented-out `UserDefaults` block in the privacy manifest becomes a real entry. The unused ATS exceptions are removed. The script follows `build-android-release.sh`'s structure. |
| II. Tests Required | Pass | `IosAppStoreReleaseTests` (source inspection, the accepted approach for native configuration) covers every FR the source can show. CI scope maps `deploy/*` and `.gitignore` so these tests, and 015's, run when those files change. The device-only behavior is covered by quickstart rows, which count as first-class coverage for native behavior. |
| III. User Experience | Pass | Testers get the same app as on Android. Beta App Review gets a demo account and a deletion path it can undo (research D9). |
| IV. Simplicity | Pass | No new abstractions. The script has no upload step, and there's no CI signing. The Debug-only partial plist was rejected because nothing uses it (D6). |
| V. Modularity | Pass | The iOS release lives in its own script, test class and docs. Android's are untouched. |
| VI. Contract-First | Pass | [contracts/build-ios-release.md](contracts/build-ios-release.md) defines the script's inputs, checks, messages and output before implementation. |
| VII. Observability | Pass | Every guard names what failed. The post-build check reports the actual build number and push environment. TestFlight crash reports are covered in the quickstart. |
| Privacy and retention | Pass | `web/app-privacy.md` maps every answer to a privacy-policy section and to 016's `data-retention.md`. Tracking: No. |

**Post-design re-check:** still all Pass. The design added no projects, packages or abstractions.

## Project Structure

### Documentation (this feature)

```text
specs/021-app-store-release/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── build-ios-release.md
├── checklists/
│   └── requirements.md
└── tasks.md             # /speckit.tasks
```

### Source Code (repository root)

```text
deploy/
└── build-ios-release.sh                         # new (FR-001, FR-002, FR-003)

src/LoopMeet.App/Platforms/iOS/
├── Info.plist                                   # + ITSAppUsesNonExemptEncryption, − NSAppTransportSecurity (FR-006, FR-007)
├── Entitlements.Release.plist                   # unchanged; asserted (FR-004)
└── Resources/PrivacyInfo.xcprivacy              # + UserDefaults CA92.1, NSPrivacyTracking false (FR-005)

web/
├── app-privacy.md                               # new (FR-010)
└── data-safety.md                               # "Keep in step" mentions app-privacy.md

tests/LoopMeet.App.Tests/Platforms/
└── IosAppStoreReleaseTests.cs                   # new

.gitignore                                       # + *.mobileprovision, *.p12, *.cer (FR-008)
.github/scripts/ci-scope.sh, ci-scope.test.sh    # deploy/* and .gitignore → app_tests
CLAUDE.md                                        # Recent Changes; gotchas (signing by SHA-1, app-privacy.md in step)
```

**Structure Decision**: same layout as 015. The script goes in `deploy/`, the platform files stay where MAUI expects them, and the source-inspection tests go in `tests/LoopMeet.App.Tests/Platforms/` next to `AndroidPlayReleaseTests` and `AppleSceneLifecycleTests`.

## Complexity Tracking

No violations.
