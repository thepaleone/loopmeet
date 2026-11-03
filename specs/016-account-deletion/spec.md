# Feature Specification: Account Deletion with a 30-Day Grace Period

**Feature Branch**: `016-account-deletion`  
**Created**: 2026-11-02  
**Status**: Draft  
**Input**: User description: "Account deletion with a 30-day grace period. The Delete Account button in Profile currently shows 'Account deletion isn't available yet.' Make it work. Google Play requires it (and the App Store will too), and it must delete the account and all of the user's data. Typed DELETE confirmation listing owned groups; hide the user and everything they own from the moment of the request; let the user restore within 30 days after signing in, with only an explicit Restore cancelling; then permanently delete all of their data, their push-notification records and their Apple sign-in token. Request and cancel are Api operations a future web page (017) will also use; privileged access only for the permanent step; idempotent and retry-safe; structured logs keyed by user id, never email; privacy and retention rules documented. Out of scope: the website, the privacy policy text, transferring group ownership."

## Background

Google Play requires every app that lets people create an account to let them delete it from inside the app and from the web, and the App Store has the same rule. The Profile screen already has a Delete Account button, but it only says "Account deletion isn't available yet," which would fail store review. Feature 017 (the loopmeet.io site) will offer the same deletion from the web and publish the privacy policy, so this feature also produces the retention facts that policy will state.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Request account deletion from the app (Priority: P1)

A signed-in user opens Profile and taps Delete Account. A confirmation screen explains what will happen: their account and all of their data will be permanently deleted 30 days from now, and each group they own (listed by name with its member count) will be deleted along with its meetups, memberships and invitations. The Delete button stays disabled until they type `DELETE`. After confirming, they see the date their data will be permanently deleted, and they are signed out.

**Why this priority**: This is the store requirement and the core of the feature. With only this story, a user can ask for deletion and the request is recorded with a date.

**Independent Test**: Sign in as a user who owns a group with members, request deletion, and confirm the screen listed that group with the right member count, the button stayed disabled until `DELETE` was typed, the scheduled date is 30 days out, and the user ended up signed out.

**Acceptance Scenarios**:

1. **Given** a signed-in user who owns two groups (3 and 1 members), **When** they open the deletion screen, **Then** both groups are listed by name with "3 members" and "1 member", together with a statement that those groups, their meetups, memberships and invitations will be deleted.
2. **Given** a user who owns no groups, **When** they open the deletion screen, **Then** no group list is shown and the explanation covers only their own account and data.
3. **Given** the deletion screen, **When** the confirmation field holds anything other than exactly `DELETE` (including `delete`, or `DELETE` followed by a space), **Then** the Delete button is disabled.
4. **Given** `DELETE` has been typed, **When** the user taps Delete, **Then** the request is recorded, the user sees "Your account will be permanently deleted on {date}", and after acknowledging they are signed out on this device.
5. **Given** the request cannot be sent (offline or a server error), **When** the user taps Delete, **Then** they see an error, nothing changes, and they remain signed in.
6. **Given** the user is also signed in on another device, **When** that device next talks to the service after the request, **Then** it shows the "scheduled for deletion" screen (User Story 3) instead of continuing normally.

---

### User Story 2 - Everyone else stops seeing the user straight away (Priority: P1)

From the moment the request is recorded, to every other user it is as if the account no longer exists. Groups the user owns disappear, along with their meetups and pending invitations. The user disappears from the member lists of other groups, and invitations they sent disappear for the recipients. No push notifications are sent to the user, and nothing the user did causes notifications to others.

**Why this priority**: The user chose "hidden now". Without it, a requested deletion leaves the user's data on show for 30 days, which contradicts what they were told.

**Independent Test**: With user A pending deletion, sign in as user B (a member of a group A owns and of a group A belongs to) and confirm that A's owned group and its meetups are gone from B's home and group lists, A is missing from the other group's member list, A's pending invitation to B is gone, and no reminder for A's group's meetups reaches B.

