# Feature Specification: App Store Release Build (TestFlight)

**Feature Branch**: `021-app-store-release`  
**Created**: 2026-11-12  
**Status**: Draft  
**Input**: User description: "Begin the publish process to the Apple App Store, the iOS counterpart of 015 (Google Play release build). The first audience is invited testers through TestFlight: internal testing first, then external testing, mirroring Play internal then closed testing."

## Background

The iOS app has only ever been run from the IDE, on a simulator or on a device with a development profile. The App Store needs an `.ipa` signed for App Store distribution, a build number that rises with every upload, a privacy manifest that declares every required-reason API the app uses, and an answer to the export-compliance question. The bundle ID is `io.loopmeet.app`, the same as the Android package name (015).

Apple's private track is TestFlight. **Internal testing** (up to 100 members of the App Store Connect team, no review) matches Play internal testing. **External testing** (up to 10,000 invited testers by email or public link, after Beta App Review) matches Play closed testing. This feature reaches internal testing and then external testing.

The work 015 did for Android already covers some of this, and so do 013, 016 and 017:

- **015:** the shared version scheme.
- **013:** the scene lifecycle.
- **016:** in-app account deletion, and revoking the Sign in with Apple token.
- **017:** the privacy policy and the deletion website.

The Api and Supabase project are still the staging ones; a production environment is out of scope.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A maintainer produces an uploadable build with one command (Priority: P1)

A maintainer sets the signing variables and runs `deploy/build-ios-release.sh -b <build-number>`. They get an App Store–signed `.ipa` that App Store Connect accepts. They never type a secret on the command line or store one in the repo.

**Independent Test**: Run the script with the distribution identity and profile. Confirm the `.ipa` has the given build number and the project's display version, is signed by the named identity, and carries the production push entitlement and the Sign in with Apple entitlement.

**Acceptance Scenarios**:

1. **Given** the signing variables are set, **When** the script runs with `-b 1`, **Then** it writes an `.ipa` under `artifacts/ios/1/` signed for App Store distribution.
2. **Given** the keychain holds more than one certificate with the same name, **When** the script runs, **Then** it signs with the identity the maintainer named, not whichever one the keychain returns first.
3. **Given** a missing variable, a profile file that doesn't exist, or a non-numeric build number, **When** the script runs, **Then** it stops before building and names what is missing.
4. **Given** the `.ipa` is uploaded, **When** App Store Connect processes it, **Then** it appears under TestFlight with no validation errors and no export-compliance prompt.

### User Story 2 - The build passes App Store Connect checks and Beta App Review (Priority: P1)

The app declares what it does accurately, so processing and Beta App Review don't stop it for a missing privacy reason, encryption declaration, or insecure network exception.

**Independent Test**: Inspect the built app's privacy manifest, `Info.plist` and entitlements. Upload a build and confirm it processes without warnings by email.

**Acceptance Scenarios**:

1. **Given** the app stores data in the system preferences store, **When** the privacy manifest is inspected, **Then** it declares that API category with an approved reason, alongside the ones the framework needs.
2. **Given** the app uses only standard HTTPS, **When** the `Info.plist` is inspected, **Then** it declares that the app uses no non-exempt encryption.
3. **Given** a Release build, **When** the `Info.plist` is inspected, **Then** it allows no plain-HTTP exceptions. Debug builds may keep the exceptions for local development.
4. **Given** a reviewer opens the external build, **When** they follow the test information, **Then** they can sign in with the demo account and reach Home, Groups, meetups and account deletion.

### User Story 3 - Testers install from TestFlight and the app works as on Android (Priority: P1)

An invited tester installs LoopMeet from TestFlight on an iPhone. They sign in with Google or Apple, receive push notifications, and can delete their account.

**Independent Test**: Run the quickstart device checks on a TestFlight-installed build on a physical iPhone.

**Acceptance Scenarios**:

1. **Given** a TestFlight install, **When** the tester launches it, **Then** it opens to sign-in or Home with no crash.
2. **Given** a TestFlight install, **When** the tester signs in with Apple, **Then** the Apple sheet appears over the app, and the tester returns signed in. This closes the 013 row that needed a device.
3. **Given** the tester allowed notifications, **When** a meetup is created in their group, **Then** a push arrives through the production push environment.
4. **Given** a tester signed in with Apple requests account deletion and the grace period ends, **When** the deletion runs, **Then** the app's Apple authorization is revoked. This closes 016 row 13.

### User Story 4 - App Store privacy answers match the privacy policy (Priority: P2)

A maintainer answers App Store Connect's App Privacy questions from one table, kept in step with the privacy policy, the same way `web/data-safety.md` does for Play.

