# Research: App Store Release Build (TestFlight)

## D1. Build command

**Decision:** `dotnet publish src/LoopMeet.App -c Release -f net10.0-ios -r ios-arm64` with these properties:

- `-p:CodesignKey=<SHA-1>`
- `-p:CodesignProvision=<profile UUID>`
- `-p:ApplicationVersion=<n>`
- `-o artifacts/ios/<n>`

Publishing an iOS device build produces a signed `.ipa`. The script runs `dotnet restore -r ios-arm64` and then `dotnet clean` first. The Android script cleans too, but iOS also needs the restore: `clean` never restores, and an IDE restore leaves out the `ios-arm64` target (NETSDK1047).

**Rationale:** this is the documented .NET for iOS App Store path, and it needs no Xcode project or archive step. `CodesignKey` goes to `codesign -s`, which accepts a SHA-1 fingerprint, so the duplicate "Apple Distribution: Joel Gray" name can't pick the wrong certificate (spec FR-001).

**Alternatives considered:**
- *`ArchiveOnBuild=true` with an Xcode Organizer upload:* adds a GUI step and an `.xcarchive` we don't otherwise need.
- *Signing by certificate name:* ambiguous when the name matches more than one keychain entry, as it does here (the same certificate in the login and System keychains).
- *Deleting the duplicate certificate:* this machine-specific fix doesn't protect the next machine. The quickstart still suggests removing an expired duplicate.

## D2. AOT and trimming

**Decision:** keep the iOS defaults: full AOT, and `SdkOnly` trimming for device Release builds. Don't copy the Android workaround that turns AOT and trimming off.

**Rationale:**
- iOS doesn't allow JIT, so device builds must be AOT. The Android `UnsatisfiedLinkError` is specific to Android.
- `SdkOnly` trims only the SDK and framework assemblies. The app, Supabase, Refit and OneSignal assemblies keep everything.
- The iOS SDK sets `JsonSerializerIsReflectionEnabledByDefault=true` (`Xamarin.Shared.Sdk.props:135`, workload 27.2.10000). Reflection-based System.Text.Json therefore keeps working, which Supabase 8, Refit and 020's `SessionStorageFormat` rely on.

**Alternatives considered:**
- *`MtouchInterpreter`:* slower, and nothing needs it.
- *Full trimming:* a risk with reflection-heavy libraries, for no benefit at this stage.

**Verification:** quickstart rows 1–4 on a TestFlight install exercise sign-in, the stored session and Api calls.

## D3. Signing inputs and checks

**Decision:** the script takes two environment variables:

- `LOOPMEET_IOS_SIGNING_IDENTITY`: the 40-character SHA-1 shown by `security find-identity -v -p codesigning`.
- `LOOPMEET_IOS_PROVISIONING_PROFILE`: the path to the App Store `.mobileprovision`.

Before building, it checks, and on failure names the problem:

1. `-b` is a positive integer.
2. Both variables are set, the identity has the SHA-1 form, and it is a valid codesigning identity in the keychain.
3. The profile exists and decodes with `security cms -D`. Then, on the decoded profile:
   - Its `application-identifier` ends in `.io.loopmeet.app`.
   - Its `aps-environment` is `production`.
   - It lists no `ProvisionedDevices`, which makes it an App Store profile.
   - It isn't expired.
   - Its `DeveloperCertificates` include the named identity.
4. It installs the profile as `<UUID>.mobileprovision` in both `~/Library/MobileDevice/Provisioning Profiles` and `~/Library/Developer/Xcode/UserData/Provisioning Profiles`, and passes the UUID as `CodesignProvision`.

After building, it reads the `.ipa`'s `CFBundleVersion` and its signed `aps-environment`, and fails if either is wrong.

**Rationale:**
- Neither value is a secret. The private key stays in the keychain. Environment variables keep machine-specific paths and fingerprints out of the repo, and they match 015's pattern.
- The checks catch the mismatches that otherwise surface as opaque codesign errors or as a TestFlight build without push (spec Edge Cases).
- The one profile on this Mac lives in `~/Library/MobileDevice/Provisioning Profiles`, but Xcode 16 and later read `~/Library/Developer/Xcode/UserData/Provisioning Profiles`. Installing into both works whichever folder this SDK reads, and costs one extra copy.