**Acceptance Scenarios**:

1. **Given** user A is pending deletion and owns group G with member B, **When** B opens the app, **Then** G and its meetups do not appear anywhere for B: home, groups, group detail, upcoming and past meetups, or invitations.
2. **Given** A is pending deletion and is a member of group H owned by C, **When** C or any other member views H, **Then** A is not in H's member list or member count.
3. **Given** A sent B an invitation that B has not answered, **When** A becomes pending deletion, **Then** B no longer sees it, and accepting it through a stale screen fails as "no longer available".
4. **Given** A is pending deletion, **When** a meetup in a group A owns reaches its reminder time, or anything else would notify A, **Then** no notification is sent, to A or because of A.
5. **Given** B opens a link or screen for G (for example an old notification or a stale page), **When** the app loads it, **Then** B sees the same "not found" result as for a deleted group.

---

### User Story 3 - Change one's mind within 30 days (Priority: P2)

A user who is pending deletion signs in (on any device, with any of their sign-in methods). Instead of the app, they see "Your account is scheduled for deletion on {date}" with **Restore account** and **Sign out**. Restore account cancels the deletion and brings everything back exactly as it was. Sign out leaves the deletion scheduled. Signing in, or the app quietly refreshing a saved session, never cancels it on its own.

**Why this priority**: The grace period exists to undo mistakes; without this story it is only a delay.

**Independent Test**: Request deletion, sign in again and choose Sign out (still pending), sign in again and choose Restore (account active), then confirm the owned groups, meetups, memberships, invitations and profile are all back, visible to other members, and that notifications resume.

**Acceptance Scenarios**:

1. **Given** user A is pending deletion, **When** A signs in, **Then** the scheduled-deletion screen shows the permanent-deletion date and only Restore account and Sign out are available. No other screen can be reached.
2. **Given** that screen, **When** A taps Sign out, **Then** A is signed out and the deletion stays scheduled for the same date.
3. **Given** that screen, **When** A taps Restore account, **Then** the deletion is cancelled, A lands on Home, and A's profile, owned groups, meetups, memberships and invitations are all exactly as they were before the request.
4. **Given** A restored, **When** other members open the app, **Then** A's groups, meetups and membership reappear, and notifications to and from A resume for future events. Events that would have notified during the pending period are not sent afterwards.
5. **Given** the app holds a saved session from before the request, **When** it refreshes that session in the background, **Then** the deletion is not cancelled and the app shows the scheduled-deletion screen.
6. **Given** the permanent-deletion date has passed, **When** A tries to sign in or restore, **Then** restore is refused, and the account behaves as deleted even if the permanent deletion has not finished running yet.

---

### User Story 4 - Permanent deletion happens automatically after 30 days (Priority: P1)

When a request reaches its date, everything tied to the user is permanently deleted without anyone doing anything:

- their sign-in account and profile, and their avatar image
- groups they own, with those groups' meetups, memberships and invitations
- their memberships in other groups, and invitations they sent or received
- meetups they created in groups they don't own
- their registered devices, and their notification delivery and open records
- their record with the push-notification provider
- if they used Sign in with Apple, their Apple sign-in authorization, which is revoked

**Why this priority**: This is what "delete my account" promises and what both stores require. The grace period is only acceptable because this step reliably follows it.

**Independent Test**: Put a test user with an owned group, a membership elsewhere, an avatar, a device registration and sent and received invitations into pending deletion with the date already passed. Let the scheduled run happen, then confirm no record of the user remains in any of those places and the push provider no longer knows them. Run it again and confirm it finishes cleanly with nothing to do.

**Acceptance Scenarios**:

