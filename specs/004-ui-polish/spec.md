# Feature Specification: UI Polish — Icons & Button Placement

**Feature Branch**: `004-ui-polish`
**Created**: 2026-03-09
**Status**: Draft
**Input**: User description: "For feature branch 4 I want to polish the UI. The login button needs to move from the Groups page to the Profile page and it should include an icon that signifies it's a logout action. The profile page Save button also needs an icon that signifies saving. On the Groups page, the 'Create Group' button should be a circle button with a large '+' sign and it should float at the bottom right of the page. On the pending invitations page the 'Accept' button which is only visible on the Desktop needs to include the same check icon that the Accept swipe action uses and it needs a Decline button only for the Desktop that uses the same icon as the decline swipe action. On the group detail page and the send invitation page the buttons need appropriate invitation icons. All icons MUST be available on all platforms. Analyze the current icon usage for the main tab navigation and use the same methodology."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Logout Moves to Profile Page (Priority: P1)

A signed-in user wants to log out of the app. Currently the Logout button sits in the Groups page header, which is unintuitive — users expect account-related actions to live on the Profile page. After this change the Logout button is removed from Groups and added to the Profile page with a recognisable logout icon.

**Why this priority**: This is a UX correctness issue. Logout belongs on the Profile page by standard convention. Removing it from Groups also clears space for Story 2's changes to that page.

**Independent Test**: Navigate to the Profile page as a signed-in user; confirm a Logout button with a logout icon is present; tap it and confirm the app navigates to the login screen. Then navigate to the Groups page and confirm no Logout button exists there.

**Acceptance Scenarios**:

1. **Given** a signed-in user is on the Profile page, **When** they view the page, **Then** a Logout button with a logout icon is visible.
2. **Given** a signed-in user taps the Logout button on the Profile page, **When** the action completes, **Then** the user is signed out and returned to the login screen.
3. **Given** a signed-in user is on the Groups page, **When** they view the page, **Then** no Logout button is present.

---

### User Story 2 — Floating "+" Create Group Button (Priority: P2)

A user browsing their group list wants to quickly create a new group. The current "Create Group" button is a plain text button in the page body. After this change it becomes a prominent circular floating action button with a large "+" icon, fixed at the bottom-right corner of the Groups page and always accessible regardless of scroll position.

**Why this priority**: The floating action button pattern significantly improves discoverability of the primary action, and the always-visible position ensures users can reach it without scrolling back to the top.

**Independent Test**: Open the Groups page with a long list; scroll to the bottom; confirm the circular "+" button is still visible at the bottom-right; tap it and confirm the Create Group flow launches.

**Acceptance Scenarios**:

1. **Given** a user is on the Groups page, **When** they view the page, **Then** a circular button displaying a large "+" is visible at the bottom-right corner of the screen.
2. **Given** the user scrolls through the group list, **When** the list scrolls, **Then** the "+" button remains fixed at the bottom-right corner.
3. **Given** the user taps the "+" button, **When** the tap registers, **Then** the Create Group flow launches (same behaviour as the original Create Group button).
4. **Given** a user is on the Groups page, **When** they view the page, **Then** no separate "Create Group" text button is present; only the floating "+" button exists.

---

### User Story 3 — Save Icon on Profile Page (Priority: P3)

A user editing their profile taps Save. The current Save button has no icon. After this change the Save button displays a recognisable save icon alongside the "Save" label.

**Why this priority**: Visual polish that brings the Profile page in line with the iconography standard applied across this feature. The page is functional without it, but the icon adds clarity.

**Independent Test**: Open the Profile page; confirm the Save button displays a save icon alongside its label; tap it and confirm the profile is saved with no behaviour change.

**Acceptance Scenarios**:

1. **Given** a user is on the Profile page, **When** they view the Save button, **Then** the button displays a save icon alongside the "Save" label.
2. **Given** a user taps the Save button, **When** the profile is saved, **Then** the app behaves identically to before (no regression).

---

### User Story 4 — Accept & Decline Buttons on Desktop Pending Invitations (Priority: P2)

On desktop, the Pending Invitations list shows an "Accept" button per row but no way to decline without swiping (which is not available on desktop). After this change: the existing desktop Accept button gains the same check icon used by the mobile Accept swipe action, and a new Decline button — with the same icon used by the Decline swipe action — is added next to it on each row.

**Why this priority**: Desktop users currently cannot decline invitations at all without a touch swipe. This is a functional gap, not just a cosmetic one.

**Independent Test**: On a desktop-sized screen, open Pending Invitations; confirm each row shows an Accept button (with check icon) and a Decline button (with decline icon); tap each and confirm the correct action fires.

**Acceptance Scenarios**:

1. **Given** a desktop user views the Pending Invitations page, **When** they look at any invitation row, **Then** an Accept button with the same check icon as the Accept swipe action is visible.
2. **Given** a desktop user views the Pending Invitations page, **When** they look at any invitation row, **Then** a Decline button with the same icon as the Decline swipe action is visible.
3. **Given** a desktop user taps the Accept button, **When** the action completes, **Then** the invitation is accepted (same result as the swipe Accept).
4. **Given** a desktop user taps the Decline button, **When** the action completes, **Then** the invitation is declined (same result as the swipe Decline).
5. **Given** a mobile or tablet user is on the Pending Invitations page, **When** they view the page, **Then** the Accept and Decline buttons are not visible; swipe actions remain the interaction model.

