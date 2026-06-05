# Feature Specification: Sign in with Apple

**Feature Branch**: `009-apple-signin`
**Created**: 2026-06-05
**Status**: Draft
**Input**: User description: "Add Apple Sign-In as a third authentication path on iOS and macOS, mirroring the existing Google OAuth flow. Apple Sign-In must use the native Sign In with Apple API, be invisible to Android and Windows users at the binary level via #if IOS || MACCATALYST guards, share account-linking behavior with the Google flow (merge identities when verified email matches an existing email or Google account), and persist sessions via the same local storage mechanism. The sign-in/sign-up screen presents the Apple button alongside Google and email only on Apple platforms. Supabase is already configured as an Apple OAuth provider."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Sign in with Apple as a New User (Priority: P1)

A first-time visitor on an iPhone or Mac opens LoopMeet and chooses Sign in with Apple. They authenticate with Face ID / Touch ID / device passcode, optionally choose whether to share their real email or use Apple's private relay, and land on the Home page logged in. A LoopMeet account is created from their Apple identity. On subsequent app launches they remain signed in without re-prompting.

**Why this priority**: Sign in with Apple is required by Apple Store guidelines for any iOS app that offers third-party social login, and unblocks a large segment of Apple-using prospects who avoid email/password forms. Without this, every Apple user is forced through the email flow.

**Independent Test**: Install the app fresh on an iOS or macOS device with no existing LoopMeet account, tap the Apple button, complete the native Apple prompt, confirm the user lands on the Home page with a profile populated from the Apple identity. Force-quit and relaunch the app — the user is still signed in.

**Acceptance Scenarios**:

1. **Given** a user has never used LoopMeet, **When** they tap **Sign in with Apple** on an iOS device and complete the native Apple prompt with a real email shared, **Then** a new LoopMeet account is created using that email, the session is persisted locally, and the user is taken to the Home page.
2. **Given** a user has never used LoopMeet, **When** they tap **Sign in with Apple** and choose Apple's "Hide My Email" option, **Then** a new account is created using Apple's relay email and the user is signed in.
3. **Given** a user signed in with Apple in the previous session, **When** they relaunch the app, **Then** they bypass the sign-in screen and arrive directly on the Home page.

---

### User Story 2 - Link an Apple Identity to an Existing Account (Priority: P2)

A user who originally registered with email/password (or with Google) on a different device or earlier session now signs in with Apple on an Apple device using the same verified email address. The system recognizes the matching email and merges the Apple identity into the existing account so the user keeps all their groups, meetups, and history; subsequently any of the three sign-in methods (email, Google, Apple) opens the same account.

**Why this priority**: Mirrors how Google sign-in already behaves and prevents the common "I made a duplicate account by accident" support ticket. Important for retention but only relevant once a user has more than one auth method.

**Independent Test**: Create an account with email/password. Sign out. From the sign-in screen, choose Sign in with Apple using an Apple ID whose verified email matches the email used at email-registration. Confirm the user lands in the original account (same groups, same meetups, same display name) — not a new empty one. Sign out and sign back in via email/password — same account, now with both identities attached.

**Acceptance Scenarios**:

1. **Given** a LoopMeet account exists for `user@example.com` registered via email, **When** the same person signs in with Apple using an Apple ID whose verified email is `user@example.com`, **Then** the existing account is opened (with all prior data) and the Apple identity is now attached to it.
2. **Given** a LoopMeet account exists for `user@example.com` registered via Google, **When** the same person signs in with Apple using that same verified email, **Then** the existing Google-registered account is opened and now accepts Apple as a third sign-in method.
3. **Given** an account is linked to all three identities (email, Google, Apple), **When** the user later signs in via any one of them, **Then** they reach the same account every time.

---

### User Story 3 - Apple Sign-In Is Hidden on Non-Apple Platforms (Priority: P3)

A user on Android or Windows opens the LoopMeet sign-in screen. The Apple button is not visible, not greyed out, and not reachable by any deep link or in-app navigation. They see only the existing email and Google options. The app's installed binary on those platforms contains no Apple sign-in UI or related service wiring.

**Why this priority**: Compliance with Apple's policy that Sign in with Apple is offered only on Apple platforms (it's also operationally simpler and prevents user confusion). Lower priority because it's an absence rather than a feature, but still verifiable.

**Independent Test**: Build and install the Android APK and the Windows MSIX, open the sign-in screen on each, and confirm visually that only email and Google buttons appear. Inspect the installed binary's assemblies / resources and confirm no Apple sign-in classes or strings are present. On iOS and macOS the same screen shows the Apple button alongside the other two.

**Acceptance Scenarios**:

1. **Given** an Android device, **When** the sign-in screen is shown, **Then** no Apple button or related UI element is rendered.
2. **Given** a Windows device, **When** the sign-in screen is shown, **Then** no Apple button or related UI element is rendered.
3. **Given** an iPhone or Mac, **When** the sign-in screen is shown, **Then** the Apple button is rendered alongside Google and email options.
4. **Given** a non-Apple build of the app, **When** the binary is inspected, **Then** no Apple sign-in service classes, type names, or string literals are present.

---

### Edge Cases

