# Feature Specification: Group Meetups

**Feature Branch**: `006-group-meetups`
**Created**: 2026-03-30
**Status**: Draft
**Input**: User description: "Add a new feature to Groups that allows meetups to be added to a group. The meetup includes a title, date/time, and location. Location may be TBD. Location selection uses Google Maps API with autocomplete. Only group owner can create/edit in the UI, but RLS allows all members to edit/add (for future use). Add icon next to invite icon on Group page. Group page shows upcoming meetups below members list sorted by date. Home page replaces placeholder card with upcoming meetup cards."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Group Owner Creates a Meetup (Priority: P1)

A group owner wants to schedule a meetup for their group. They navigate to the group detail screen, tap a new "add meetup" action button (positioned alongside the existing invite button), and are taken to a meetup creation form. They enter a title, pick a date and time, and optionally search for a location. When they save, the meetup appears in the group's upcoming meetups list.

**Why this priority**: Creating meetups is the core capability of this feature. Without it, nothing else functions.

**Independent Test**: Can be fully tested by having a group owner create a meetup with title + date/time (location left as TBD) and verifying it appears in the group detail view.

**Acceptance Scenarios**:

1. **Given** a user is the owner of a group, **When** they tap the add meetup button on the group detail screen, **Then** a meetup creation form is displayed with fields for title, date, time, and location.
2. **Given** a group owner is on the meetup creation form, **When** they enter a title and select a date/time but leave location blank, **Then** the meetup is saved successfully with the location shown as "TBD".
3. **Given** a group owner is on the meetup creation form, **When** they begin typing a location name or address, **Then** a list of matching places is displayed in real-time as they type, sourced from a maps service.
4. **Given** a group owner selects a location from the autocomplete results, **When** the meetup is saved, **Then** the stored location contains enough detail (name, address, and geographic coordinates) to open the location in a maps application later.
5. **Given** a user is NOT the owner of a group, **When** they view the group detail screen, **Then** the add meetup button is not visible to them.

---

### User Story 2 - Group Members View Upcoming Meetups on Group Page (Priority: P1)

Any member of a group (including the owner) wants to see what meetups are coming up. On the group detail screen, below the members list, upcoming meetups are displayed in chronological order (nearest date first). Past meetups are excluded from this list.

**Why this priority**: Viewing meetups is equally critical — without visibility, creating them has no value for the group.

**Independent Test**: Can be tested by creating several meetups (some in the future, some in the past) and verifying only future meetups appear, sorted with the nearest date first.

**Acceptance Scenarios**:

1. **Given** a group has upcoming meetups, **When** any group member views the group detail screen, **Then** the meetups are listed below the members section, ordered by date with the soonest meetup first.
2. **Given** a group has meetups with dates in the past, **When** a member views the group detail screen, **Then** past meetups are not displayed.
3. **Given** a group has no upcoming meetups, **When** a member views the group detail screen, **Then** no meetups section is shown (or an appropriate empty state is displayed).
4. **Given** a meetup has a location set, **When** a member views the meetup in the list, **Then** the location name is displayed alongside the title and date/time.
5. **Given** a meetup has no location set, **When** a member views the meetup in the list, **Then** "TBD" is displayed in place of the location.

---

### User Story 3 - Home Page Shows Upcoming Meetups Across All Groups (Priority: P2)

A user wants to see all their upcoming meetups at a glance when they open the app. The home page replaces the current placeholder card with cards showing upcoming meetups from all of the user's groups. The most imminent meetup is listed first. If there are no upcoming meetups, a card indicates that there are no upcoming meetings.

**Why this priority**: Provides high-value at-a-glance information but depends on meetups existing first (P1 stories).

**Independent Test**: Can be tested by having a user who belongs to multiple groups with meetups, verifying all upcoming meetups appear on the home screen sorted by date, and verifying the empty state when none exist.

**Acceptance Scenarios**:

1. **Given** a user belongs to groups that have upcoming meetups, **When** they view the home screen, **Then** meetup cards are displayed sorted by date with the soonest first, each showing the meetup title, date/time, location (or TBD), and group name.
2. **Given** a user belongs to groups but none have upcoming meetups, **When** they view the home screen, **Then** a card is displayed indicating there are no upcoming meetings.
3. **Given** a user belongs to no groups, **When** they view the home screen, **Then** the same "no upcoming meetings" card is displayed.
4. **Given** a meetup's date passes, **When** the user next views the home screen, **Then** that meetup no longer appears in the list.

---

### User Story 4 - Group Owner Edits a Meetup (Priority: P2)

