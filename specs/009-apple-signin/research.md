# Phase 0 Research: Sign in with Apple

## 1. Native API choice (iOS / MacCatalyst)

**Decision**: Use Apple's `AuthenticationServices` framework: `ASAuthorizationAppleIDProvider` + `ASAuthorizationController`, presented via `ASAuthorizationController.Delegate` callbacks bridged to a `TaskCompletionSource<ASAuthorizationAppleIDCredential>` in C#. The credential's `IdentityToken` (a JWT) is then exchanged for a Supabase session via `_client.Auth.SignInWithIdToken(Constants.Provider.Apple, idToken, rawNonce)`.

**Rationale**: 
- Apple App Store Review Guideline 4.8 mandates the native Sign in with Apple flow when an app offers any third-party social-login provider. Using a web-based OAuth redirect for Apple on Apple platforms is grounds for rejection.
- `AuthenticationServices` ships in Microsoft.iOS and Microsoft.MacCatalyst — no third-party MAUI NuGet package required.
- `Auth.SignInWithIdToken` is the Supabase-GoTrue C# method designed for native identity tokens. It accepts the Apple identity JWT, validates the nonce, creates or links the Supabase user, and returns a `Session` identical in shape to what the Google `ExchangeCodeForSession` flow already returns. The downstream `LoginViewModel` glue (profile lookup → create profile → cache → `HandleSuccessfulSignInAsync` → `Shell.Current.GoToAsync(SignedInTabs.HomeShellPath)`) is reusable unchanged.

**Alternatives considered**:
- *Web-based OAuth via `WebAuthenticator` (same as Google flow)*: rejected because Apple's review process will reject the app, and the UX is markedly worse than the native sheet on Apple devices. Allowed only on non-Apple platforms — but we don't offer Apple sign-in on non-Apple platforms at all.
- *Third-party MAUI wrappers (e.g. `Plugin.SignInWithApple`)*: rejected as unnecessary indirection over a stable Apple framework that Microsoft.iOS already binds. Adds maintenance surface and another version pin.

## 2. Nonce protocol

**Decision**: Generate a 32-byte cryptographically random nonce, SHA-256 hash it, send the hex-encoded hash to Apple in `ASAuthorizationAppleIDRequest.Nonce`, then send the *un-hashed raw nonce* to Supabase as the third argument of `SignInWithIdToken(provider, idToken, nonce)`. Supabase validates that `SHA256(nonce)` equals the `nonce` claim Apple embedded in the identity JWT.