1. **Given** a pending request whose date has passed, **When** the scheduled run happens, **Then** within 24 hours of the date every item listed above is gone and the request is marked complete without keeping identifying data.
2. **Given** the run fails partway (for example the push provider is unreachable), **When** the next run happens, **Then** it carries on from where it stopped without errors, and the user stays hidden in the meantime.
3. **Given** a completed deletion, **When** the run happens again for the same user, **Then** nothing fails and nothing else changes.
4. **Given** the deleted user's email address, **When** someone later signs up or signs in with it, **Then** they get a brand-new, empty account with no link to the old data.
5. **Given** the user signed in with Apple, **When** permanent deletion completes, **Then** their Apple authorization for LoopMeet is revoked. A failed revocation is retried and reported rather than silently skipped.

---

### Edge Cases

- **Requesting twice** (for example from the app and later from the web page in 017): the original date stands. A second request returns the existing date and does not extend the grace period.
- **Restoring on the day itself**: the date is a precise moment, 30 × 24 hours after the request. Restore is allowed up to that moment and refused after it, whatever the device's time zone. The date shown to the user is in their local time.
- **Group owner and members both requesting deletion**: each request is handled independently. A group disappears when its owner is pending. A member's pending status only hides that member.
- **The owner restores after members have also left or been deleted**: the group comes back with whoever is still an active member.
- **Invitations addressed to the pending user's email from other groups**: they are hidden from the inviter's view while the user is pending. Invitations created after the request are deleted with the account at permanent deletion.
- **A member edits or deletes a meetup in a group owned by a pending user**: not possible, because the group is hidden from them. Server-side operations on hidden groups fail as "not found".
- **The push provider or Apple is down at permanent-deletion time**: the run retries on later runs. The account's own data is still deleted on schedule, and only the outstanding external step is retried. Each failure is logged.
- **An account with no profile row or no avatar** (an incomplete sign-up): deletion still completes.
- **Hidden-now timing**: other users already looking at a screen may keep seeing stale data until it refreshes. Any action they take on it fails as "not found".

## Requirements *(mandatory)*

### Functional Requirements

#### Requesting deletion

- **FR-001**: The Profile Delete Account action MUST open a confirmation screen that states the permanent-deletion date (30 days from now) and lists each group the user owns with its name and current member count, saying those groups and their meetups, memberships and invitations will be deleted.
- **FR-002**: The confirmation MUST only be possible after the user types exactly `DELETE` (case-sensitive, no surrounding whitespace).
- **FR-003**: On confirmation, the system MUST record a deletion request with the request time and a permanent-deletion time exactly 30 days later, show the user that date, and sign them out on the current device.
- **FR-004**: Requesting and restoring MUST be service operations performed as the signed-in user, usable by any client (the app now, the 017 web page later), and MUST NOT depend on anything specific to the mobile app.
- **FR-005**: A deletion request for a user who is already pending MUST succeed without changing the existing date.

#### Hidden now

- **FR-006**: While a user is pending deletion, every view other users have of the service MUST exclude: the user's profile; groups the user owns, with their meetups, memberships and invitations; the user's memberships in other groups (including member counts); invitations the user sent; and meetups the user created.
- **FR-007**: While a user is pending deletion, operations by other users on any of the hidden items MUST fail exactly as they would for items that do not exist.
- **FR-008**: While a user is pending deletion, the system MUST NOT send push notifications or reminders to that user, or about meetups and invitations in their hidden groups.
- **FR-009**: While a user is pending deletion, every signed-in session of that user MUST be limited to viewing the scheduled-deletion status, restoring, and signing out.

#### Restoring

- **FR-010**: When a pending user signs in, or a saved session is refreshed, the app MUST show "Your account is scheduled for deletion on {date}" with Restore account and Sign out, and nothing else.
- **FR-011**: Only an explicit Restore account action MUST cancel a pending deletion. Signing in, refreshing a session, and any other request MUST NOT cancel it.
- **FR-012**: Restoring MUST return the user and everything hidden under FR-006 to exactly its pre-request state, and MUST be refused once the permanent-deletion time has passed.
- **FR-013**: Notifications suppressed during the pending period MUST NOT be sent after a restore.

#### Permanent deletion