A group owner needs to change the details of an upcoming meetup — for example, updating the location from TBD to a specific place, changing the date/time, or modifying the title. They can tap on an existing meetup from the group detail screen to open an edit form.

**Why this priority**: Editing is a natural follow-on to creation, especially since meetups may initially be created with TBD locations.

**Independent Test**: Can be tested by creating a meetup, then editing each field (title, date/time, location) and verifying the changes are reflected in the group detail view.

**Acceptance Scenarios**:

1. **Given** a group owner views the group detail screen, **When** they tap on an existing upcoming meetup, **Then** an edit form opens pre-populated with the meetup's current details.
2. **Given** a group owner is editing a meetup, **When** they change the title, date/time, or location and save, **Then** the updated details are reflected in the group meetups list.
3. **Given** a group owner is editing a meetup with a location set, **When** they clear the location, **Then** the meetup reverts to showing "TBD" for location.
4. **Given** a user is NOT the owner of the group, **When** they tap on a meetup in the group detail screen, **Then** no edit option is available (view-only).

---

### User Story 5 - Location Search with Maps Autocomplete (Priority: P2)

When creating or editing a meetup, the group owner can search for a location by typing a name or address. As they type, a filtered list of matching places appears. Selecting a result populates the location with enough information (place name, formatted address, and geographic coordinates) to later open the location in an external maps application.

**Why this priority**: Enhances the meetup creation/edit experience but the feature is still usable without it (locations can be left as TBD).

**Independent Test**: Can be tested by typing partial location names/addresses and verifying autocomplete results appear, then selecting one and verifying the saved location contains name, address, and coordinates.

**Acceptance Scenarios**:

1. **Given** a user is on the meetup form (create or edit), **When** they begin typing in the location field, **Then** autocomplete suggestions appear after a short delay, filtered by the input text.
2. **Given** autocomplete results are displayed, **When** the user selects a result, **Then** the location field is populated with the place name and address, and geographic coordinates are stored.
3. **Given** the user has selected a location, **When** they clear the location field, **Then** the autocomplete resets and the location reverts to empty (TBD on save).
4. **Given** the maps autocomplete service is unavailable, **When** the user types in the location field, **Then** an appropriate message indicates that location search is temporarily unavailable, but the user can still save the meetup without a location.

---

### User Story 6 - Group Owner Deletes a Meetup (Priority: P2)

A group owner decides to cancel or remove an upcoming meetup. From the group detail screen's meetup list, they swipe on a meetup to reveal a delete action — using the same swipe gesture pattern established for accepting/declining invitations. Before the deletion executes, a confirmation dialog asks the owner to confirm. Once confirmed, the meetup is removed from all views.

**Why this priority**: Deletion completes the meetup lifecycle management. Owners need to be able to remove meetups that are cancelled or created in error.

**Independent Test**: Can be tested by creating a meetup, swiping to delete it, confirming the deletion, and verifying it no longer appears in the group detail or home screen.

**Acceptance Scenarios**:

1. **Given** a group owner is viewing the meetup list on the group detail screen, **When** they swipe on an upcoming meetup, **Then** a delete action is revealed (consistent with the invitation swipe pattern).
2. **Given** the owner has triggered the delete swipe action, **When** the action fires, **Then** a confirmation dialog appears asking the owner to confirm deletion before the meetup is removed.
3. **Given** the confirmation dialog is displayed, **When** the owner confirms, **Then** the meetup is deleted and immediately removed from the group detail and home screen views.
4. **Given** the confirmation dialog is displayed, **When** the owner cancels, **Then** the meetup is not deleted and the swipe resets.
5. **Given** a user is NOT the owner of the group, **When** they view the meetup list, **Then** no swipe-to-delete action is available.

---

### Edge Cases

