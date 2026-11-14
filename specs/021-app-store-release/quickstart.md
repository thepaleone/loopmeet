# Quickstart: First TestFlight Release

TestFlight has two tracks:

- **Internal testing:** members of your App Store Connect team, up to 100 people. There's no review, and builds are available minutes after processing.
- **External testing:** invited testers, up to 10,000, by email or public link. The first build of each version goes through Beta App Review.

## One-time setup

### Developer portal (developer.apple.com → Certificates, Identifiers & Profiles)

1. **App ID:** open Identifiers → `io.loopmeet.app`. Check that **Push Notifications** and **Sign in with Apple** are enabled.
2. **Certificate:** "Apple Distribution: Joel Gray" is listed twice because the same certificate is in both the login and System keychains (one SHA-1, `4803FF3E…`). List the identities with their fingerprints:
   ```sh
   security find-identity -v -p codesigning
   ```
   Use that SHA-1. If two different fingerprints ever appear, pick the newest valid one.
3. **Profile:** under Profiles, create a new profile:
   1. Choose Distribution → **App Store Connect**.
   2. Select App ID `io.loopmeet.app` and the certificate from step 2.
   3. Name it `LoopMeet App Store` and download it.
   4. Keep the file outside the repo. `*.mobileprovision` is git-ignored anyway.

### OneSignal

TestFlight builds register with Apple's production push environment, so OneSignal needs production push credentials.

1. Open OneSignal → Settings → Push & In-App → Apple iOS.
2. If it uses a `.p8` auth key (token-based), it already covers production and sandbox, so there's nothing to do.
3. If it uses a `.p12` certificate, replace it with a `.p8` key from the portal (Keys → Apple Push Notifications service).

### App Store Connect (appstoreconnect.apple.com)

1. **App record:** Apps → + → New App:
   - Platform: iOS.
   - Name: LoopMeet.
   - Bundle ID: `io.loopmeet.app`.
   - SKU: `loopmeet-ios`.
   - Primary language: English (U.S.).
2. **App Information:**
   - Privacy Policy URL: `https://loopmeet.io/privacy/`.
   - Category: Social Networking.
3. **App Privacy:** answer from `web/app-privacy.md`. Tracking: No.
4. **TestFlight → Test Information:**
   - Beta App Description: "LoopMeet helps small groups plan meetups: create a group, invite friends by email, schedule meetups with a place and time, and get notified when plans change."
   - Feedback email: your support address.
   - Privacy Policy URL: `https://loopmeet.io/privacy/`.
   - Sign-in required: yes. Enter the demo account (below).
   - Review notes: "Sign in with the demo account (email and password). Google and Apple sign-in also work with your own accounts. Profile → Delete Account starts a 30-day grace period. Restore on the next screen undoes it, so the demo account stays usable."
5. **Demo account:** on staging, create an email and password account that has a display name and belongs to a group with at least one upcoming meetup.
6. **Groups:**
   - Internal: TestFlight → Internal Testing → +, named "Team". Add yourself and other team members.
   - External: TestFlight → External Testing → +, named "Invited". Testers are added after the first build passes review.

## Build

```sh
export LOOPMEET_IOS_SIGNING_IDENTITY=<40-character SHA-1 from security find-identity>
export LOOPMEET_IOS_PROVISIONING_PROFILE=~/path/to/LoopMeet_App_Store.mobileprovision
./deploy/build-ios-release.sh -b <n>
```

- **Before building:** the script checks the profile against the identity, and stops with a message if anything doesn't match.
- **Output:** the `.ipa` lands in `artifacts/ios/<n>/`.
- **Build numbers:** use the next unused number for every upload. iOS and Android are kept in step: build 8 matched Play's build 8, and build 9 is the first iPhone-only build (research D11).

## Upload

- **Transporter:** install Transporter.app from the Mac App Store, sign in with your Apple ID, drag in the `.ipa`, then Deliver.
- **Command line** (optional, needs an App Store Connect API key with the Developer role, saved as `~/.appstoreconnect/private_keys/AuthKey_<KEY_ID>.p8`):
  ```sh
  xcrun altool --upload-app -f artifacts/ios/1/*.ipa -t ios --apiKey <KEY_ID> --apiIssuer <ISSUER_ID>
  ```

Processing takes 5–30 minutes, and App Store Connect emails you when the build is ready or if it found a problem. The build should not ask about export compliance, because the app declares `ITSAppUsesNonExemptEncryption = NO`.

**Internal:** add the build to "Team". Testers install the TestFlight app and accept the invitation.

**External:** add the build to "Invited" and submit it for Beta App Review. Once it's approved, add testers by email or turn on the public link.

## Uploads

| Build | Version | Date | Track | Result |
| --- | --- | --- | --- | --- |
| | | | | |

## Device checks (TestFlight install on a physical iPhone)

| # | Check | Expected | Result |
| --- | --- | --- | --- |
| 1 | Fresh install from TestFlight, then launch | Sign-in screen, no crash | |
| 2 | Sign in with email and password, then force-quit and relaunch | Home with groups and meetups, still signed in | |
| 3 | Sign in with Google | The browser sheet returns to the app signed in | |
| 4 | Sign in with Apple (closes 013 row 6 on iPhone) | The Apple sheet appears over the app, and the app returns signed in | |
| 5 | Profile → change photo → library, then camera | The picker and the camera open after their permission prompts, and the avatar uploads | |
| 6 | Create meetup → search for a place with location allowed | Nearby results first | |
| 7 | Allow notifications. Another member creates a meetup in your group | A push arrives, which proves the production push environment and OneSignal credentials | |
| 8 | Tap that push with the app in the background, then again with it closed (closes 013 rows 7–8) | The app opens. Opening the meetup itself is #18, so record what happens without failing the row | |
| 9 | Settings → General → About, or the Profile version label | `0.1.8` (`<build>`), the project's `ApplicationDisplayVersion` | |
| 10 | Sign in with Apple, request deletion, then force the purge (016 row 13) | An `apple_authorizations` row exists after sign-in. After the purge, LoopMeet is gone from Settings → Apple ID → Sign in with Apple | |
| 11 | Profile → Delete Account → Restore, as the reviewer would | The account comes back, and the demo data is intact | |

For row 10, force the purge the way 016's quickstart does. Use a throwaway Apple ID account, not the demo account.

## If something fails

- **Codesign: "no identity found" or "ambiguous":** check that `LOOPMEET_IOS_SIGNING_IDENTITY` is a SHA-1 from `security find-identity -v -p codesigning`, not the certificate's name.
- **Processing email ITMS-91053 (missing API declaration):** a required-reason API is missing from `Platforms/iOS/Resources/PrivacyInfo.xcprivacy`. Add the category it names, with its approved reason, and add it to `IosAppStoreReleaseTests`.
- **No pushes on TestFlight while development builds get them:** OneSignal is missing production credentials. See the OneSignal section above.
- **Launch crash only on the TestFlight build:** a trimming or AOT problem. Get the crash log from TestFlight → Crashes, or from Xcode → Devices.
