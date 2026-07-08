# Feature Specification: Reliable Sign-In Sessions & Startup Check

**Feature Branch**: `010-fix-auth-session`
**Created**: 2026-07-08
**Status**: Draft
**Input**: User description: "Fix the authentication and session experience so users are not unexpectedly signed out during normal use and are never stuck unable to log back in. Today, users intermittently lose their signed-in status mid-session -- the app stops showing their data and eventually forces them back to the login screen -- and afterward they usually cannot sign back in (with email/password, Google, or Apple) without fully force-quitting and relaunching the app. There should be no fixed session timeout; if a hard limit is unavoidable, the session should last as long as technically possible and use a sliding expiration that renews automatically every time the user actively uses the app, including returning to the app after it was backgrounded, so an active user is never logged out. When a session does need to end (expired, revoked, or refresh failed), the app must consistently and fully sign the user out everywhere -- clearing all cached credentials and profile data, not just navigating away -- and must always let them sign back in immediately using any supported method, on the same screen, without needing to restart the app. Separately, on every app launch, instead of briefly flashing the login screen before deciding whether the user is actually signed in, the app should show a clear checking-your-session loading state (indicator plus status text) while it determines sign-in status in the background, then go directly to either the login screen or the signed-in home screen -- never both."

## User Scenarios & Testing *(mandatory)*

<!--
  IMPORTANT: User stories should be PRIORITIZED as user journeys ordered by importance.
  Each user story/journey must be INDEPENDENTLY TESTABLE - meaning if you implement just ONE of them,
  you should still have a viable MVP (Minimum Viable Product) that delivers value.
-->

### User Story 1 - Stay Signed In While Actively Using the App (Priority: P1)

As a returning user, I want the app to keep me signed in for as long as I keep using it -- across single sessions, app backgrounding, and multi-day usage -- so that I'm never unexpectedly dropped to the login screen and locked out of my groups, meetups, and invitations while I'm an active user.

**Why this priority**: This is the core complaint -- unexplained, disruptive loss of authenticated state is the most damaging bug because it interrupts active use and erodes trust in the app.

**Independent Test**: Can be fully tested by signing in, using the app intermittently over an extended period (including backgrounding and returning to it many times over hours/days), and confirming the user is never forced back to the login screen while remaining an active user.

**Acceptance Scenarios**:

1. **Given** a signed-in user with the app backgrounded for a period shorter than any hard session ceiling, **When** they bring the app back to the foreground, **Then** their session is silently renewed and they see their data without any re-authentication prompt.
2. **Given** a signed-in user who opens and uses the app at least once within each rolling time window that would otherwise cause expiration, **When** their usage pattern continues indefinitely, **Then** the user is never forced to sign in again purely due to elapsed time.
3. **Given** a signed-in user actively viewing Home, Groups, Invitations, or Profile, **When** normal app usage continues, **Then** none of these screens spontaneously stop showing the user's data or bounce the user to the login screen.

---

### User Story 2 - Always Able to Sign Back In Immediately (Priority: P2)

As a user who has been signed out (for any reason), I want to be able to sign back in right away using whichever method I prefer (email/password, Google, or Apple) without needing to force-quit and relaunch the app, so a sign-out is never a dead end.

**Why this priority**: Even with better session handling, sign-outs will still happen occasionally (revocation, device changes, explicit logout). When they do, the app becoming unable to re-authenticate the user is the most severe consequence of the current bug and must never happen.

**Independent Test**: Can be fully tested by forcing a sign-out (manually logging out, or letting a session end) and then immediately attempting to sign back in with each supported method in turn, confirming every attempt succeeds on the first try without restarting the app.

**Acceptance Scenarios**:

1. **Given** a user was just signed out, **When** they attempt to sign back in with email/password, **Then** the sign-in completes normally and they land on the home screen.
2. **Given** a user was just signed out, **When** they attempt to sign back in with Google, **Then** the sign-in completes normally and they land on the home screen.
3. **Given** a user was just signed out, **When** they attempt to sign back in with Apple, **Then** the sign-in completes normally and they land on the home screen.
4. **Given** a user's previous sign-in attempt was interrupted or abandoned (e.g., they backgrounded the app mid-sign-in), **When** they return and try to sign in again, **Then** the new attempt is not blocked by the earlier interrupted one.

