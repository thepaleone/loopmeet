# Feature Specification: Profile Tab and Profile Management

**Feature Branch**: `003-profile-settings-tab`  
**Created**: 2026-02-26  
**Status**: Draft  
**Input**: User description: "I'd like to implement a new feature that adds another tab to the end tabbar. The tab should read \"Profile\" and it will show a screen that allows the user to view and edit their profile. They can update the display name, change their password and change their avatar, if they have one. If they change the avatar it should override any avatar that comes from their social logins. This change should also update the default create user flow to copy the avatar from the social login if an override has not already been established. Changing the password should be it's own popup modal rather than just on the profile page. I think the profile page should also show the date the user join (User since) and how many groups the user belongs to."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Open and View Profile Tab (Priority: P1)

As a signed-in user, I want a Profile tab in the main tab bar so I can view my profile details and access profile actions from a dedicated screen.

**Why this priority**: This is the entry point for the feature and must exist before any profile editing actions can be used.

**Independent Test**: Sign in, verify a `Profile` tab appears at the end of the tab bar, open it, and confirm the profile screen shows the expected summary information and actions.

**Acceptance Scenarios**:

1. **Given** a signed-in user is viewing the main tab bar, **When** they look at the tab order, **Then** a tab labeled `Profile` appears as the last tab.
2. **Given** a signed-in user selects the `Profile` tab, **When** the profile screen loads, **Then** the user can view their display name, avatar (or avatar placeholder), `User since` date, and group membership count.
3. **Given** a signed-in user is on the profile screen, **When** profile data is shown, **Then** the screen also presents actions to edit display name, change avatar, and start a password change flow.

---

### User Story 2 - Edit Display Name and Avatar (Priority: P2)

As a signed-in user, I want to update my display name and avatar from the profile screen so my profile reflects how I want to be represented in the app.

**Why this priority**: Profile editing is the primary user value of the new screen after navigation access is established.

**Independent Test**: Open the Profile tab, change the display name and avatar, save, and verify the updated values appear on reload and are not replaced by social-login avatar data after an account sign-in refresh.

**Acceptance Scenarios**:

1. **Given** a signed-in user is on the profile screen, **When** they update and save their display name, **Then** the new display name is shown on the profile screen after the save completes.
2. **Given** a signed-in user currently displays an avatar sourced from a social login, **When** they change their avatar from the profile screen and save, **Then** the new avatar becomes the displayed profile avatar and is treated as a user override.
3. **Given** a signed-in user has previously set an avatar override, **When** the account later signs in through a social login that provides an avatar, **Then** the social avatar does not replace the user-selected avatar override.

---

### User Story 3 - Change Password in a Modal (Priority: P3)

As a signed-in user, I want password changes to happen in a separate popup modal so the security-sensitive action is clearly separated from profile page content.

**Why this priority**: Password management is important but depends on the Profile tab existing and is a narrower workflow than basic profile viewing/editing.

**Independent Test**: Open the Profile tab, start password change, confirm a popup modal opens (instead of inline fields), complete the change, and verify the profile page remains available after the modal closes.

**Acceptance Scenarios**:

1. **Given** a signed-in user is on the profile screen, **When** they choose to change their password, **Then** a popup modal opens for password change entry and confirmation.
2. **Given** a signed-in user submits a valid password change in the popup modal, **When** the change completes, **Then** the user receives a success confirmation and returns to the profile screen without losing other profile information.
3. **Given** the profile screen is displayed, **When** the user has not opened the password change modal, **Then** password entry fields are not shown inline on the profile page.

---

### User Story 4 - Inherit Social Avatar on Account Creation (Priority: P4)

As a user signing up with a social login, I want my avatar to be copied into my new profile by default so my profile starts with recognizable identity information without extra setup.

**Why this priority**: This improves first-time profile completeness and supports the avatar override behavior, but it is secondary to the on-screen profile workflows.

**Independent Test**: Create a user account using a social login with an avatar and verify the initial profile avatar is copied; then confirm users with an established avatar override are not overwritten by later social avatar values.

**Acceptance Scenarios**:

1. **Given** a new user account is created through a social login that provides an avatar, **When** the initial profile is created and no avatar override is established, **Then** the user's profile avatar is populated from the social login avatar.
2. **Given** a user account already has an established avatar override, **When** profile creation or profile bootstrap logic runs again for the same user context, **Then** the existing override is preserved and not replaced by social avatar data.

---

### Edge Cases