- What happens when a meetup is created with a date/time in the past? The system should prevent saving a meetup with a past date/time during creation.
- What happens when the user's device has no internet connectivity during location search? The autocomplete should gracefully degrade and the user should be able to save without a location.
- What happens when two meetups are scheduled for the exact same date and time? Both should be displayed — there is no uniqueness constraint on date/time.
- What happens when the group owner creates a meetup and then another member opens the group page? The new meetup should be visible after the member navigates to the group detail.
- What happens when a meetup is right at the boundary (happening now)? Meetups should be considered "upcoming" until their scheduled date/time has passed.
- What happens when an owner deletes a meetup while another member is viewing the group page? The deleted meetup should no longer appear when the member next refreshes or navigates to the group detail.
- What happens if the deletion confirmation is interrupted (e.g., app backgrounded)? The meetup should remain intact — deletion only occurs after explicit confirmation.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow group owners to create meetups within a group, with a title (required), date and time (required), and location (optional).
- **FR-002**: System MUST display a location autocomplete powered by a maps service when the user types in the location field during meetup creation or editing.
- **FR-003**: System MUST store selected locations with sufficient detail (place name, formatted address, latitude, longitude) to open the location in an external maps application.
- **FR-004**: System MUST display "TBD" for meetups where no location has been set.
- **FR-005**: System MUST display upcoming meetups (future date/time only) on the group detail screen below the members list, sorted by date ascending (soonest first).
- **FR-006**: System MUST exclude past meetups from display on both the group detail screen and the home screen.
- **FR-007**: System MUST allow group owners to edit existing upcoming meetups (title, date/time, location).
- **FR-008**: System MUST restrict meetup creation, editing, and deletion in the UI to the group owner only.
- **FR-009**: System MUST allow all group members (including non-owners) to view meetup information.
- **FR-010**: Data access policies MUST allow all group members to create, edit, and delete meetups at the data layer, to support future features where non-owners can manage meetups.
- **FR-011**: System MUST display an add meetup action on the group detail screen, positioned alongside the existing invite action, visible only to the group owner.
- **FR-012**: System MUST replace the home screen placeholder card with a list of upcoming meetup cards across all of the user's groups, sorted by date ascending.
- **FR-013**: System MUST display a "no upcoming meetings" card on the home screen when the user has no upcoming meetups across any of their groups.
- **FR-014**: System MUST prevent creation of meetups with a date/time in the past.
- **FR-015**: Each meetup card (on both group detail and home screens) MUST display the meetup title, date/time, and location (or "TBD").
- **FR-016**: Meetup cards on the home screen MUST also display the group name so users can distinguish which group the meetup belongs to.
- **FR-017**: System MUST allow group owners to delete upcoming meetups via a swipe gesture on the group detail screen only (home page is view-only), using the same swipe interaction pattern as invitation accept/decline.
- **FR-018**: System MUST display a confirmation dialog before executing a meetup deletion, requiring the owner to explicitly confirm the action.
- **FR-019**: System MUST immediately remove a deleted meetup from all views (group detail and home screen) upon confirmed deletion.
- **FR-020**: System MUST open the device's default maps application when a user taps on a meetup's location (on both group detail and home screen), using the stored coordinates and place information.

### Key Entities

- **Meetup**: Represents a scheduled group gathering. Key attributes: title, scheduled date/time, location (optional — place name, address, latitude, longitude), association to a group, and the user who created it.
- **Group** (existing): A meetup belongs to exactly one group. A group can have many meetups.
- **Membership** (existing): Determines which users can view meetups. All members of a group can see that group's meetups.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Group owners can create a meetup (with title, date/time, and optional location) in under 60 seconds.
- **SC-002**: Location autocomplete displays relevant suggestions within 2 seconds of the user pausing their input.
- **SC-003**: All group members can view upcoming meetups on the group detail screen immediately after navigating to it.
- **SC-004**: The home screen displays upcoming meetups from all of a user's groups upon loading, replacing the former placeholder content.
- **SC-005**: Past meetups are never displayed on the group detail screen or home screen.
- **SC-006**: 100% of saved locations contain sufficient information to open in an external maps application.
- **SC-007**: Users with no upcoming meetups see a clear "no upcoming meetings" message on the home screen.
- **SC-008**: Deleted meetups are immediately removed from all views and cannot be recovered.

## Clarifications

### Session 2026-03-30

- Q: Should owners be able to swipe-to-delete meetups from the home page cards, or only from the group detail page? → A: Group detail page only — home page is view-only.
- Q: Should tapping a meetup's location open the device's default maps application? → A: Yes — tapping a location opens it in the device's maps app.

## Assumptions

- The Google Maps Places API (or equivalent maps autocomplete service) will be used for location search. The specific service is an implementation detail, but the feature requires a maps autocomplete provider.
- Location coordinates (latitude/longitude) are the standard mechanism for opening locations in external maps applications across mobile platforms.
- The existing invite action on the group detail screen will be adapted to accommodate a second action (add meetup) — the specific UI pattern is an implementation decision.
- Meetup time zones are stored based on the device's local time zone at the time of creation. Time zone handling details are an implementation concern.
- There is no limit on the number of meetups per group.
- Meetup deletion is a hard delete (permanently removed from the data store). There is no soft-delete or archive requirement for this initial release.
- The home screen meetup list does not need pagination for the initial release, as meetup volume is expected to be low.