**Rationale**: 
- Replay-attack prevention as documented by Apple ([Sign in with Apple > Verifying a User](https://developer.apple.com/documentation/sign_in_with_apple/sign_in_with_apple_rest_api/verifying_a_user)).
- Required by Supabase GoTrue for the Apple provider when using the native identity-token flow; sign-in fails with `nonce_validation_failed` otherwise.
- `RandomNumberGenerator.GetBytes(32)` + `SHA256.HashData(...)` are stdlib in .NET 10 — no extra dependency.

**Alternatives considered**:
- *Skip the nonce*: rejected. Supabase rejects native Apple identity tokens without a nonce in production.
- *Use a static nonce*: defeats replay protection; would also drift between client/server invocations.

## 3. Identity-merging behavior

**Decision**: Rely on Supabase's built-in `Auth.SignInWithIdToken` behavior, which automatically attaches the new Apple identity to an existing user whose verified email matches. No client-side merge logic is required. The `LoginViewModel` glue (already present for Google) reads the returned session token's user, calls `_usersApi.GetProfileAsync()`, and either updates the avatar (if no avatar was set yet) or upserts the profile from the OAuth fields — same as today's Google branch.

**Rationale**: 
- The Supabase project is already configured with Apple as an OAuth provider, which is the prerequisite for this server-side merging to fire. Confirmed in the spec input ("Supabase is already configured … no Supabase configuration changes are required").
- The Google flow validates this works for cross-provider email matches (`user@example.com` registered via email → Google sign-in on same email → same account). Apple behaves identically.

**Alternatives considered**:
- *Client-side merge via explicit `User.Identities.Add(...)` call*: rejected. Possible with Supabase JS, but the .NET SDK does not surface an equivalent today, and the automatic email-match merge covers every case in the spec.
- *Force the user to confirm the merge interactively*: rejected per the spec's Assumptions section ("no additional user confirmation step is required before the merge"). Matches Google flow's silent merge.

## 4. Display name preservation across re-authorizations

**Decision**: On every successful Apple sign-in, the `ASAuthorizationAppleIDCredential.FullName` is included only on the first authorization (Apple's well-documented policy). The viewmodel branch already in place — *"if profile exists, do not overwrite; if not, upsert with OAuth display name"* — naturally preserves the original display name. No code addition required. We only need to ensure the upsert-from-OAuth path uses the `FullName` claim when it is present and falls back to an empty string when it is not.

**Rationale**: 
- Mirrors the Google flow's exact behavior. Google also only returns the name on first consent; the existing viewmodel already handles the absence.
- Apple recommends capturing first-authorization names server-side at first use; the existing Supabase user-row is the canonical place for that, written through `_usersApi.UpsertProfileAsync(...)` in `TryCreateProfileFromOAuthAsync`.

**Alternatives considered**:
- *Cache the FullName locally and resend on subsequent sign-ins*: rejected. Apple's policy is that the name is captured once; doing client-side caching to "remember" it after first sign-in adds state without benefit because the server already has it from the first sign-in.

## 5. "Hide My Email" relay address treatment

**Decision**: Treat Apple's private relay address (`*@privaterelay.appleid.com`) as an ordinary verified email. The `email` claim and `is_private_email = true` claim are passed through unchanged to Supabase via the identity token. The Supabase user row stores whichever address Apple returns. Identity merging matches on the literal email string.

**Rationale**: 
- If a user signs in to LoopMeet with email/password using their real email first, then later signs in with Apple choosing "Hide My Email", the relay address will not match. Supabase will create a new account. This is the spec's documented behavior in edge cases (the user can subsequently choose to share their real email in Apple settings; that switches the relay off and surfaces the real address on the next sign-in). Attempting to resolve a relay address to its underlying real address would require Apple's private email relay management API (server-only) and is out of scope.
- Matches user expectation: choosing "Hide My Email" creates a privacy boundary; we honor it.

**Alternatives considered**:
- *Refuse Apple sign-in when the user picks "Hide My Email"*: rejected. Apple App Store Review Guideline 4.8 explicitly requires apps to permit the user's choice of private relay.

## 6. Entitlement, capability, and provisioning

**Decision**: Add `com.apple.developer.applesignin = [ "Default" ]` to both `Platforms/iOS/Entitlements.Debug.plist` and `Platforms/iOS/Entitlements.Release.plist`. No `Info.plist` change is required — the entitlement key alone carries the capability declaration for AuthenticationServices. The user must also enable **Sign in with Apple** on the `io.loopmeet.app` App ID in Apple Developer Portal and re-download both the iOS App Development and Ad Hoc / App Store provisioning profiles. That provisioning step is per-developer and is documented in `quickstart.md`, not committed to the repo.

**Rationale**: 
- The capability is declared in entitlements (entitlements file is what's signed and validated at install time, not Info.plist). The `Default` value enables the standard sign-in scope; alternative values (`Default-NotDeveloperAccount`, `WatchKitNotificationServiceExtension`) are not applicable here.
- Existing project structure already has per-config entitlements wired in `LoopMeet.App.csproj` via `<CodesignEntitlements>` — adding a key inside the existing files requires no MSBuild changes.

**Alternatives considered**:
- *Put the capability in `Info.plist`*: not how the iOS code-signing chain validates Sign in with Apple. Apple validates the entitlement at install/launch time; `Info.plist` would be a no-op.
- *Use a single entitlements file*: would require ignoring the existing per-config split (Debug uses `development` aps-environment, Release uses `production`). We already adopted the per-config split for push notifications; reusing both files keeps the pattern consistent.

## 7. Platform-conditional scope

**Decision**: Use `#if IOS || MACCATALYST` to guard:
- The entire body of `AuthService.SignInWithAppleAsync` (the cross-platform method signature exists on all targets; the implementation throws `PlatformNotSupportedException` on non-Apple builds and is never invoked because of the next point).
- The `AppleAuthCredentialProvider.cs` file in `Features/Auth/Platforms/Apple/` (compiled only on Apple targets; references `AuthenticationServices`).
- The `SignInWithAppleAsync` command method in `LoginViewModel` (entirely absent on non-Apple builds — the command property itself is never registered, the XAML binding silently no-ops on non-Apple).
- The `ShowAppleSignIn` property getter — returns `true` only on Apple targets; the XAML `IsVisible` binding then hides the button on non-Apple.

The "where possible" clause from the spec's constraint means: cross-platform XAML markup (one `<Button>` line) does land in the binary on every platform, but it has no Apple-code-reachable execution path on non-Apple. Apple-specific types (`ASAuthorizationAppleIDProvider`, etc.) are only referenced inside `#if` blocks and so do not appear in non-Apple build outputs.

**Rationale**: 
- Matches the project's established pattern (`AuthSessionService.cs:55` and `ProfileViewModel.cs:175` already use `#if ANDROID` / `#if MACCATALYST` directives).
- Achieves the spec's binary-level invisibility goal for service code and Apple-SDK references. The button's XAML markup remaining in non-Apple binaries is acceptable per the spec's "where possible" wording.

**Alternatives considered**:
- *Separate XAML pages per platform (e.g. `LoginPage.iOS.xaml`)*: rejected as new architectural pattern not used elsewhere in the project. Constitution Gate IV (Simplicity) and the spec's explicit instruction "do not invent a different approach" disqualify it.
- *Runtime `DeviceInfo.Platform` check*: rejected — the spec mandates compiler directives, not runtime checks, for service wiring and UI registration.

## 8. Session persistence

**Decision**: Reuse `AuthService.SaveAccessToken` / `Preferences.Default["loopmeet.auth.access_token"]` after Supabase returns the session. No new storage mechanism, no new key, no new file.

**Rationale**: 
- The Google flow uses exactly this mechanism; mirroring it satisfies the spec's "same local storage calls" requirement and Constitution Gate V (Modularity — no new shared concerns introduced).
- `MauiSessionPersistence` on top of the Supabase GoTrue client also continues to apply for the in-memory session lifecycle; the access-token cache is the recovery path on cold start.

**Alternatives considered**:
- *Use the device Keychain for Apple-derived tokens specifically*: rejected. The Supabase access token is the same format regardless of provider; segregating storage by provider would introduce inconsistency. (Future cross-cutting improvement: migrate all tokens to Keychain; out of scope.)

## 9. Test surface

**Decision**: Add one xUnit test file `tests/LoopMeet.App.Tests/Features/Auth/AppleSignInCommandSurfaceTests.cs` with two assertions:
1. `LoginViewModel` exposes a `SignInWithAppleCommand` property whose binding name matches the XAML (`SignInWithAppleCommand`) when the test is built on an Apple target, and is absent on non-Apple targets — done via `#if IOS || MACCATALYST` in the test itself.
2. The source file `LoginViewModel.cs` contains the expected `#if IOS || MACCATALYST` guard around the Apple command — verified by string-presence check on the source path (matching the existing pattern at `tests/LoopMeet.App.Tests/Services/Notifications/NotificationPermissionServiceTests.cs`, which uses source-text assertions).

**Rationale**: 
- The native Apple flow itself cannot be unit-tested off an iOS host (AuthenticationServices is iOS-only). The cross-platform surface (command registration + compile-time guards) is what's testable from the standard `net10.0` test host.
- Existing project pattern uses source-text assertions for similar compile-time guarantees (`NotificationPermissionServiceTests.cs:9`); we extend that pattern instead of introducing a new test style.

**Alternatives considered**:
- *Mock the native Apple flow*: rejected. The Apple SDK surface (`ASAuthorizationController`, delegates, `IdentityToken`) is not designed for mocking and the resulting test would assert MAUI scaffolding, not behavior.
- *No tests at all (matching the Google flow's current zero-tests state)*: rejected. Constitution Gate II requires tests for new behavior; we add the minimum testable surface without inflating the existing Google flow's untested state.

## Summary of resolved unknowns

There were no `NEEDS CLARIFICATION` markers in the spec, so Phase 0 research was used to lock in technical decisions for the implementation:

| Topic | Resolved Decision |
|---|---|
| Native API | `AuthenticationServices.ASAuthorizationAppleIDProvider` + Supabase `SignInWithIdToken(Provider.Apple, idToken, nonce)` |
| Nonce | Random 32-byte; SHA-256 hash to Apple; raw nonce to Supabase |
| Identity merge | Supabase server-side automatic merge on verified-email match |
| First-authorization name | Captured in `OAuthSignInResult.DisplayName`; preserved by viewmodel's existing "if profile exists, no overwrite" branch |
| Private relay address | Treated as ordinary verified email |
| Entitlement | `com.apple.developer.applesignin = ["Default"]` in both per-config entitlements files |
| Platform guard | `#if IOS || MACCATALYST` compiler directives, mirroring existing project pattern |
| Token persistence | Existing `Preferences["loopmeet.auth.access_token"]` |
| Tests | Source-text + command-surface assertions in `tests/LoopMeet.App.Tests/Features/Auth/` |
