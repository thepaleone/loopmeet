# Quickstart: Sign in with Apple

End-to-end setup, build, and manual validation steps for Feature #009. Read this once you've reviewed [`spec.md`](./spec.md) and [`plan.md`](./plan.md) and are ready to either implement or validate.

## Prerequisites

- macOS with Xcode 26.5 or newer.
- .NET 10 SDK 10.0.300 or newer with `ios` + `maccatalyst` workloads (`dotnet workload list` should show `ios` at `26.5.10284` or newer).
- An iOS device or simulator on iOS 15+ (push-style notification permission prompts in this app expect iOS 13 minimum; Sign in with Apple itself is iOS 13+ but our `<SupportedOSPlatformVersion>` is set to `15.0`).
- An active **Apple Developer** membership tied to Team ID `7GQMSU8CT6` (or whichever team the existing iOS provisioning profiles use).
- The existing Supabase project with Apple OAuth provider already configured (verified in the spec).

## Apple Developer Portal one-time setup

1. Log into <https://developer.apple.com> → **Certificates, Identifiers & Profiles → Identifiers**.
2. Open `io.loopmeet.app`. Under **Capabilities**, check **Sign in with Apple**. Save.
3. → **Profiles**. Find the iOS App Development profile used by the dev/Debug build. **Edit** (then **Generate** — capability changes require regeneration). Download.
4. Repeat step 3 for the Ad Hoc / App Store profile used by Release.
5. Double-click both downloaded `.mobileprovision` files. They install into `~/Library/MobileDevice/Provisioning Profiles/`.

Optional sanity check:

```bash
ls ~/Library/MobileDevice/Provisioning\ Profiles/
security cms -D -i ~/Library/MobileDevice/Provisioning\ Profiles/<filename>.mobileprovision \
  | grep -B1 -A1 -E "Name|AppIDName|TeamName|com.apple.developer.applesignin"
```

Confirm the output lists `com.apple.developer.applesignin = ("Default")` and `AppIDName = LoopMeet` (or matching).

## Verify the Supabase side

The spec asserts this is already done, but a 30-second check:

1. <https://app.supabase.com> → your project → **Authentication → Providers → Apple**.
2. Confirm **Enabled** is on.
3. Confirm **Service ID** matches `io.loopmeet.app.signin` (or whichever Service ID was registered in Apple Developer Portal under **Identifiers → Services IDs**).
4. Confirm the `.p8` private key was uploaded and the **Key ID** + **Team ID** match Apple Developer Portal.

If any of these are missing or stale, the Apple identity token will fail validation server-side with `invalid_request: identity token verification failed`. No client retry will fix that.

## Build and run

### iOS simulator (cannot exercise Apple sign-in end-to-end, but builds + boots)

```bash
xcrun simctl boot "iPhone 17 Pro Max"   # or any iOS 26.x simulator already created
dotnet build src/LoopMeet.App/LoopMeet.App.csproj \
    -c Debug -f net10.0-ios -t:Run \
    -p:RuntimeIdentifier=iossimulator-arm64
```

The "Continue with Apple" button is present; tapping it on the simulator will present the Sign in with Apple sheet — but the simulator's Apple ID session is typically a Beta sandbox account, and behavior can be flaky. Real-device testing is recommended for the actual flow.

### iOS device

```bash
# UDID from Xcode → Window → Devices and Simulators → your iPhone → Identifier
dotnet build src/LoopMeet.App/LoopMeet.App.csproj \
    -c Debug -f net10.0-ios -t:Run \
    -p:Device=<your-iphone-udid>
```

If the build fails with **"Could not find any available provisioning profiles"**, you missed the profile regeneration in step 3 of the Apple Developer Portal setup above. Regenerate the profile and re-install.

### Android & Windows (verify Apple sign-in is absent)

```bash
./deploy/deploy-android.sh -c Release       # default device
# Windows build (if Windows host available):
# dotnet build src/LoopMeet.App/LoopMeet.App.csproj -c Release -f net10.0-windows10.0.19041.0
```

The login screen on Android/Windows must show only **Sign In**, **Continue with Google**, and **Create Account** — no Apple button. If you see an Apple button on Android or Windows, the `ShowAppleSignIn` binding is wrong and the `#if` guard is misplaced.

## Manual validation matrix

Execute these on a real iPhone signed in with an Apple ID:

| # | Scenario | Setup | Expected outcome |
|---|---|---|---|
| 1 | First-time Apple user, real email shared | Fresh install. Tap Sign in with Apple. Choose "Share My Email". | New LoopMeet account created with that email. Land on Home page. |
| 2 | First-time Apple user, Hide My Email | Fresh install. Tap Sign in with Apple. Choose "Hide My Email". | New LoopMeet account created with `*@privaterelay.appleid.com` email. Land on Home page. |
| 3 | Returning Apple user, app relaunch | After #1 or #2, force-quit. Relaunch. | Skip login screen; arrive directly at Home page. |
| 4 | Link to existing email account | Pre-create a LoopMeet account with email/password using `joel@…`. Sign out. From login screen, choose Sign in with Apple with an Apple ID whose verified email is `joel@…`. | Same original account opens (groups, meetups, display name intact). Now also accepts Apple. |
| 5 | Link to existing Google account | Pre-create a LoopMeet account with Google sign-in. Sign out. From login screen, choose Sign in with Apple with the same verified email. | Same original account opens. All three providers (email/Google/Apple) now reach the same account. |
| 6 | Apple flow cancelled | On the native Sign in with Apple sheet, tap "Cancel". | Return to login screen, no error toast, button re-enabled, other sign-in methods still available. |
| 7 | Apple service unreachable | Toggle airplane mode mid-flow, then tap Sign in with Apple. | Humane error message shown ("Apple sign-in failed. Please try again."). Login screen intact, other methods available. |
| 8 | Subsequent Apple sign-in (no email returned) | Sign out from a previously-Apple-linked account. Tap Sign in with Apple again. | Apple does not re-share email/name; user reaches the same existing account via the stable Apple `sub` claim. |
| 9 | Apple button absent on non-Apple | Build Android Release per `./deploy/deploy-android.sh -c Release` and Windows Release. Open login screen. | Apple button is not visible, not greyed out, not present in any tab order. |
| 10 | Binary inspection (non-Apple) | After Android build, run `strings src/LoopMeet.App/bin/Release/net10.0-android/io.loopmeet.app.apk` and grep for `ASAuthorization` and `AppleAuthCredentialProvider`. | No matches. (The string "Continue with Apple" may appear because it's in XAML compiled into the APK — that's expected and acceptable per the spec's "where possible" wording.) |

## Logs to watch

Mirroring the Google flow's logging conventions:

```
LoginViewModel: Starting Apple sign-in.
AuthService: <internal Supabase log if any>
LoginViewModel: <success silent>      or       LoginViewModel: Apple sign-in failed. ...
```

On Apple devices, the system also writes AuthenticationServices events to the Console.app log under the `com.apple.AuthenticationServices` subsystem when in dev mode — useful when the native sheet fails to present (capability mismatch, etc.).

## Rollback

If a critical defect surfaces post-merge:

1. Hide the button without removing the implementation: change `ShowAppleSignIn` to return `false` unconditionally. The button is now invisible on all platforms; service code remains compiled on Apple but unreachable from the UI.
2. Full rollback: revert the feature branch's merge commit. The `Platforms/iOS/Entitlements.*.plist` change is the only one with code-signing implications — the entitlement key, if left in place after rollback, is benign on a profile that supports Sign in with Apple, but if you re-revoke the capability in Apple Developer Portal you should also remove the entitlement key locally.