- User has no avatar from any source; the Profile screen shows a placeholder state and still allows all non-avatar profile information to load.
- User has no groups; the Profile screen shows a group count of `0` without blocking other profile actions.
- User belongs to many groups; the Profile screen still displays a single count value without truncation ambiguity.
- User starts changing password, then closes the modal without submitting; no password changes are applied and the Profile screen remains unchanged.
- Password change submission fails (for example, incorrect current password or password policy mismatch); the modal stays open and shows a clear error without discarding unrelated profile changes.
- User has a social-login avatar and later sets a profile avatar override; subsequent social logins must not replace the override.
- Social login does not provide an avatar during account creation; profile creation completes without an avatar and does not fail the account creation flow.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The signed-in tab bar MUST include a `Profile` tab as the last tab in the tab order.
- **FR-002**: Selecting the `Profile` tab MUST open a dedicated profile screen for the signed-in user.
- **FR-003**: The profile screen MUST display the user's current display name.
- **FR-004**: The profile screen MUST display the user's current avatar when one exists, and a clear placeholder state when no avatar exists.
- **FR-005**: The profile screen MUST display the user's account join date labeled as `User since`.
- **FR-006**: The profile screen MUST display how many groups the user currently belongs to.
- **FR-007**: The profile screen MUST allow the user to update and save their display name.
- **FR-008**: The system MUST confirm whether a display name update succeeds or fails and keep the user on the profile screen.
- **FR-009**: The profile screen MUST provide a way for the user to change their avatar when an avatar is present.
- **FR-010**: When a user changes their avatar from the profile screen, the system MUST treat the new avatar as a user override for avatar display.
- **FR-011**: Once a user avatar override is established, avatar data from social logins MUST NOT overwrite the user-selected avatar.
- **FR-012**: During default user creation from a social login, the system MUST copy the social-login avatar into the user's profile when a social avatar is available and no avatar override exists.
- **FR-013**: The profile screen MUST provide a password change action that opens a separate popup modal for entering password change details.
- **FR-014**: Password change inputs MUST NOT be displayed inline on the main profile page.
- **FR-015**: The password change modal MUST provide clear success and error feedback without discarding unrelated profile page data.
- **FR-016**: The profile screen MUST only display and edit profile information for the currently authenticated user.

### Key Entities *(include if feature involves data)*

- **User Profile**: Represents the signed-in user's editable profile information, including display name, avatar, join date, and profile-level actions.
- **Avatar Source State**: Represents whether the current avatar comes from a social login or from a user-set override and governs overwrite behavior.
- **Password Change Request**: Represents a user-initiated password update submitted from the popup modal, including validation outcome and completion status.
- **Group Membership Count**: Represents the total number of groups the user currently belongs to and is shown as a summary value on the profile screen.

## Assumptions

- The `Profile` tab is shown only to authenticated users inside the signed-in experience.
- The group count includes all groups where the user currently has active membership, including groups they own.
- Accounts that do not currently support password changes (for example, accounts without a local password credential) will show a clear unavailable state or explanatory message; introducing a new credential setup flow is out of scope for this feature.
- Removing an avatar (as distinct from changing/replacing it) is not required for this feature unless already supported by existing profile behavior.

## Privacy & Safety Considerations *(mandatory if user data or user-to-user features are involved)*

- **Data Minimization**: This feature reuses existing profile attributes and adds editing of display name/avatar plus password-change initiation; no new profile fields beyond those requested are required.
- **Default Visibility**: The Profile tab and password-change workflow are visible only to the authenticated account owner viewing their own signed-in experience.
- **Sharing/Consent**: Display name and avatar changes require explicit user action to submit/save; copying a social avatar on account creation relies on the user's consent already granted through the social sign-in provider.
- **Retention**: Updated profile values follow existing account retention policies; superseded avatar images and password change records follow existing profile/security retention rules.
- **Safety Controls**: Password changes must be handled in a dedicated modal with clear error feedback, and the system must prevent users from changing another user's profile information.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of signed-in users in QA validation can access a `Profile` tab as the last tab in the tab bar and open the profile screen without leaving the signed-in experience.
- **SC-002**: In usability testing, at least 90% of users can locate the `Profile` tab and identify their display name, `User since` date, and group count within 15 seconds.
- **SC-003**: In usability testing, at least 90% of users can complete a display name update from the profile screen in under 60 seconds on their first attempt.
- **SC-004**: In QA validation, 100% of password change attempts initiated from the profile screen open in a popup modal, with no inline password fields shown on the profile page.
- **SC-005**: In QA validation of social-login accounts, user-set avatar overrides remain unchanged after subsequent social sign-ins in 100% of tested override scenarios.
- **SC-006**: In QA validation of new social-login signups that provide an avatar, at least 95% of new profiles display the copied social avatar on first profile load when no override exists.
