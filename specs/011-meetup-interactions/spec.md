# Feature Specification: Meetup Interaction Improvements

**Feature Branch**: `011-meetup-interactions`
**Created**: 2026-07-30
**Status**: Draft
**Input**: User description: "Improve meetup interactions across the app: the create/edit meetup forms, and how users view meetup information. Move the save action on Create/Edit Meetup pages to an icon-only button in the top-right corner beside the page title so the on-screen keyboard cannot cover it. Add a read-only meetup details screen (title, date/time, location or TBD, group, organizer display name) reachable by tapping meetup cards on both the Home page Upcoming Meetups list and the Group Detail page meetup list, with a map glyph to open a valid location in the native maps app. Show a pencil edit glyph on the details screen only for group owners, routing to the existing Edit Meetup form; non-owners can now view details where previously tapping did nothing. Swipe-to-delete on Group Detail remains owner-only and unchanged."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Save a Meetup Without Fighting the Keyboard (Priority: P1)

A group owner filling in a new meetup (or editing an existing one) types into the title field. Today the save button sits at the bottom of a scrolling form, so the on-screen keyboard covers it: the owner must dismiss the keyboard, scroll down, and only then can they save. With this change the save action lives as a compact icon in the top-right corner of the page, level with the page title — always visible, always reachable. The owner taps it directly from the keyboard-open state and the meetup is saved.

**Why this priority**: This is a friction point in the app's core creation flow, hit every single time anyone creates or edits a meetup. It is also the smallest, lowest-risk slice: it touches two pages, changes no data, and delivers value entirely on its own.

**Independent Test**: Open Create Meetup, tap into the title field so the keyboard appears, and confirm the save control is visible and reachable without dismissing the keyboard or scrolling. Complete and save a meetup. Repeat on Edit Meetup with a change to the title. Confirm validation failures and error messages behave exactly as before.

**Acceptance Scenarios**:

1. **Given** a group owner on the Create Meetup page with the keyboard open and the title field focused, **When** they tap the save icon in the top-right corner, **Then** the meetup is created and they return to the previous screen — with no need to dismiss the keyboard or scroll.
2. **Given** a group owner on the Edit Meetup page, **When** they change the title and tap the save icon, **Then** the change is saved with identical behavior to the previous bottom button.
3. **Given** a form with a validation problem (e.g., an empty title), **When** the owner taps the save icon, **Then** the same error message appears in the same place as before, and nothing is saved.
4. **Given** the owner taps the save icon twice in rapid succession, **When** the first save is still in progress, **Then** only one meetup is created or one update applied.
5. **Given** either meetup form is open, **When** the owner looks at the bottom of the form, **Then** there is no second or duplicate save button — the icon is the single save affordance.

---

### User Story 2 - See Everything About a Meetup (Priority: P2)

Any member of a group wants to know the full story of a meetup: what it is, when it is, where it is, which group it belongs to, and who organized it. Today that information is scattered and partly unavailable — the Home page's Upcoming Meetups cards show a summary and cannot be tapped at all, and nowhere in the app shows who created a meetup. This story adds a read-only meetup details screen, reached by tapping a meetup card on the Home page, that presents all of a meetup's information on one screen, with a map affordance to open the location in the device's native maps app when a usable location is set.

**Why this priority**: This is the feature's main information gain and it stands alone: Home page cards do nothing when tapped today, so adding navigation there cannot regress any existing behavior. It is deliverable without touching the Group Detail page at all.

**Independent Test**: From the Home page, tap an Upcoming Meetups card and confirm the details screen opens showing title, date/time, location (or "TBD"), group name, and organizer display name. Tap the map affordance on a meetup with a location and confirm the native maps app opens at the right place. Confirm a meetup with no location shows "TBD" and offers no map affordance.

**Acceptance Scenarios**:

1. **Given** a signed-in user on the Home page with at least one upcoming meetup, **When** they tap a meetup card, **Then** the meetup details screen opens for that meetup.
2. **Given** the details screen is open for a meetup with a location, **When** the user reads the screen, **Then** they see the meetup's title, date and time, location, owning group, and the display name of the member who organized it.
3. **Given** the details screen is open for a meetup whose location was never set, **When** the user reads the location, **Then** it shows "TBD" and no map affordance is offered.
4. **Given** the details screen is open for a meetup with a location that can be opened in maps, **When** the user taps the map affordance, **Then** the device's native maps app opens at that location.
5. **Given** the user is on the details screen, **When** they navigate back, **Then** they return to the screen they came from.
6. **Given** a Home page meetup card for a meetup with an openable location, **When** the user taps the card's map control, **Then** the native maps app opens at that location and the details screen is not opened.
7. **Given** a Home page meetup card, **When** the user taps the location text itself, **Then** the details screen opens — the text is not a separate tap target.

---

### User Story 3 - Owners Reach the Edit Form From Details; Members Reach the Details (Priority: P3)

On the Group Detail page, tapping a meetup currently jumps a group owner straight into the edit form, and does nothing at all for a non-owner. This story makes the tap open the meetup details screen for everyone, and puts an edit (pencil) affordance on that details screen visible only to owners of the group the meetup belongs to. Owners keep a path to editing (one extra tap), and non-owners gain a working tap where they previously got no response. Swipe-to-delete on the Group Detail meetup list stays exactly as it is: owner-only.

**Why this priority**: This depends on the details screen from User Story 2 existing. The Group Detail tap re-route and the owner-only edit affordance must ship together — re-routing the tap without the edit affordance would remove owners' only route to editing a meetup, so they are one slice, not two.

**Independent Test**: As a group owner, tap a meetup on the Group Detail page, confirm the details screen opens, confirm an edit affordance is present, tap it and confirm the existing Edit Meetup form opens for that meetup. Repeat as a non-owner member of the same group: the details screen opens, and no edit affordance is present or reachable. Confirm swipe-to-delete still works for the owner and remains unavailable to the non-owner.

**Acceptance Scenarios**:

1. **Given** a group owner on the Group Detail page, **When** they tap a meetup card, **Then** the meetup details screen opens (not the edit form).
2. **Given** a group owner viewing a meetup's details, **When** they tap the edit affordance, **Then** the existing Edit Meetup form opens for that meetup.
3. **Given** a non-owner member of the group viewing the same meetup's details, **When** they look for an edit control, **Then** no edit affordance is shown and no path to the edit form exists from that screen.
4. **Given** a non-owner member on the Group Detail page, **When** they tap a meetup card, **Then** the details screen opens — where previously nothing happened.
5. **Given** a group owner on the Group Detail page, **When** they swipe a meetup card, **Then** the delete action behaves exactly as it does today.
6. **Given** a non-owner member on the Group Detail page, **When** they swipe a meetup card, **Then** no delete action is offered, exactly as today.
7. **Given** an owner edits a meetup from the details screen and saves, **When** they return to the details screen, **Then** it shows the updated information rather than the pre-edit values.
8. **Given** any group member on the Group Detail page, **When** they tap a meetup card's map control for a meetup with an openable location, **Then** the native maps app opens and the details screen is not opened.

---

### Edge Cases

- **Location name but no map coordinates**: A meetup whose location has a name but no usable coordinates shows the location name and offers no map affordance — on the card or the details screen — so a tap can never open an empty or incorrect map.
- **Card with no location**: The card shows "TBD" with no map control, and every part of it opens the details screen.
- **Organizer no longer in the group, or name unresolvable**: The details screen shows a neutral fallback in the organizer field rather than a blank space, an error, or a raw internal identifier.
- **Organizer is not the group owner**: A meetup created by a non-owner member (permitted at the data layer today) shows that member as organizer, while the edit affordance still appears only for the group's owner — organizing a meetup grants no edit access.
- **Meetup changed or deleted by someone else while the details screen is open**: The user is not left looking at silently stale information; returning to the screen shows current state, and a meetup that no longer exists does not present a broken edit path.
- **Location search active on the meetup forms**: The forms collapse their fields while the user searches for a location. The save icon remains visible in that state and behaves normally — saving with incomplete data produces the usual validation message.
- **Very long titles, place names, or group names** on the details screen wrap or truncate gracefully rather than pushing other information off-screen.
- **Past meetups**: A meetup whose date has passed uses the same details screen with no special handling.
- **Group name unavailable** for the meetup behind a card: the group field shows a fallback rather than an empty row.