---

### User Story 5 — Invitation Icons on Group Detail & Send Invitation Pages (Priority: P3)

Users who send invitations from the Group Detail page or the Send Invitation page see action buttons without icons. After this change, each invitation-related button on these pages displays an appropriate invitation-themed icon (e.g., paper plane, envelope, person-plus) alongside its label.

**Why this priority**: Visual polish that rounds out icon consistency across all invitation touchpoints in the app.

**Independent Test**: Open the Group Detail page; confirm the invitation action button has a relevant invitation icon; navigate to the Send Invitation page; confirm the "Send Invite" button has a relevant invitation icon.

**Acceptance Scenarios**:

1. **Given** a user is on the Group Detail page, **When** they view invitation-related action buttons, **Then** each displays an appropriate invitation icon alongside its label.
2. **Given** a user is on the Send Invitation page, **When** they view the "Send Invite" button, **Then** it displays an appropriate invitation-themed icon.
3. **Given** the user taps either button, **When** the action completes, **Then** the app behaves identically to before (no regression).

---

### Edge Cases

- The floating "+" button must not permanently cover actionable list items at the bottom of the Groups list. Sufficient bottom padding must be applied to the list so the last item is fully visible above the FAB.
- Icons must render correctly in both light mode and dark mode on all supported platforms.
- If the device idiom cannot be determined at runtime, the desktop Accept and Decline buttons on the Pending Invitations page must default to visible rather than hidden, to avoid hiding critical functionality.
- Removing the Logout button from Groups must not break any navigation or app state logic previously tied to that location.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The Logout button MUST be removed from the Groups page header.
- **FR-002**: A Logout button with a logout icon MUST be added to the Profile page.
- **FR-003**: Tapping the Profile page Logout button MUST sign the user out and navigate to the login screen, identical in behaviour to the removed Groups page Logout button.
- **FR-004**: The Profile page Save button MUST display a save icon alongside its "Save" label.
- **FR-005**: The Groups page MUST replace the existing "Create Group" text button with a circular floating action button (FAB) displaying a large "+" icon.
- **FR-006**: The FAB MUST be positioned fixed at the bottom-right corner of the Groups page.
- **FR-007**: The FAB MUST remain visible and tappable regardless of list scroll position.
- **FR-008**: Tapping the FAB MUST initiate the Create Group flow.
- **FR-009**: On desktop idiom, each invitation row on the Pending Invitations page MUST display an Accept button that includes the same check icon used by the Accept swipe action.
- **FR-010**: On desktop idiom, each invitation row on the Pending Invitations page MUST include a Decline button that uses the same icon as the Decline swipe action.
- **FR-011**: The desktop Decline button MUST trigger the same decline action as the swipe Decline action.
- **FR-012**: The desktop Accept and Decline buttons MUST NOT be visible on phone or tablet idioms.
- **FR-013**: Invitation-related action buttons on the Group Detail page MUST each display an appropriate invitation icon alongside their labels.
- **FR-014**: The "Send Invite" button on the Send Invitation page MUST display an appropriate invitation-themed icon alongside its label.
- **FR-015**: All icons introduced in this feature MUST be available and render correctly on iOS, Android, macOS, and Windows.
- **FR-016**: All icons MUST render correctly in both light mode and dark mode.
- **FR-017**: All new icons MUST use the same cross-platform delivery approach as the existing main tab navigation icons.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of sign-out interactions originate from the Profile page; no sign-out path exists from the Groups page after this change.
- **SC-002**: The floating "+" Create Group button is reachable from any scroll position on the Groups page in a single tap, with no scrolling required.
- **SC-003**: Desktop users can both accept and decline any pending invitation without performing a swipe gesture — both actions are completable via visible, labelled buttons on the Pending Invitations page.
- **SC-004**: All new icon assets render without missing-image errors on iOS, Android, macOS, and Windows in both light and dark mode, verified across all four platforms.
- **SC-005**: No existing functionality is regressed — all button actions (logout, save, create group, accept invitation, decline invitation, send invite) produce identical outcomes before and after this feature.
- **SC-006**: Users can identify the intended action of each iconified button without reading its label, as confirmed by at least 4 out of 5 participants in an informal review correctly identifying each icon's meaning.

## Assumptions

- The app targets iOS, Android, macOS, and Windows via .NET MAUI.
- Tab navigation icons are bundled as PNG/SVG image resources (MAUI image assets). This same mechanism will be used for all new icons (logout, save, invitation send) introduced in this feature.
- The existing Accept swipe action uses a Unicode checkmark character (✓) and the Decline swipe action uses a Unicode trash/bin character (🗑). The new desktop Accept and Decline buttons will use these same characters to guarantee visual consistency with the swipe actions without introducing new assets for those specific icons.
- The Group Detail page contains at least one invitation-related action button (e.g., "Invite Member"). The exact button label will be confirmed during planning.
- The FAB requires no sub-menu or animation — a single "+" tap action is sufficient.
- The Decline button on desktop invitations reuses the existing `DeclineInvitationCommand` already wired to the swipe action; no new back-end logic is required.
- Sufficient bottom padding on the Groups list will be added to prevent the FAB from permanently obscuring the last list item.