- **FR-014**: The system MUST automatically and permanently delete, no later than 24 hours after the permanent-deletion time: the user's sign-in account and identities; their profile; their avatar image; groups they own with all of those groups' meetups, memberships and invitations; their memberships in other groups; invitations they sent or received; meetups they created anywhere; their registered devices; and their notification delivery and open records.
- **FR-015**: Permanent deletion MUST remove the user's record from the push-notification provider.
- **FR-016**: For users who signed in with Apple, permanent deletion MUST revoke their Apple sign-in authorization.
- **FR-017**: Permanent deletion MUST be idempotent and resumable: re-running it after a partial failure MUST finish the remaining steps without errors, and running it for an already-deleted user MUST change nothing.
- **FR-018**: After permanent deletion, no record in the live system may identify the user. A completion marker MAY keep the opaque user identifier and timestamps only.
- **FR-019**: Elevated (administrative) access MUST be limited to the permanent-deletion step. Requesting, restoring, and the visibility rules MUST run with the signed-in user's own permissions. Where elevated access is used, and why, MUST be documented.

#### Observability and documentation

- **FR-020**: The system MUST write structured log entries for each deletion request, restore, refused restore, permanent-deletion start, completion, and each failed step (including external-provider failures). Each entry MUST be keyed by the opaque user identifier and MUST NOT contain the user's email address, name, or other profile content.
- **FR-021**: The feature MUST document the privacy and retention rules it implements: the 30-day grace period and what is hidden during it; exactly what is deleted and when; what, if anything, remains (completion markers, operational logs, provider backups), with how long each is kept. This is the source for the 017 privacy policy.

### Key Entities

- **Deletion request**: one per user. Holds the user's opaque identifier, when it was requested, the permanent-deletion time (request + 30 days), its state (pending, restored, completed) and, while completing, which steps are done. After completion it holds no identifying data.
- **Pending user**: a user with a pending deletion request. They are hidden from everyone else and limited to the status, restore and sign-out actions.
- **Owned group**: a group whose owner is the user. It is hidden while the owner is pending and deleted with them.
- **External records**: the user's push-notification provider record and, for Apple sign-in users, their Apple authorization. These are removed or revoked at permanent deletion.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can go from Profile to a confirmed deletion request in under 1 minute.
- **SC-002**: In testing, a deletion can never be confirmed without typing `DELETE`: 0 of 20 attempts with other input succeed.
- **SC-003**: Within 30 seconds of a request (the existing list cache lifetime), other users see none of the hidden items when they refresh, and 0 notifications reach or concern a pending user during the grace period.
- **SC-004**: After a restore, 100% of the user's groups, meetups, memberships and invitations from before the request are back and visible to the same people.
- **SC-005**: Within 24 hours of the permanent-deletion time, an audit of every store listed in FR-014 and FR-015 finds 0 records for the user. A second run for the same user completes with 0 errors.
- **SC-006**: 100% of deletion log entries carry the user identifier and 0 contain an email address or display name.
- **SC-007**: The account deletion path passes Google Play review for closed testing (it is no longer flagged as unavailable).

## Assumptions

- "30 days" means 30 × 24 hours from the moment of the request. Dates shown to users are converted to their local time.
- Pending users' owned groups are hidden without telling the other members why. To them it looks the same as the owner deleting the group.
- Only group owners create meetups through the app (the 006 rule), so meetups a user created in groups they don't own are rare, but they are still covered by FR-006 and FR-014.
- Infrastructure backups and operational logs held by hosting providers can't be selectively purged. They are allowed to expire on the providers' own schedules, which FR-021 documents with their real durations.
- There is no legal requirement to keep any LoopMeet user data after deletion. If one appears, it is added to FR-021 explicitly.
- Deleting the user's sign-in account also removes every sign-in method linked to it (email, Google, Apple), so the same email can later create a fresh, unrelated account.
- Group ownership transfer, the web deletion page and the privacy policy text are out of scope (017 covers the latter two).