## Requirements *(mandatory)*

### Functional Requirements

#### Save action on the meetup forms

- **FR-001**: The Create Meetup and Edit Meetup pages MUST present their save action as an icon-only control positioned on the same row as the page title, aligned to the top-right of the page.
- **FR-002**: The save control MUST remain visible and operable while the on-screen keyboard is displayed and a text field is focused, without requiring the user to scroll or dismiss the keyboard.
- **FR-003**: The previous full-width save button at the bottom of each form MUST be removed, so exactly one save affordance exists per form.
- **FR-004**: Save behavior MUST be functionally unchanged: the same validation rules apply, the same error messages appear in the same location, the same navigation occurs on success, and repeated activation while a save is in progress MUST NOT produce duplicate meetups or duplicate updates.
- **FR-005**: The save control MUST expose a text description of its purpose to assistive technologies, since it presents no visible text label.

#### Meetup details screen

- **FR-006**: The system MUST provide a read-only meetup details screen presenting the meetup's title, date and time, location, owning group, and organizer display name.
- **FR-007**: Users MUST be able to open the details screen by tapping a meetup card in the Home page's Upcoming Meetups list.
- **FR-008**: Users MUST be able to open the details screen by tapping a meetup in the Group Detail page's meetup list, regardless of whether they own the group.
- **FR-009**: When a meetup has a location that can be opened in a maps application, the details screen MUST offer a control that opens that location in the device's native maps application.
- **FR-010**: When a meetup has no location set, the details screen MUST display "TBD" for the location and MUST NOT offer the map control. When a location has a name but cannot be opened in maps, the name MUST be shown without the map control.
- **FR-011**: The organizer field MUST show the display name of the group member who created the meetup, and MUST fall back to a neutral placeholder when that name cannot be determined — never a blank field or an internal identifier.
- **FR-012**: The details screen MUST reflect the meetup's current information when the user arrives at it, including after returning from editing that meetup.
- **FR-013**: Navigating back from the details screen MUST return the user to the screen they came from (Home or Group Detail).
- **FR-014**: The details screen MUST NOT offer any action that changes or deletes the meetup, other than the owner-only edit path in FR-015.

#### Edit access

- **FR-015**: The details screen MUST show an edit control that opens the existing Edit Meetup form for that meetup, and MUST show it only when the current user is the owner of the group the meetup belongs to.
- **FR-016**: Edit-access determination MUST depend only on the current user's ownership of the meetup's group — not on which screen the user arrived from, and not on who created the meetup.
- **FR-017**: For users who are not the group's owner, the details screen MUST provide no route to the edit form: the edit control is absent, not merely disabled or hidden-but-reachable.
- **FR-018**: The existing swipe-to-delete gesture on the Group Detail page's meetup list MUST remain unchanged and MUST remain available only to the group's owner.

#### Interaction consistency on the meetup cards

- **FR-019**: On both the Home page and Group Detail page, tapping a meetup card — including its title, date/time, location text, and group name — MUST open the details screen. The location text MUST NOT be a tap target of its own.
- **FR-020**: When a meetup on a card has a location that can be opened in maps, the card MUST display a map control on its location row that opens that location in the device's native maps application. This control MUST be the only part of the card that does not navigate to the details screen, and MUST be absent when the location cannot be opened.
- **FR-021**: The icon affordances introduced by this feature (edit, open-in-maps) MUST render reliably on every platform the app supports, using the same icon presentation style already proven in the app for its delete and accept actions.

### Key Entities