**Alternatives considered:**
- *A `.p12` file and password exported from the keychain:* only needed for CI, which is out of scope.
- *Automatic signing through Xcode:* doesn't apply to a `dotnet` CLI build.

## D4. Privacy manifest

**Decision:**
- Add `NSPrivacyAccessedAPICategoryUserDefaults` with reason `CA92.1` to `Platforms/iOS/Resources/PrivacyInfo.xcprivacy`, replacing the template's commented-out entry.
- Keep the three framework entries: `C617.1`, `35F9.1` and `E174.1`.
- Set `NSPrivacyTracking` to false, and leave the domain and collected-data keys empty.

**Rationale:**
- MAUI `Preferences` is backed by `NSUserDefaults`. The app stores the session, the profile cache and settings there, so App Store Connect would flag the missing reason (ITMS-91053).
- `CA92.1` covers data the app reads and writes only for itself.
- OneSignal ships its own manifest inside its framework.
- The collected-data answers go in App Store Connect (D8). The manifest's own collected-data keys are only required for third-party SDKs.

## D5. Export compliance

**Decision:** add `ITSAppUsesNonExemptEncryption` = `false` to the iOS `Info.plist`.

**Rationale:**
- The app uses only HTTPS and the system's cryptography. That's exempt, so no documentation is needed.
- Without the key, App Store Connect asks the question on every build before it can be tested.

**Alternatives considered:**
- *Answering in App Store Connect each time:* this is the manual step the spec removes.

Mac Catalyst's `Info.plist` is unchanged, because it's out of scope.

## D6. App Transport Security

**Decision:** remove the `NSAppTransportSecurity` dictionary from the iOS `Info.plist`.

**Rationale:**
- Every app configuration uses `https://api-staging.loopmeet.io` and the HTTPS Supabase URL (`MauiProgram.cs`).
- The `dev.loopmeet.io` and `localhost` exceptions only serve a commented-out local-backend block, so no build uses them.
- Removing them satisfies FR-007 with no build logic.

**Alternatives considered:**
- *A Debug-only partial app manifest that keeps the exceptions:* this adds a build mechanism for a setup nothing uses. If someone revives the local-backend block, they add the exception as a Debug-only `PartialAppManifest` at that point.

## D7. Upload

**Decision:** the script builds and verifies, but doesn't upload. The quickstart documents two ways to upload:

- **Transporter.app (primary):** sign in with the Apple ID, then drag in the `.ipa`.
- **`xcrun altool --upload-app -f <ipa> -t ios --apiKey <id> --apiIssuer <issuer>` (alternative):** uses an App Store Connect API key stored at `~/.appstoreconnect/private_keys/AuthKey_<id>.p8`, which `*.p8` keeps out of git.

**Rationale:**
- Uploading stays a manual step, as Play uploads were in 015.
- Transporter validates and reports problems without extra setup.

## D8. App Privacy answers

**Decision:** add `web/app-privacy.md`, the App Store counterpart of `web/data-safety.md`. It covers:

- Tracking: **No**.
- Every collected data type, with its purposes, whether it's linked to the user, and the privacy-policy section it comes from.

| App Store data type | Policy section | Linked to the user | Purposes |
| --- | --- | --- | --- |
| Contact Info → Name | 2, Account | Yes | App Functionality |
| Contact Info → Email Address | 2, Account; 2, What you create | Yes | App Functionality |
| Contact Info → Phone Number | 2, Account (optional) | Yes | App Functionality |
| User Content → Photos or Videos | 2, Profile photo | Yes | App Functionality |
| User Content → Other User Content | 2, What you create | Yes | App Functionality |
| Identifiers → User ID | 2, Account; 2, Sign in with Apple | Yes | App Functionality |
| Identifiers → Device ID | 2, Push notifications; 5 | Yes | App Functionality |
| Location → Precise Location | 2, Place search; 4 | No | App Functionality |
| Location → Coarse Location | 2, Place search; 4 (iOS lets the user allow approximate location instead) | No | App Functionality |
| Search History | 2, Place search | No | App Functionality |
| Usage Data → Product Interaction | 5, Push notifications (delivery and open records) | Yes | App Functionality |
| Diagnostics → Other Diagnostic Data | 2, Operational logs | Yes | App Functionality |

