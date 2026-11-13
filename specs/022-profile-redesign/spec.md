# Feature Specification: Profile Screen Redesign

**Feature Branch**: `022-profile-redesign`
**Created**: 2026-11-13
**Status**: Draft
**Input**: User description: "Redesign the Profile tab so no control can be cut off, and give it a cleaner, more modern layout." Full prompt and design input: [design/](design/) (README, current captures, mockups, reference XAML).

## Background

Today's Profile tab lays its five actions out in two fixed rows of buttons that never wrap or shrink. On narrow phones, in some orientations and at larger text sizes the last buttons run off the right edge: the Android capture ([design/current/android-profile.png](design/current/android-profile.png)) shows Log out cut off in portrait at the default text size. Log out and Delete account also look exactly like Save, and "User since: 2026-11-03" and "Groups: 5" read like debug output.

The mockups in [design/mockups/](design/mockups/) show the target: a header with an icon-only Save, a header card with the avatar, name, email and two summary chips, then full-width rows grouped into cards. The mockups are HTML renders, so fonts and icons are close, not exact. Where they differ from this spec (Save in the navigation bar), the spec decides.

## Clarifications

### Session 2026-11-13

- Q: Where does Save go: the navigation bar (as in the mockups) or an in-page header row? → A: The in-page header row, outside the scrolling content, the Create/Edit Meetup pattern (011). Not the navigation bar.
- Q: What is the exact Delete account subtitle? → A: "Review what's deleted before you confirm", with no day count or other duration.
- Q: Landscape and wide screens: one centred column, or the two-column variant in `mockups/ios_landscape.png`? → A: Two columns when wide: at an available width of 800 points or more, the header card sits on the left and the other cards on the right. Below that, one centred column.
- Q: What does the Notifications row show for each permission state? → A: Granted: "On · Manage in device settings". Denied and Unknown (not yet asked, or the OS state can't be read): "Off · Turn on in device settings".

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Every Profile action is reachable on any screen (Priority: P1)

A signed-in user opens the Profile tab on any phone or tablet, in portrait or landscape, at any system text size, and can see and tap every action: save, change password (when it applies), notifications, log out and delete account. Nothing is cut off at the edge of the screen.

**Why this priority**: an action that is cut off can't be used. Today Log out is clipped on a common Android phone in portrait, which is a defect, not just a style issue.

**Independent Test**: open the Profile tab on a narrow Android phone and an iPhone, in both orientations, at the default text size and at the largest system text size. Every action is fully visible (scrolling down where needed) and works.

**Acceptance Scenarios**:

1. **Given** a narrow Android phone in portrait at the default text size, **When** the user opens Profile, **Then** Log out and Delete account are fully visible and tappable (the case clipped today).
2. **Given** any supported device at the largest system text size, **When** the user opens Profile, **Then** every row and chip grows to fit its text, nothing is clipped horizontally, and the page scrolls vertically to reach everything.
3. **Given** a screen narrower than 800 points (most phones in portrait, small phones in landscape), **When** the user opens Profile, **Then** the content sits in one centred column of comfortable reading width, not stretched edge to edge.
4. **Given** a screen at least 800 points wide (larger phones in landscape, tablets), **When** the user opens Profile, **Then** the header card sits in a left column and the Account card, the Log out / Delete account card, the status message and the version text sit in a right column, as in [design/mockups/ios_landscape.png](design/mockups/ios_landscape.png).
5. **Given** the user rotates the device or resizes the window across the 800-point width, **Then** the layout switches between one and two columns without leaving the page and without losing an unsaved display-name edit.
6. **Given** an email/password account, **When** the user opens Profile, **Then** the Change password row is shown. **Given** a Google or Apple account, **Then** it isn't, as today.

---

### User Story 2 - Edit the display name and save it from a header that stays in view (Priority: P1)

The user edits their display name in the Account card and saves it with the Save control in the page header. The header stays above the scrolling content, so the on-screen keyboard can't hide Save, exactly as on Create and Edit Meetup.

**Why this priority**: saving the display name is the page's only edit, and it moves from a button in the clipped row to the header. It must keep working from the first release of the new layout.

**Independent Test**: change the display name, check that Save becomes active, save, and see "Profile updated." Then check that Save is inactive when nothing has changed or the name is blank.

**Acceptance Scenarios**:

1. **Given** the profile has loaded, **When** nothing has been edited, **Then** Save is visibly inactive and does nothing when tapped.
2. **Given** the user changes the display name to a different, non-blank value, **When** they look at the header, **Then** Save is active (see [design/mockups/ios_editing.png](design/mockups/ios_editing.png)).
3. **Given** the user clears the display name or enters only spaces, **Then** Save is inactive.
4. **Given** the user changes the name and then changes it back to the saved value, **Then** Save is inactive again.
5. **Given** an edited name, **When** the user taps Save, **Then** the name is saved as today, the status message reads "Profile updated.", the header card shows the new name, and Save becomes inactive.
6. **Given** the keyboard is open while editing the name, **Then** Save is still visible and tappable.
7. **Given** a screen reader is on, **When** focus reaches Save, **Then** it is announced with a description such as "Save profile", not as an unlabeled image.

---

### User Story 3 - See who you are at a glance (Priority: P2)

The top of the page shows a header card with a large round avatar, the display name, the email address and two short facts, "Member since Nov 2026" and "5 groups". Tapping the avatar changes the photo as it does today.

**Why this priority**: it replaces the debug-style labels and makes the photo picker discoverable. It's an improvement, not a fix for a broken action.

**Independent Test**: open Profile for a user with a photo and for one without. Check the avatar, name, email and chips, then change the photo by tapping the avatar.

**Acceptance Scenarios**:

1. **Given** a user without a photo, **Then** the avatar shows their initial in white on the brand's amber-to-pink gradient. **Given** a user with a photo, **Then** the photo fills the circle.
2. **Given** the header card, **Then** a small camera badge on the avatar signals that it can be tapped to change the photo.
3. **Given** the user taps the avatar, **Then** the existing photo picker opens, and an upload indicator shows on the avatar while the new photo uploads.
4. **Given** a user who joined in November 2026 and belongs to 5 groups, **Then** the chips read "Member since Nov 2026" and "5 groups". **Given** exactly one group, **Then** the chip reads "1 group". **Given** none, **Then** "0 groups".
5. **Given** a very long display name or email address, **Then** each stays on one line and is shortened with an ellipsis, rather than wrapping or pushing other content off screen.
6. **Given** a narrow screen or a large text size where the two chips don't fit side by side, **Then** the second chip moves to the next line instead of being clipped.
7. **Given** the user is editing the display name, **Then** the header card keeps showing the saved name until the change is saved.

---

### User Story 4 - Know your notification state and reach its settings (Priority: P2)

The Account card has a Notifications row that says whether notifications are on or off, and opens the device's notification settings for LoopMeet when tapped, as the current button does.

**Why this priority**: the state is new information. Opening device settings already works today.

**Independent Test**: with notifications allowed, open Profile and see "On". Turn them off in device settings, return to the app, and see "Off" without restarting.

**Acceptance Scenarios**:

1. **Given** notifications are allowed for LoopMeet (Granted), **Then** the row's subtitle reads "On · Manage in device settings".
2. **Given** notifications are denied, or haven't been asked for yet, or the state can't be read (Denied or Unknown), **Then** the subtitle reads "Off · Turn on in device settings".
3. **Given** the user changes the setting in device settings and comes back to Profile, **Then** the subtitle reflects the new state each time the page appears.
4. **Given** the user taps the row, **Then** the device's notification settings for LoopMeet open. If they can't be opened, the status message says so, as today.

---

### User Story 5 - Tell destructive actions apart from routine ones (Priority: P2)

Log out and Delete account sit together in their own card below the Account card. Log out looks neutral. Delete account uses the brand's soft terracotta, not the amber used for primary actions and not a harsh alert red, and opens the existing delete-account flow.

**Why this priority**: today Delete account looks the same as Save. Telling them apart reduces mistakes, but the existing delete flow already asks for confirmation.

**Independent Test**: open Profile, check the styling of both rows, tap Log out to sign out, sign back in, then tap Delete account and confirm that the existing flow opens.

**Acceptance Scenarios**:

1. **Given** the Profile page, **Then** Log out and Delete account are in a card separate from the Account card, and neither uses the primary-action amber.
2. **Given** the user taps Log out, **Then** they are signed out as today.
3. **Given** the user taps Delete account, **Then** the existing delete-account flow opens, and nothing is deleted from this page.
4. **Given** the Delete account row, **Then** its subtitle reads exactly "Review what's deleted before you confirm", and states no number of days.

---

### Edge Cases

- **Profile still loading or failed to load:** the page shows what it has, Save stays inactive, and the existing error message appears in the status area, as today.
- **Saving fails** (network or server error): the status message reports it, as today, the edited name stays in the field, and Save stays active so the user can retry.
- **Save tapped twice quickly:** only one save is sent.
- **Photo upload in progress:** the upload indicator shows on the avatar, and the avatar can't start a second pick until the first finishes.
- **Display name with leading or trailing spaces only:** treated as blank, so Save is inactive.
- **Notification permission unreadable on this platform:** the existing permission service falls back to its last known state, or Unknown. Unknown shows "Off · Turn on in device settings", and the row still opens settings.
- **Very large text size with a long name:** the name and email truncate, while row titles, subtitles and chips wrap onto more lines, so the page grows taller rather than wider.
- **Tablet or wide window:** in two columns, the whole content stops at about 1100 points wide and stays centred. In one column, it stops at about 600 points. Either way, the header row lines up with the content's edges.
- **Width exactly at the threshold:** 800 points or more means two columns.
- **Two columns with a long page:** each column scrolls with the page as one, so nothing in the right column is cut off when it's taller than the left.
- **Status message and version text:** both stay at the bottom of the page, below the cards.

## Requirements *(mandatory)*

### Functional Requirements

#### Layout and reachability

- **FR-001**: The Profile page MUST present every action as a full-width row or as the header's Save control, never as buttons side by side in a row that can't wrap. No control may extend past the screen's edge at any supported width, orientation or system text size.
- **FR-002**: When the available width is under 800 points, the page MUST show one column, at most about 600 points wide and centred. At 800 points or more, it MUST show two columns: the header card on the left, and the Account card, the Log out / Delete account card, the status message and the version text on the right. The two-column content MUST be at most about 1100 points wide and centred. The layout MUST switch when the width crosses 800 points (rotation, window resize) without losing unsaved edits.
- **FR-003**: Every row title, row subtitle and chip MUST grow with the system text size, wrapping onto more lines rather than clipping, on iOS and Android in both orientations. Only the display name and the email in the header card may truncate, with an ellipsis.

#### Header and saving

- **FR-004**: The page MUST show a header row with the "Profile" title and an icon-only Save control. The header row MUST stay outside the scrolling content, so neither scrolling nor the on-screen keyboard can hide Save. This reuses the header pattern of Create and Edit Meetup (011), which are not changed. Save MUST NOT be placed in the navigation bar, even though the mockups show it there.
- **FR-005**: Save MUST be active only when the edited display name differs from the saved one and isn't blank after trimming spaces. It MUST look inactive and do nothing otherwise, and it MUST not start a second save while one is in progress.
- **FR-006**: Save MUST have an accessible description (for example "Save profile") that screen readers announce.
- **FR-007**: Saving MUST keep today's behaviour: the same save path and the "Profile updated." message on success, with today's error messages on failure. After a successful save, the saved name becomes the new baseline for FR-005.

#### Header card

- **FR-008**: A header card MUST show a large circular avatar: the user's photo when they have one, otherwise their first initial in white on the brand's amber-to-pink gradient (style guide, Default Profile Avatar).
- **FR-009**: The avatar MUST carry a camera badge, and tapping the avatar MUST open the existing photo picker, with the existing upload indicator shown on the avatar while the upload runs.
- **FR-010**: Below the avatar, the card MUST show the saved display name and the email address, each on one line, truncated with an ellipsis when too long.
- **FR-011**: The card MUST show two chips: `Member since <abbreviated month> <year>` (for example "Member since Nov 2026") and `<n> groups`, with "1 group" for exactly one. The chips MUST wrap onto a new line rather than clip when they don't fit side by side.

#### Account card

- **FR-012**: An "Account" section card MUST contain full-width rows for:
  - the display name, edited in place in a full-width field;
  - Change password, shown only for accounts that can change their password (as today), opening the existing change-password flow;
  - Notifications.
- **FR-013**: The Notifications row MUST show a subtitle for the current notification permission state: "On · Manage in device settings" for Granted, and "Off · Turn on in device settings" for Denied and for Unknown. The state MUST be read again every time the page appears. Tapping the row MUST open the device's notification settings for LoopMeet, as today.

#### Session and deletion card

- **FR-014**: A separate card below the Account card MUST contain Log out, styled neutrally, and Delete account, styled in the brand's soft terracotta. Neither may use the primary-action amber, and Delete account MUST NOT use a harsh alert red.
- **FR-015**: Log out MUST sign the user out as today. Delete account MUST open the existing delete-account flow and delete nothing itself.
- **FR-016**: Delete account's subtitle MUST read exactly "Review what's deleted before you confirm". It MUST NOT state a number of days or any other duration. The grace period is already defined in two places (016), and this text must not become a third.
- **FR-017**: The terracotta MUST be defined once in the app's colour resources, as one light and one dark value, and the Delete account text in it MUST meet a contrast ratio of at least 4.5:1 against the card's background.

#### Bottom of page

- **FR-018**: The status message and the app version text MUST stay at the bottom of the page, below the cards, with today's content and behaviour.

#### Scope guard

- **FR-019**: The redesign MUST NOT change the Api, Supabase, the database, the profile fields, or the change-password, delete-account and notification-settings flows. No profile field or action may be added or removed.

### Key Entities

- **Profile summary (shown, not stored)**: the display name, email address, avatar (photo or initial), the date the user joined (shown as month and year) and the number of groups they belong to. All of these already come from the existing profile load. The two chip texts are derived from them on the device.
- **Notification permission state**: Granted, Denied or Unknown, read from the existing notification permission service each time the page appears. Granted shows as on, and Denied and Unknown as off.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On the device matrix (a narrow Android phone, an iPhone and an iPad, each in portrait and landscape, at the default and at the largest system text size), 100% of Profile actions are fully visible and work, in both the one-column and the two-column layout. Today, at least one action is cut off on the narrow Android phone in portrait.
- **SC-002**: A user can change and save their display name in at most 3 interactions (tap the field, type, tap Save), with Save visible throughout, including while the keyboard is open.
- **SC-003**: In a check of each screen in the matrix, no destructive action (Log out, Delete account) shares the styling of the primary Save action.
- **SC-004**: After the user changes notification permission in device settings, the Profile page shows the new state the next time it appears, in 100% of attempts, with no app restart.
- **SC-005**: All existing automated app tests continue to pass, and new automated checks cover the Save rule, the chip texts and the absence of side-by-side button rows.

## Assumptions

- **Language and dates:** the app is English-only, so "Member since" uses English abbreviated month names ("Nov 2026"), from the join date the profile already provides.
- **Two-column layout and simplicity:** two columns add one width-based layout switch to the page. The plan records this under Complexity Tracking (Principle IV), as the user chose it over the single-column default.
- **Terracotta value:** the style guide names a "soft terracotta" for destructive actions but gives no exact value. The plan picks one (the reference XAML uses `#C0573E`) that meets FR-017's contrast rule.
- **New glyph:** the camera badge needs a new camera icon. The other icons (save, change password, log out, delete account, notification bell, and the profile glyph for the display-name row) already exist in the app.
- **Navigation title:** the page keeps its "Profile" title in the header row. The bottom tab bar is unchanged.
- **Existing behaviour stays:** profile loading when the page appears, the "Profile updated." message and the version text all stay as they are. Existing source-inspection tests that read the Profile page are kept, or updated deliberately where the layout they check changes.
- **Testing:** layout is checked by source-inspection tests (the accepted approach for XAML-only behaviour) and by a quickstart device matrix, since text-size and orientation behaviour can only be seen on devices.

## Out of Scope

- Any Api, Supabase or database change.
- New profile fields or actions.
- The change-password, delete-account and notification-settings flows themselves.
- A dark-mode redesign (the page must stay usable in dark mode, but isn't restyled for it).
- Restyling other pages, including the Create and Edit Meetup header pattern, which is reused, not changed.