- **Apple "Hide My Email" relay address**: A user signs in with Apple while choosing to hide their real email. The relay address is treated as a normal verified email — used for account creation when no match exists, and for linking when a match exists. Future changes to that relay (Apple disabling it, etc.) follow the same rules as a bouncing real email.
- **Apple ID provides no email**: Some Apple ID configurations return only a stable user identifier with no email at all (e.g., second-time sign-ins where the user previously declined to share email). The system uses the stable Apple user identifier to find the existing LoopMeet account; if none exists, a new account is created with no email on file (display name pulled from Apple's full-name claim when available).
- **User cancels mid-flow**: User taps the Apple button, then dismisses the native Apple prompt. The sign-in screen returns to its initial state with no error toast and no partial account state. The user can try again or pick a different method.
- **Apple ID email matches a soft-deleted or banned LoopMeet account**: System refuses the sign-in with a clear message: "This account isn't currently available. Contact support if you believe this is in error."
- **Apple service is unreachable or returns an error**: User sees a user-understandable error ("Couldn't reach Apple to sign in. Try again, or sign in with email."), the screen returns to its initial state, and other sign-in options remain available.
- **Apple credential is later revoked at the OS level** (user removes the app's Apple ID access in Settings → Apple ID → Sign in with Apple): the persisted LoopMeet session continues to work as long as the local session token is valid; if it expires, the next sign-in attempt re-prompts.
- **First-time Apple sign-in provides a full name; subsequent ones don't**: Apple only returns a user's name on first authorization. The display name captured during first sign-in is preserved on the account; later sign-ins with the same Apple ID do not overwrite the display name with empty values.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The sign-in/sign-up screen MUST present a Sign in with Apple option alongside the existing email and Google options on iOS and macOS.
- **FR-002**: The Sign in with Apple option MUST NOT appear, be reachable, or have any binary presence on Android or Windows builds.
- **FR-003**: Signing in with Apple MUST use Apple's native Sign in with Apple flow (Face ID / Touch ID / device passcode), not a web browser redirect, on Apple platforms.
- **FR-004**: When a user signs in with Apple for the first time and the Apple-provided verified email does not match any existing LoopMeet account, the system MUST create a new account from that Apple identity and sign the user in.
- **FR-005**: When a user signs in with Apple and the Apple-provided verified email matches an existing LoopMeet account (registered via email or Google), the system MUST attach the Apple identity to that existing account rather than create a duplicate. After this attachment any of the three methods MUST open the same account.
- **FR-006**: When a user signs in with Apple and Apple returns no email (e.g., a returning user who declined to share email), the system MUST use the stable Apple user identifier to locate any existing account and sign in to it if found, or create a new account with no email on file if not.
- **FR-007**: Successful Apple sign-in MUST persist the resulting session locally so that the next app launch on the same device skips the sign-in screen.
- **FR-008**: If the user cancels Apple's native prompt, the sign-in screen MUST return to its initial state with no error toast, no partial account, and no navigation away.
- **FR-009**: If Apple's sign-in service is unreachable or returns an error, the system MUST show a user-understandable error message, keep the user on the sign-in screen, and leave email and Google options available.
- **FR-010**: An account with an Apple identity attached MUST be removable through the same account-deletion path as accounts with email or Google identities. Removing the account MUST also remove the Apple identity link.
- **FR-011**: The user's display name captured during the first Apple sign-in MUST be preserved across subsequent Apple sign-ins, even when Apple omits the name claim on those subsequent sign-ins.
- **FR-012**: An account that is soft-deleted, suspended, or otherwise non-current MUST reject Apple sign-in with the same message and behavior used by the existing Google and email flows for the same state.

### Key Entities

- **Apple Identity**: A linkage between a LoopMeet account and an Apple ID. Captured fields: a stable Apple user identifier (the canonical key), a verified email (which may be a real address, a private relay address, or absent), and the user's display name as provided by Apple at first authorization. One account may have at most one Apple identity attached at a time, and zero or more email/Google identities.
- **LoopMeet Account**: The existing user account, unchanged in shape, that now may have an Apple identity attached in addition to (or instead of) the existing email and Google identities. Account identity-merging rules already established by the Google flow apply: matching verified email = same account.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: First-time users on Apple devices who choose Sign in with Apple complete account creation in under 15 seconds end-to-end (tap to Home page) in 95% of attempts.
- **SC-002**: 100% of Sign-in-with-Apple attempts whose Apple-provided email matches an existing LoopMeet account result in the existing account being opened — never a duplicate.
- **SC-003**: 100% of Android and Windows builds, inspected post-build, contain no Apple-sign-in UI elements, service classes, or related string literals.
- **SC-004**: 90% of users who sign in with Apple on first launch remain signed-in (no re-prompt) on every subsequent app launch over the next 30 days, until they explicitly sign out or are forcibly signed out by an account-side event.
- **SC-005**: Fewer than 1% of Sign-in-with-Apple attempts in the first 30 days post-launch produce an unhandled error visible to the user.

## Assumptions

- The existing Google sign-in implementation in the codebase establishes the canonical pattern for: service interface shape, error-handling style, post-sign-in navigation, local session persistence, and identity-merging semantics. Apple Sign-In mirrors this pattern; no new architectural patterns are introduced by this feature.
- Supabase is already configured to accept Apple as an OAuth provider (provider credentials, redirect URLs, etc.) and no Supabase-side configuration changes are required as part of this feature.
- Apple Developer Portal has the Sign in with Apple capability enabled on the `io.loopmeet.app` App ID, and a provisioning profile that includes that capability is available for signing iOS/macOS builds. Provisioning is a per-developer setup step and is not part of this feature's deliverables.
- macOS support is delivered via the MacCatalyst target (consistent with the existing project structure). A separate native macOS bundle is out of scope.
- Account-linking by verified email matches the Google flow's existing behavior; no additional user confirmation step is required before the merge.
- "Hide My Email" Apple relay addresses are treated as ordinary verified emails for the purposes of matching and account creation. The system does not attempt to resolve a relay address to its underlying real address.
- Analytics, telemetry, and logging are limited to whatever the Google sign-in flow already emits. No additional Apple-specific instrumentation is in scope for this feature.