- **Meetup**: An existing scheduled event belonging to one group. Attributes surfaced by this feature: title, scheduled date and time, optional location (name, address, and map coordinates), the group it belongs to, and the member who created it.
- **Organizer**: The group member who created a meetup, presented by display name. Distinct from the group's owner; being the organizer conveys no edit permission in the user interface.
- **Group Ownership**: The existing relationship that determines whether a user may modify a group's meetups through the interface. Sole input to whether the edit control appears.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On every supported device size, a user editing any text field on the Create or Edit Meetup page can reach the save action with a single tap while the keyboard is open — zero scrolling or keyboard-dismissal steps, in 100% of attempts.
- **SC-002**: Creating a meetup requires no interaction whose only purpose is to reach the save control, reducing the current minimum of two such steps (dismiss keyboard, scroll) to zero.
- **SC-003**: 100% of a group's members — owners and non-owners alike — can open a meetup's details from both the Home page and the Group Detail page.
- **SC-004**: All five pieces of meetup information (title, date/time, location, group, organizer) are visible on the details screen without scrolling on a standard phone screen.
- **SC-005**: In 100% of checks, users who do not own a meetup's group see no edit control on the details screen and cannot reach the edit form from it.
- **SC-006**: For meetups with an openable location, tapping the map control — on a card or on the details screen — opens the device's maps application at the correct location in at least 95% of attempts across supported platforms.
- **SC-010**: Every tap on a meetup card outside its map control opens the details screen: no tap lands on an unlabelled target with a different destination.
- **SC-007**: Tapping a meetup card presents the details screen quickly enough to feel immediate, with the meetup's information visible within one second under normal connectivity.
- **SC-008**: Zero regressions in meetup save behavior: every validation rule, error message, and success navigation that worked before the change still works after it.
- **SC-009**: Tapping a meetup card on the Group Detail page produces a visible response for 100% of group members, up from owners only.

## Assumptions

- **Save "busy" behavior**: The current forms have no visible busy or disabled state on the save button — the protection against double submission is internal to the save action. FR-004 preserves that protection as-is; adding a visible progress indicator is out of scope.
- **Editing stays owner-gated in the interface**: The data layer permits any group member to modify a group's meetups, while the interface restricts modification to group owners. This is an existing, deliberate decision (feature 006) that this feature preserves rather than revisits.
- **Organizer display name resolution**: Member display names are already visible to group members elsewhere in the app, so showing the organizer's name introduces no new exposure of user data. Where a name cannot be resolved — for instance because the creator has left the group — a neutral fallback is displayed.
- **Group name for the details screen**: The group's name is expected to be available from the context the user navigated from; where it is not, a fallback is shown rather than an empty field.
- **Read-only means read-only**: The details screen shows information and offers only the two navigation actions specified (open in maps, and edit for owners). Deleting a meetup remains available only where it is today, on the Group Detail page.
- **Past meetups**: No distinct presentation or restriction for meetups whose date has passed.
- **Icon style**: Text glyphs are used rather than new image files for the new edit and map affordances, matching the delete and accept actions already shipping in the app, because that presentation is already proven across all supported platforms. The existing save icon asset is reused for the save control.
- **Card location tapping (decided 2026-07-30)**: The cards' current behavior — where the location text is silently tappable and opens maps — is replaced by an explicit map control on the location row, with the rest of the card opening the details screen (FR-019, FR-020). This keeps one-tap directions available while making the target visible, rather than leaving an unlabelled tap boundary inside a card that now has a different primary destination.

## Out of Scope

- Any change to what a meetup stores, or to the rules about who may create, modify, or delete one.
- RSVP, attendance, comments, reminders, or sharing on the details screen.
- Adding a delete action to the details screen.
- Changing the Group Detail swipe-to-delete gesture, its styling, or its owner-only restriction.
- Changing which meetups appear on the Home page, or in what order.
- A visible progress indicator during save.
- Redesigning the meetup cards beyond making them tappable and adding the map control described in FR-019 and FR-020.

## Privacy & Safety Considerations

- **Data Minimization**: The details screen surfaces only information the viewer can already access as a member of the meetup's group — the meetup's own fields plus the organizer's display name, which group members already see in the group's member list. No email addresses, phone numbers, or other contact details are exposed.
- **Default Visibility**: Meetup details remain visible only to members of the owning group; this feature adds no route for a non-member to view a meetup.
- **Retention**: No new data is stored and no retention behavior changes.
- **Safety Controls**: Modifying a meetup continues to require group ownership in the interface, and the edit control's absence for non-owners is enforced by the same ownership check used elsewhere in the app rather than by hiding an otherwise-reachable action.
