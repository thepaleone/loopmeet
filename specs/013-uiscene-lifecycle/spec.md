# Feature Specification: Scene Lifecycle for iOS and Mac Catalyst

**Feature Branch**: `013-uiscene-lifecycle`  
**Created**: 2026-10-30  
**Status**: Draft  
**Input**: User description: "Adopt the UIKit scene lifecycle on iOS and Mac Catalyst so the app launches when built with Xcode 27. Apple requires apps built with the Xcode 27 / iOS 27 SDK to adopt the scene lifecycle or they crash at launch. Add a scene delegate and scene manifest on both Apple platforms, update the UI framework to a version that supports it, raise the Mac Catalyst minimum OS, and confirm that launch, foreground session refresh, sign-in, and notification taps keep working."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - The app opens on iPhone, iPad and Mac (Priority: P1)

A LoopMeet user taps the app icon on an iPhone or iPad running iOS 27, or opens it on a Mac, and the app starts normally into the screen they expect (sign-in for a signed-out user, Home for a signed-in user). Under Apple's new platform rules, a build made with the current Apple toolchain that has not adopted the scene lifecycle crashes immediately at launch, so without this change the next iOS or Mac release cannot ship at all.

**Why this priority**: Nothing else in the app is usable if it cannot launch. This is the reason the feature exists.

**Independent Test**: Install a build made with the current Apple toolchain on an iOS 27 device and on a Mac; cold-launch it signed out and signed in, and confirm it reaches the right first screen without crashing. Launch the Android build and confirm it is unchanged.

**Acceptance Scenarios**:

1. **Given** the app is fully closed on an iOS 27 device, **When** the user taps the app icon, **Then** the app opens to the sign-in screen (signed out) or Home (signed in) without crashing.
2. **Given** the app is fully closed on a Mac, **When** the user opens it, **Then** it opens to the same first screen as on iOS without crashing.
3. **Given** the app is in the background, **When** the user switches back to it, **Then** it returns to the screen they left.
4. **Given** an Android device, **When** the user launches the new build, **Then** behavior is unchanged from the previous release.

---

### User Story 2 - Signed-in users stay signed in and can sign in again (Priority: P2)

A signed-in user who leaves the app in the background and comes back later finds their session refreshed and their data loading, exactly as before. A signed-out user can sign in with Google or with Apple and lands in the app.

**Why this priority**: The launch fix changes how the app is told it is being opened, foregrounded and handed sign-in results. Sign-in and session refresh are the flows most likely to break silently, and a user who cannot sign in cannot use the app.

**Independent Test**: On an iOS 27 device and a Mac, sign in with Google, sign out, sign in with Apple; background the app long enough for the session to need refreshing, return, and confirm data loads without being asked to sign in again.

**Acceptance Scenarios**:

1. **Given** a signed-out user, **When** they choose Sign in with Google and complete it in the browser sheet, **Then** they return to the app signed in.
2. **Given** a signed-out user, **When** they choose Sign in with Apple and approve it, **Then** they are signed in.
3. **Given** a signed-in user who backgrounded the app, **When** they return to it, **Then** the session is refreshed and lists load without a sign-in prompt.

---

### User Story 3 - Notification taps still open the right screen (Priority: P3)

A group member receives a push notification (for example a new meetup or a reminder) and taps it. The app opens to the screen the notification is about, whether the app was already running in the background or fully closed.

**Why this priority**: Notifications are how members learn about meetups, but a broken tap still leaves the app usable, so it ranks below launch and sign-in.

**Independent Test**: Send a test notification to an iOS 27 device with the app in the background, tap it, and confirm the destination; repeat with the app fully closed.

**Acceptance Scenarios**:

1. **Given** the app is in the background, **When** the user taps a meetup notification, **Then** the app opens to that meetup's group.
2. **Given** the app is fully closed, **When** the user taps a meetup notification, **Then** the app launches and opens to that meetup's group.

---

### Edge Cases

- **Service unavailable at startup**: when the app cannot reach the LoopMeet service and the user chooses Quit, the app closes as it does today rather than hanging.
- **Mac running an older macOS**: Macs below the new minimum version can no longer install updates; they keep the last version they installed.
- **Multiple windows**: the app keeps a single window per device. Requests to open a second window (for example from the iPad app switcher or the Mac Window menu) are not supported.
- **Updating over an older install**: a user updating from the previous release launches straight into their existing session without signing in again.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The iOS and Mac Catalyst apps MUST launch without crashing when built with the current Apple toolchain (Xcode 27 / iOS 27 SDK) and run on the current OS versions.
- **FR-002**: The app MUST declare and use the scene-based lifecycle on iOS and Mac Catalyst, with exactly one window and multiple windows disabled.
- **FR-003**: Existing startup, foreground, background, sign-in and notification behavior MUST be preserved; no user-visible behavior changes are intended.
- **FR-004**: Returning to the app from the background MUST still refresh the user's session before data loads.
- **FR-005**: Sign in with Google and Sign in with Apple MUST complete and return the user to the app signed in.
- **FR-006**: Tapping a push notification MUST open its destination both when the app is running in the background and when it was fully closed.
- **FR-007**: The Android app MUST be functionally unaffected.
- **FR-008**: The minimum supported Mac version MUST rise to what the current Apple toolchain requires (macOS 14 Sonoma, i.e. Mac Catalyst 17); the iOS minimum (iOS 15) is unchanged.
- **FR-009**: Automated checks MUST fail if the scene lifecycle configuration is removed or altered on either Apple platform, or if the UI framework drops below the version that supports it.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of cold launches on iOS 27 devices and on Macs succeed during release testing (zero launch crashes).
- **SC-002**: Every device-test scenario (launch signed in and out, background and return, Google and Apple sign-in, notification tap while running and while closed, Quit when the service is unavailable) passes on at least one iPhone, one iPad or simulator, and one Mac before release.
- **SC-003**: Android release testing shows no new failures compared with the previous release.
- **SC-004**: The next iOS and Mac builds can be produced and submitted with the current Apple toolchain.

## Assumptions

- Apple's requirement applies to builds made with the Xcode 27 SDK regardless of the device OS they run on, so the change ships in the next build for all iOS and Mac users.
- The UI framework provides the scene support needed (Microsoft's guidance: version 10.0.110 or later); the project moves to the latest stable patch, 10.0.120.
- iOS and Mac Catalyst builds cannot be produced on the development machine until a .NET workload that supports Xcode 27.2 is released; device verification happens after that, using quickstart.md.
- Raising the Mac minimum to macOS 14 is acceptable: it is required by the toolchain, and staying on the older toolchain would block all future iOS and Mac releases.
- No backend, database, or feature changes are part of this work.
- **Out of scope**: multi-window support, any container or deployment pipeline changes, and the developer's local build-warning suppression in the app project file.
