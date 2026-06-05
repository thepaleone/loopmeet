# Implementation Plan: Sign in with Apple

**Branch**: `009-apple-signin` | **Date**: 2026-06-05 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/009-apple-signin/spec.md`

## Summary

Add Sign in with Apple as a third authentication path on iOS and MacCatalyst (the project's "Apple platforms"), structurally identical to the existing Google OAuth flow. Use Apple's native AuthenticationServices `ASAuthorizationAppleIDProvider` rather than a web redirect (Apple platform requirement), exchange the resulting identity token for a Supabase session via `Auth.SignInWithIdToken(Provider.Apple, idToken, nonce)`, persist the access token through the existing `Preferences` mechanism, and let Supabase's built-in identity-by-verified-email merging handle the account-linking case. The Apple sign-in button appears on the existing `LoginPage` only on Apple targets, and all Apple-specific service code is excluded from Android and Windows binaries via `#if IOS || MACCATALYST` compiler directives.

## Technical Context

**Language/Version**: C# 13 / .NET 10 (MAUI client). No backend changes.
**Primary Dependencies**: Microsoft.Maui.Controls 10.0.70, Supabase 1.1.1 + Supabase.Gotrue, AuthenticationServices framework (Microsoft.iOS / Microsoft.MacCatalyst — already part of those workloads, no new NuGet package).
**Storage**: None new. Sessions persist via the existing `MauiSessionPersistence` + `Preferences.Default["loopmeet.auth.access_token"]` mechanism. Supabase stores the identity linkage server-side; no Supabase schema changes are required because Supabase Apple OAuth provider is already configured.
**Testing**: xUnit. New tests live in `tests/LoopMeet.App.Tests/Features/Auth/` mirroring the patterns of any Google-flow tests present there (none exist for Google sign-in today, so per the non-requirements section of the spec, we add tests only where they're testable cross-platform — payload assembly and view-model command wiring; the native Apple flow itself is not unit-testable without an iOS host).
**Target Platform**: iOS 15+, MacCatalyst 15+ (Apple sign-in present); Android 21+, Windows 10.0.17763+ (Apple sign-in absent).
**Project Type**: Mobile-app (MAUI multi-target) + existing Supabase backend (unchanged).
**Performance Goals**: Tap-to-Home under 15 s on Apple devices for new users (SC-001); identity merge correctness 100% by verified email (SC-002).
**Constraints**: Apple Sign-In on Apple platforms is required by App Store policy when any third-party login is offered. Implementation must use the native API (not a web redirect). Apple-related code must not be present in Android / Windows binaries (use compiler directives, not runtime platform checks, for service wiring and UI registration).
**Scale/Scope**: Single new authentication path on top of the two existing ones (email, Google). Touches one service file, one viewmodel, one view, one model file (reuse), one iOS Info.plist key, one Apple Developer Portal capability (per-developer environment setup, out-of-source).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Gate I — Code Quality**: PASS. Plan reuses existing abstractions (`AuthService`, `LoginViewModel`, `OAuthSignInResult`, `AuthCoordinator`). No new architectural patterns introduced. The only new file is the Apple credential request helper, scoped behind `#if IOS || MACCATALYST`.
- **Gate II — Tests Are Required**: PASS, with the explicit non-requirement carved out by the spec mirroring the Google flow (which itself ships without unit tests today). Tests are added at the layer where they exist: a compile-time guard test on the LoginPage view model command surface, and a payload-assembly test if any logic is added on the non-platform side.
- **Gate III — UX First**: PASS. Acceptance scenarios cover the new-user path, the link-to-existing-account path, and the cancel/error path. Error messages mirror the Google flow's wording. The button appears only on Apple platforms.
- **Gate IV — Simplicity**: PASS. One service method, one viewmodel command, one button. No new dependency added; AuthenticationServices ships with Microsoft.iOS. Supabase's existing email-match identity merging is reused without modification.
- **Gate V — Modularity**: PASS. Apple-specific code goes in a single helper file (`Features/Auth/Platforms/Apple/AppleAuthCredentialProvider.cs`) compiled only on iOS/MacCatalyst. The cross-platform `AuthService.SignInWithAppleAsync` is a thin method that delegates to that helper inside the `#if` block.
- **Gate VI — Contract-First Interfaces**: PASS. Contracts captured in `contracts/apple-signin-contract.md`: the public `AuthService.SignInWithAppleAsync` method returns the existing `OAuthSignInResult`; the canonical Apple identity-token claim set is documented; nonce protocol and identity-merging trigger condition are explicit.
- **Gate VII — Observability & Reliability**: PASS. Logging mirrors the Google flow's `ILogger.LogInformation("Starting … sign-in.")` / `LogError(ex, "… sign-in failed.")` pattern. No new structured logs required beyond that.

No complexity violations to track.

## Project Structure

### Documentation (this feature)

```text
specs/009-apple-signin/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
│   └── apple-signin-contract.md
├── checklists/
│   └── requirements.md  # Spec quality checklist (already created)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/LoopMeet.App/
├── Features/Auth/
│   ├── AuthService.cs                              # ADD SignInWithAppleAsync (cross-platform method;
│   │                                               # implementation block guarded by #if IOS || MACCATALYST)
│   ├── ViewModels/LoginViewModel.cs                # ADD SignInWithAppleCommand + ShowAppleSignIn property;
│   │                                               # logic mirrors SignInWithGoogleAsync
│   ├── Views/LoginPage.xaml                        # ADD "Continue with Apple" button, IsVisible bound
│   │                                               # to ShowAppleSignIn (false on non-Apple targets)
│   └── Platforms/Apple/
│       └── AppleAuthCredentialProvider.cs          # NEW — native AuthenticationServices flow
│                                                   # entire file wrapped in #if IOS || MACCATALYST
└── Platforms/iOS/
    └── Entitlements.Debug.plist                    # ADD com.apple.developer.applesignin (Default)
    └── Entitlements.Release.plist                  # ADD com.apple.developer.applesignin (Default)
                                                    # (no Info.plist change needed; entitlement carries it)

tests/LoopMeet.App.Tests/
└── Features/Auth/
    └── AppleSignInCommandSurfaceTests.cs           # NEW — asserts that the LoginViewModel's
                                                    # SignInWithAppleCommand is wired and that
                                                    # ShowAppleSignIn reflects the build-target flag
```

**Structure Decision**: Mirror the existing `Features/Auth/` layout exactly. Add a single Apple-platform-only helper inside `Features/Auth/Platforms/Apple/` (a new sub-folder convention that documents intent; the file itself is compile-gated). Reuse `OAuthSignInResult` for the return shape — no new model. Reuse the existing `AuthSessionService.HandleSuccessfulSignInAsync` for post-sign-in side-effects (notification setup, device registration, profile timezone sync). Reuse `AuthCoordinator` for navigation. Reuse `Preferences` for token persistence.

## Post-Design Constitution Re-Check

*Re-evaluated after Phase 1 artifacts (`research.md`, `data-model.md`, `contracts/`, `quickstart.md`) were authored.*

- **Gate I — Code Quality**: PASS (unchanged). Phase 1 surfaced no new abstractions beyond the four files in the contract: `AuthService.SignInWithAppleAsync` (method on existing service), `AppleAuthCredentialProvider` (single helper, Apple-only), `AppleAuthNonce` (single helper, cross-platform stdlib), and the `LoginViewModel` command addition. No dead code, no commented-out code paths, no unresolved TODOs.
- **Gate II — Tests**: PASS (unchanged). One xUnit test file at `tests/LoopMeet.App.Tests/Features/Auth/AppleSignInCommandSurfaceTests.cs` covers the testable cross-platform surface. The native Apple flow is acknowledged as platform-bound and validated via the manual matrix in `quickstart.md`, matching the project's existing test-pragmatism level.
- **Gate III — UX**: PASS (unchanged). `data-model.md` state-transition diagram and `quickstart.md` validation matrix cover every acceptance scenario from `spec.md` including cancel, error, link-existing, and Hide-My-Email. Humane error message is reused verbatim from the Google flow's text (`"…sign-in failed. Please try again."`).
- **Gate IV — Simplicity**: PASS (unchanged). Phase 1 confirmed no new architectural patterns: same service class, same viewmodel pattern, same XAML pattern, same compiler-directive pattern (`#if IOS || MACCATALYST`), same `Preferences` persistence, same Supabase identity-merge behavior, same `AuthSessionService.HandleSuccessfulSignInAsync` post-sign-in dispatch.
- **Gate V — Modularity**: PASS (unchanged). The Apple-SDK boundary is hermetic to one file (`AppleAuthCredentialProvider.cs`). `AuthService` references AuthenticationServices only inside the `#if` block of one method. No circular dependencies.
- **Gate VI — Contract-First Interfaces**: PASS (strengthened). `contracts/apple-signin-contract.md` documents the public method signature, error contract, error mapping rules, helper interfaces, entitlement key, identity-merge trigger conditions, and Apple Developer Portal prerequisites. Implementation is bound to this contract; tests assert against it.
- **Gate VII — Observability & Reliability**: PASS (unchanged). Logs mirror the Google flow's `ILogger.LogInformation` / `LogError` calls verbatim. No new structured logging needed.

No new complexity violations to track.

## Complexity Tracking

No constitution gates require justification — the design adds one method, one command, one button, one Apple-only helper file, one cross-platform nonce helper, and one entitlement key, all mirrored from or built on the existing Google flow.