**Rationale:**
- Account, content, push and log data are kept and keyed to the user id (016 `data-retention.md`), so they're linked.
- Location and search text are used once, for place search, and never stored. However, they go to Google Places, a third party, so declaring them is the conservative choice and matches the Play answers. They're declared as not linked.
- There are no analytics or advertising SDKs, so the answer is no tracking, and the app doesn't ask for tracking permission.

**Keeping in step:** `data-safety.md`'s "Keep in step" section and the `CLAUDE.md` gotcha both add `app-privacy.md`. A policy change updates the policy, both tables and both store forms.

**Alternatives considered:**
- *Treating location and search as not collected under Apple's real-time exception:* this is defensible, but a reviewer comparing the answers with the policy would see a mismatch.

## D9. Beta App Review access

**Decision:**
- The reviewer gets an email and password demo account on staging, belonging to a group with an upcoming meetup.
- The test notes say that the Google and Apple buttons work with the reviewer's own accounts.
- They also say that Delete Account starts a 30-day grace period, which Restore undoes.

**Rationale:**
- Beta App Review needs a working sign-in it can use without any setup.
- Account deletion must be reachable (guideline 5.1.1(v)), and if the reviewer tries it, the restore path keeps the demo account usable.

## D10. Tests and CI scope

**Decision:**
- Add `tests/LoopMeet.App.Tests/Platforms/IosAppStoreReleaseTests.cs`, a source-inspection test in 015's style. It asserts:
  - the privacy manifest's four reasons, and that no entry is commented out;
  - `ITSAppUsesNonExemptEncryption` is false;
  - the plist has no ATS exception allowing plain HTTP;
  - `UIDeviceFamily` is iPhone only (D11);
  - the Release entitlements, which are production push and Sign in with Apple, and the project's mapping of Release builds to them;
  - the script's environment variables, identity pinning, build number, output folder and every pre-build guard;
  - the ignore rules.
- Add `*.mobileprovision`, `*.p12` and `*.cer` to `.gitignore`.
- Map `deploy/*` and `.gitignore` to `app_tests` in `.github/scripts/ci-scope.sh`, with cases in `ci-scope.test.sh`.

**Rationale:**
- The script and the plists can't run in CI (Linux), so source inspection is the accepted coverage (`CLAUDE.md`), and the device rows in the quickstart cover the rest.
- `ci-scope.sh` currently puts `deploy/*` and `.gitignore` in the "selects nothing" list, even though 015's `AndroidPlayReleaseTests` already reads both. A change to either file would skip the tests that guard it. `CLAUDE.md` requires mapping a path when tests read it.

**Alternatives considered:**
- *Running the zsh script under test:* the CI runners don't have zsh, and its guards need the macOS `security` tool.

## D11. iPhone only

**Decision:** `Info.plist`'s `UIDeviceFamily` lists only `1` (iPhone), from build 9. `IosAppStoreReleaseTests.InfoPlist_TargetsIPhoneOnly` pins it.

**Rationale:**
- With `2` (iPad) declared, App Store Connect requires 13-inch iPad screenshots, and the app's layouts aren't designed for iPad yet. The maintainer chose to ship iPhone-only and design for iPad later.
- iPads can still install the app and run it in iPhone compatibility mode.
- The App Store doesn't let an update drop a device family once a build has shipped with it. Adding iPad later is allowed, so this is the reversible direction.

**Alternative considered:** *keep iPad and upload iPad screenshots:* it would commit the app to iPad support with layouts that don't work well there.