---

### User Story 3 - Consistent, Complete Sign-Out Everywhere (Priority: P3)

As a user whose session has ended, I want every part of the app to recognize that consistently and fully sign me out (not just kick me off one screen while others still act as if I'm signed in, and not just show empty content while pretending everything is fine), so I always know clearly when I need to sign in again and never see stale data.

**Why this priority**: Inconsistent handling today means the app's behavior on session loss depends on which screen the user happens to be on -- this must be unified so the fix in P1/P2 is trustworthy everywhere, not just on some screens.

**Independent Test**: Can be fully tested by forcing a session to end while positioned on each main screen (Home, Groups, Invitations, Profile) in turn and confirming identical behavior: the user is taken to the login screen and no cached credentials or personal data remain accessible.

**Acceptance Scenarios**:

1. **Given** a user's session has ended while they are on the Home screen, **When** the app next tries to load their data, **Then** they are taken to the login screen rather than shown an empty or "no data" state that masks the sign-out.
2. **Given** a user's session has ended while they are on the Groups or Invitations screen, **When** the app detects this, **Then** the user is taken to the login screen and all locally cached credentials and profile data are cleared.
3. **Given** a session-ending condition occurs, **When** it is handled, **Then** the outcome (redirect to login, full data clear) is the same regardless of which screen triggered the detection.

---

### User Story 4 - Clear Status While the App Checks Sign-In State on Launch (Priority: P4)

As a user opening the app, I want to see a clear "checking your session" indicator instead of a flash of the login screen, so I'm not confused about whether I'm signed in while the app figures it out.

**Why this priority**: This is a separate, lower-severity usability issue (confusing but not blocking) compared to the session-loss and re-login failures above, but it's part of the same overall "trustworthy sign-in state" experience the user asked for.

**Independent Test**: Can be fully tested by cold-launching the app both as a previously signed-in user and as a signed-out user, and confirming in each case that a single loading state is shown until the correct destination (home or login) appears, with no flash of the other screen.

**Acceptance Scenarios**:

1. **Given** a previously signed-in user with a renewable session, **When** they launch the app, **Then** they see a loading indicator with status text while the app checks their session, then go directly to the home screen -- the login screen is never shown.
2. **Given** a signed-out user (or one whose session cannot be renewed), **When** they launch the app, **Then** they see the same loading indicator with status text, then go directly to the login screen -- the home screen is never shown.
3. **Given** the session check is slow (e.g., poor connectivity), **When** the user waits, **Then** the loading state remains visible with its status text rather than the app appearing frozen or blank.

---

### Edge Cases

- The app is force-quit or the device restarts while a session is in the middle of being renewed -- the next launch must still resolve to a definite, correct signed-in or signed-out state rather than a permanently broken one.
- The user has no network connectivity at launch or while backgrounded -- the app must not incorrectly treat "can't reach the network" the same as "session is invalid" and must not strand the user on the checking-session state indefinitely.
- The device's clock is significantly wrong relative to the server -- session validity decisions must still behave sensibly.
- The user rapidly switches the app to the background and back multiple times in quick succession -- this must not trigger repeated redundant renewal attempts or conflicting navigation.
- The user has an unsaved action in progress (e.g., filling out a new meetup or group form) when their session ends -- the sign-out must not silently destroy their in-progress input without at least the same care given to other error conditions.
- The user previously signed in with one method (e.g., Google) and later attempts to sign back in with a different method (e.g., Apple or email/password) using the same identity -- sign-back-in must not fail or create a duplicate/conflicting account.
- A session-ending condition is detected while the user is offline -- the user should still end up in a clear, recoverable state once connectivity returns, rather than stuck.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST NOT sign out an actively-using user purely due to elapsed time; if a hard session ceiling is technically unavoidable, it MUST be set to the longest duration technically supported.
- **FR-002**: The system MUST automatically renew/extend a signed-in user's session on active use, including each time the app returns to the foreground after being backgrounded, so continued use never results in expiration.
- **FR-003**: The system MUST treat "session ended" the same way everywhere in the app -- every screen MUST use the same detection and response behavior rather than each screen deciding independently.
- **FR-004**: When a session ends for any reason (expiration, revocation, failed renewal, or explicit sign-out), the system MUST fully clear all locally cached credentials and personal profile data as part of that sign-out.
- **FR-005**: The system MUST NOT mask a session-ending condition by silently showing an empty or "no data" state; a session ending MUST always result in the user being routed to the login screen.
- **FR-006**: After any sign-out (user-initiated or forced), the system MUST allow the user to immediately attempt to sign back in again from the login screen, using any supported sign-in method, without restarting the app.
- **FR-007**: An interrupted or abandoned sign-in attempt (with any provider) MUST NOT block or break a subsequent sign-in attempt.
- **FR-008**: On app launch, the system MUST show a loading/checking state -- combining a progress indicator and descriptive status text -- while it determines whether the user is currently signed in.
- **FR-009**: The system MUST NOT display the login screen and the home screen in succession for the same app launch; only the correct destination, once determined, MUST be shown.
- **FR-010**: The behaviors above (session persistence, renewal, consistent sign-out, immediate re-sign-in, and startup checking state) MUST apply uniformly across all supported sign-in methods: email/password, Google, and Apple.
- **FR-011**: If the startup session check cannot complete promptly (e.g., due to connectivity issues), the system MUST keep the user informed via the loading state rather than appearing unresponsive, and MUST eventually resolve to a definite signed-in or signed-out destination.

### Key Entities *(include if feature involves data)*

- **User Session**: Represents a user's authenticated state over time, including how it is renewed through active use and how/why it ends.
- **Sign-In Attempt**: Represents a single attempt to authenticate via a specific method (email/password, Google, or Apple), including interrupted or abandoned attempts that must not affect later attempts.

## Assumptions

- "As long as technically possible" for session duration means the maximum lived session duration the underlying authentication provider supports, extended via sliding renewal on active use; this spec does not mandate a specific numeric duration.
- "Active use" for renewal purposes includes both in-app interaction and the app returning to the foreground from the background; it does not require the app to renew sessions while fully closed/terminated.
- Sign-back-in after a forced sign-out is expected to behave like any normal sign-in (including account creation for genuinely new identities), not a special-cased recovery flow.
- Finishing on-device validation of the existing Apple Sign-In feature (branch 009-apple-signin) is out of scope for this feature; however, since Apple Sign-In shares the same underlying sign-in/session behavior this feature changes, it must be re-checked against the requirements above before that feature is considered complete.
- Resolving the startup session check is expected to rely on both locally cached session state and a background connectivity/validity check, so that offline or slow-network launches still resolve rather than hang indefinitely.

## Privacy & Safety Considerations *(mandatory if user data or user-to-user features are involved)*

- **Data Minimization**: Sign-out clears all locally cached credentials and personal profile data; nothing a signed-out user shouldn't see should remain accessible on-device.
- **Default Visibility**: Group, meetup, invitation, and profile data must never be visible or fetched on behalf of a user whose session has actually ended.
- **Retention**: Session renewal extends how long a user stays signed in, but does not change what data is retained about them; existing data retention behavior is unchanged.
- **Safety Controls**: A consistent, complete sign-out (FR-003/FR-004) ensures that a session ending on a shared or lost device does not leave the previous user's data accessible.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Across a sample of realistic multi-day usage sessions, active users experience zero unexpected sign-outs while continuing to use the app within normal usage patterns.
- **SC-002**: 100% of sign-out events (forced or voluntary) are immediately followed by a successful sign-in on the user's first subsequent attempt, with no app restart required.
- **SC-003**: When a session ends, 100% of app screens reflect the signed-out state (redirect to login, no stale data) rather than a mix of behaviors across screens.
- **SC-004**: 100% of app launches show exactly one continuous loading state that resolves directly to either the login screen or the home screen, with zero observed cases of one screen flashing before the other.
- **SC-005**: The startup session check resolves to a definite screen within a few seconds under normal connectivity, and never leaves the user stuck on the checking-session state indefinitely even under poor connectivity.