**Independent Test**: Compare the table with the privacy policy and with `web/data-safety.md`. Every data type the policy names has an App Store answer.

**Acceptance Scenarios**:

1. **Given** the privacy policy, **When** the App Privacy answers are entered from the table, **Then** each collected data type, its purposes, and whether it's linked to the user match the policy.
2. **Given** the app has no analytics or advertising SDKs, **Then** the table answers "no tracking", and the app does not ask for tracking permission.

### Edge Cases

- **Build numbers** are never reused. App Store Connect rejects a repeat of the same version and build number, and a rejected or expired build still consumes its number. iOS and Android numbers may diverge, since each store checks only its own.
- **The display version** must increase once an external build of that version has passed Beta App Review and a later build needs review again. Apple allows more builds under the same version, so this is a maintainer choice, not a requirement.
- **Expiry:** TestFlight builds expire after 90 days. Testers need a newer build before then.
- **Push environment:** a development-signed build registers with the sandbox push environment, and a TestFlight build with production. OneSignal must have the production APNs credentials, or TestFlight testers get no pushes.
- **Expired or missing signing assets:** an expired distribution certificate or profile fails signing. The script reports it and does not produce an unsigned build.
- **Apple sign-in credentials:** Sign in with Apple on the web uses the Services ID `io.loopmeet.web`, and the app uses `io.loopmeet.app`. Supabase's Apple Client IDs already list both (017), so no change is expected.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The repo MUST provide `deploy/build-ios-release.sh` that builds a Release `.ipa` of `io.loopmeet.app` signed for App Store distribution. It MUST take the build number as an argument. It MUST take the signing identity and the provisioning profile from environment variables, and MUST pin the identity by its SHA-1 fingerprint, never by name, so a duplicate certificate name can't select the wrong one.
- **FR-002**: The script MUST stop before building, naming the problem, when a required variable is unset, the profile file is missing, or the build number isn't a positive integer.
- **FR-003**: The build MUST use the version scheme from 015. `ApplicationDisplayVersion` becomes `CFBundleShortVersionString`, and the build number becomes `CFBundleVersion`.
- **FR-004**: Release builds MUST use the production push entitlement and keep the Sign in with Apple entitlement.
- **FR-005**: The privacy manifest MUST declare every required-reason API category the app and its frameworks use, including the system preferences store.
- **FR-006**: The `Info.plist` MUST declare that the app uses no non-exempt encryption.
- **FR-007**: Release builds MUST NOT include App Transport Security exceptions that allow plain HTTP.
- **FR-008**: Release output (`artifacts/`), certificates, private keys, App Store Connect API keys and provisioning profiles MUST be git-ignored.
- **FR-009**: A quickstart MUST cover:
  - App Store Connect setup: the app record, TestFlight test information, the beta app description, the demo account for review, and the internal and external tester groups.
  - Uploading the build, with Transporter or the command line.
  - Device checks on a TestFlight install, including the 013 device rows and 016 row 13.
- **FR-010**: The repo MUST hold a table of App Store App Privacy answers that maps each answer to a privacy-policy section, kept in step with the policy the same way `web/data-safety.md` is.
- **FR-012**: The app MUST declare iPhone as its only device family, so App Store Connect asks for no iPad screenshots. iPad support waits until the layouts are designed for it.
- **FR-011**: Android builds and every existing automated test MUST be unaffected.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The first build processes in App Store Connect with no errors and no export-compliance prompt, and installs from TestFlight internal testing on a physical iPhone.
- **SC-002**: An external TestFlight build passes Beta App Review on its first or second submission.
- **SC-003**: Every quickstart device check passes on a TestFlight install. The exception is the known push-tap behavior, which is tracked as #18.
- **SC-004**: 100% of existing automated tests still pass. New tests guard the privacy manifest, the encryption declaration, the Release network exceptions, the script's argument and variable checks, and the ignore rules.

## Assumptions

- The maintainer has a paid Apple Developer Program membership and an App Store Connect team, and a valid Apple Distribution certificate is in the keychain.
- The maintainer creates the App Store provisioning profile in the developer portal and keeps it outside the repo. Its App ID has Push Notifications and Sign in with Apple enabled.
- OneSignal has, or will be given, production APNs credentials.
- The demo account for Beta App Review is an email and password account on the staging backend.
- Uploading stays a manual step, with Transporter or `xcrun altool`. Scripting the upload with an App Store Connect API key is optional.
- Out of scope:
  - Mac Catalyst distribution.
  - iPad as a supported device family (FR-012).
  - The public App Store release: screenshots, the store listing and App Review.
  - A production backend.
  - CI-built releases.
  - Bug #18, where tapping a push opens the app but not the meetup.
  - The CA1422 `UIWindow` fallback warning.
