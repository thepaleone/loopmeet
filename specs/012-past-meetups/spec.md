# Feature Specification: Past Meetups on Group Detail

**Feature Branch**: `012-past-meetups`  
**Created**: 2026-10-30  
**Status**: Draft  
**Input**: User description: "View and maintain past meetups from Group Detail: an Upcoming/Past toggle replaces the static 'Upcoming Meetups' label; past meetups list most-recent-first with paging; a meetup becomes past at the end of its scheduled day in its own timezone; group owners can edit or delete past meetups without group members receiving push notifications, unless an edit moves the meetup back into the future."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Browse a group's past meetups (Priority: P1)

A group member opens a group and wants to look back at meetups the group has already held — where they went, when, and who organized. Above the meetup list they see a two-option toggle, "Upcoming Meetups" (selected) and "Past Meetups". Selecting "Past Meetups" shows the group's past meetups, most recent first. Scrolling to the end of the loaded meetups reveals older ones until the group's entire history has been shown. Tapping a past meetup opens its detail page.

**Why this priority**: This is the core value of the feature — a group's history is currently invisible once a meetup's time passes. Everything else builds on being able to see past meetups.

**Independent Test**: Create a group with several meetups dated on different past days and some in the future. Open Group Detail, switch to "Past Meetups", and confirm only past meetups appear, newest first, that older ones load as you reach the end of the list, and that tapping one opens its detail.

**Acceptance Scenarios**:

1. **Given** a member opens Group Detail, **When** the page appears, **Then** the toggle is visible with "Upcoming Meetups" selected and the upcoming list showing, regardless of which option was selected on a previous visit.
2. **Given** the group has past meetups on 3 different days, **When** the member selects "Past Meetups", **Then** those meetups are listed with the most recent scheduled date/time first, using the same card layout as upcoming meetups.
3. **Given** the group has more past meetups than fit in one page (about 20), **When** the member reaches the end of the loaded list, **Then** the next page of older meetups is loaded and appended, until no more remain.
4. **Given** the "Past Meetups" list is showing, **When** the member taps a past meetup, **Then** that meetup's detail page opens and shows its details (it is not treated as missing).
5. **Given** the group has no past meetups, **When** the member selects "Past Meetups", **Then** the toggle remains visible and a message such as "No past meetups yet" is shown.
6. **Given** the group has no upcoming meetups, **When** the page opens, **Then** the toggle is still visible and the upcoming list shows its own empty-state message, so the member can still reach the past list.
7. **Given** the member is viewing one list, **When** they switch to the other option, **Then** only the meetup list changes; the group's name, members, and other page content do not reload.
8. **Given** the member is viewing the past list, **When** they pull to refresh, **Then** the past list is refreshed (and the upcoming list is not reloaded).

---

### User Story 2 - Correct a past meetup's details without alerting the group (Priority: P2)

A group owner notices that a past meetup recorded the wrong venue (for example, the group moved to a different restaurant on the night). They open the past meetup, edit the location, and save. The record is now accurate, and group members do not get a push notification about an event that already happened.

**Why this priority**: Keeping history accurate is the reason past meetups are editable, and suppressing pointless alerts is an explicit requirement. It depends on Story 1 for reaching the past meetup.

**Independent Test**: As a group owner, open a meetup from a previous day, change its location (and separately its title or time to another past time), and save. Confirm the change is saved and that no group member device receives a notification.

**Acceptance Scenarios**:

1. **Given** the owner is viewing the detail of a past meetup, **When** the page loads, **Then** the edit control is offered to the owner (and not to non-owner members), exactly as for upcoming meetups.
2. **Given** the owner edits a past meetup's location, title, or other details without changing its date/time, **When** they save, **Then** the save succeeds even though the meetup's time has passed.
3. **Given** the owner changes a past meetup's date/time to another time that is also past, **When** they save, **Then** the save succeeds and the meetup stays in the past list, positioned by its new date.
4. **Given** a meetup was past before an edit and is still past after it, **When** the edit is saved, **Then** no group member receives a push notification for that edit.
5. **Given** the owner deletes a past meetup, **When** the action completes, **Then** the meetup is removed from the past list and no group member receives a push notification.

---

### User Story 3 - Move a past meetup back into the future (Priority: P3)

A group owner wants to repeat or reschedule a meetup that has already passed by moving its date into the future. After saving, the meetup reappears in the upcoming list and group members are notified, just like any other change to an upcoming meetup.

**Why this priority**: Less common than correcting history, but it must behave predictably because it crosses from "no alerts" back to "normal alerts".

**Independent Test**: As a group owner, edit a past meetup and set its date to next week. Confirm it moves to the upcoming list and that group members receive the normal "meetup updated" notification.

**Acceptance Scenarios**:

1. **Given** the owner edits a past meetup and sets its date/time in the future, **When** they save, **Then** the save succeeds, the meetup leaves the past list, and it appears in the upcoming list (on Group Detail and on the Home tab).
2. **Given** a meetup was past before an edit and upcoming after it, **When** the edit is saved, **Then** group members receive the same "meetup updated" notification they receive for any upcoming meetup change.
3. **Given** the meetup was moved into the future, **When** its new day arrives, **Then** members receive the usual same-day reminders for it.

---

### User Story 4 - A meetup stays "upcoming" for the rest of its day (Priority: P3)

A member opens the app during or shortly after a meetup that started earlier the same day. The meetup is still listed under Upcoming (on Group Detail and the Home tab) until the day ends in the meetup's timezone, so people can still find directions and details while the event is happening. After midnight it moves to Past.

**Why this priority**: Defines the single cutoff that every other behavior depends on, and changes the current behavior (meetups currently vanish from upcoming lists the moment they start).

**Independent Test**: Create a meetup scheduled one hour ago today. Confirm it appears under Upcoming on both Group Detail and Home and not under Past; after the end of that day in the meetup's timezone, confirm it appears only under Past.

**Acceptance Scenarios**:

1. **Given** a meetup started earlier today (in its timezone), **When** a member views Group Detail or the Home tab, **Then** it appears in the upcoming list and not the past list.
2. **Given** a meetup's scheduled day has ended in its timezone, **When** a member views Group Detail, **Then** it appears in the past list and not the upcoming list, and it no longer appears on the Home tab.
3. **Given** a meetup that started earlier today is still upcoming, **When** the owner edits other details without changing the time, **Then** the save succeeds and members are notified as for any upcoming meetup change.
4. **Given** a meetup that started earlier today is still upcoming, **When** the owner changes its time to a different time that has already passed, **Then** the save is rejected with the existing "time must be in the future" message.

---

### Edge Cases

- **Meetup timezone not set**: The meetup is classified using the fallback timezone defined in Assumptions, so every viewer sees it in the same list regardless of their own device timezone.
- **Viewer in a different timezone than the meetup**: Classification depends only on the meetup's timezone, not the viewer's. A member in another timezone may see a meetup stay "upcoming" past their own midnight; this is expected.
- **Meetup crosses midnight while the page is open**: The lists are not live-updated; the meetup moves to the past list the next time the list is loaded or refreshed.
- **Edit that is saved just as the day ends**: Whether the meetup was "past before the edit" is judged at the moment the change is processed; an edit processed after midnight to a meetup that is still past after the edit sends no notification.
- **Past → future → past**: A meetup moved into the future and later moved back into the past follows the rules for each individual edit (notify on the first, not on the second). No reminders are ever sent for a day that has already ended.
- **Paging while data changes**: If a meetup is added, edited, or deleted between page loads, the list must not show the same meetup twice; a refresh resets the list to the first page.
- **Paging failure**: If loading a further page fails, the already-loaded meetups stay visible and the member can retry loading more.
- **Non-owner member opens a past meetup**: They can view it but are not offered edit or delete, matching current permissions.
- **Group with a long history** (hundreds of meetups): The first page appears as quickly as the upcoming list does; older meetups load progressively.

## Requirements *(mandatory)*

### Functional Requirements

#### Upcoming / Past toggle

- **FR-001**: Group Detail MUST replace the static "Upcoming Meetups" heading with a two-option toggle labeled "Upcoming Meetups" and "Past Meetups".
- **FR-002**: The toggle MUST default to "Upcoming Meetups" every time Group Detail is opened; the previous selection is not remembered.
- **FR-003**: The toggle MUST be visible whenever Group Detail is showing, including when one or both lists are empty.
- **FR-004**: Each list MUST show its own empty-state message when it has no meetups (e.g. "No upcoming meetups" / "No past meetups yet").
- **FR-005**: Switching the toggle MUST change only the meetup list; other Group Detail content MUST NOT reload.
- **FR-006**: Pull-to-refresh MUST refresh the currently selected list only.

#### Past / upcoming classification

- **FR-007**: A meetup MUST be classified as **past** once the calendar day of its scheduled time has ended (midnight) in the meetup's timezone, and as **upcoming** until then.
- **FR-008**: The same classification rule MUST be used for the Group Detail upcoming list, the Group Detail past list, the Home tab's upcoming list, edit validation, and notification decisions, so a meetup is never in both lists or in neither.
- **FR-009**: The Home tab's upcoming list MUST keep meetups that started earlier the same day (per FR-007), instead of dropping them when they start.

#### Past list

- **FR-010**: The past list MUST show the group's past meetups ordered by scheduled date/time, most recent first.
- **FR-011**: The past list MUST load in pages of about 20 meetups and load the next page when the member reaches the end of the loaded list, until the group's whole history is reachable.
- **FR-012**: Past meetups MUST use the same card layout and information as upcoming meetups.
- **FR-013**: Tapping a past meetup MUST open its detail page.
- **FR-014**: Paging MUST NOT show duplicate meetups, and a refresh MUST restart the list from the first page.

#### Meetup detail

- **FR-015**: Meetup detail MUST display a meetup whether it is upcoming or past; a past meetup MUST NOT be treated as missing.
- **FR-016**: Edit and delete permissions for past meetups MUST be the same as for upcoming meetups (group owner only).

#### Editing past meetups

- **FR-017**: The group owner MUST be able to edit any field of a past meetup, including its date/time.
- **FR-018**: Saving a past meetup with a date/time that is still in the past MUST succeed.
- **FR-019**: Saving a past meetup with a future date/time MUST succeed and make it upcoming again.
- **FR-020**: For an upcoming meetup, the "time must be in the future" rule MUST apply only when the date/time is being changed; saving an upcoming meetup whose time has already started today without changing its date/time MUST succeed.
- **FR-021**: The edit rules MUST be enforced identically in the app and on the server, so the app never offers a save the server rejects and vice versa.
- **FR-021a**: An edit that doesn't change a meetup's date/time MUST NOT change the timezone it's classified in. Otherwise a title or location edit could move it between lists, or change whether it notifies, without its time changing.
- **FR-022**: Deleting a past meetup MUST be allowed for the group owner.

#### Push notifications

- **FR-023**: Editing a meetup that is past both before and after the edit MUST NOT send any push notification to group members.
- **FR-024**: Editing a meetup that was past before the edit and is upcoming after it MUST send the same "meetup updated" notification as any upcoming-meetup edit.
- **FR-025**: Deleting a meetup that was already past MUST NOT send any push notification.
- **FR-026**: Creating, editing and deleting upcoming meetups MUST notify exactly as they do today.
- **FR-027**: Same-day meetup reminders MUST NEVER be sent for a meetup whose day has already ended, including meetups that were moved between past and future.
- **FR-028**: Notification suppression decisions MUST be recorded in the existing structured logs (meetup, event type, reason suppressed) so a missing alert can be explained.

#### Quality

- **FR-029**: Regression tests MUST cover both sides of the end-of-day cutoff for: list classification, edit validation, and notification decisions (past→past edit, past→future edit, past delete, upcoming edit).

### Key Entities

- **Meetup** (existing, unchanged): a group's planned get-together with a title, scheduled date/time, optional location, optional timezone, and the group it belongs to. "Upcoming" and "past" are derived from the scheduled date/time and timezone at read time; they are not stored.
- **Meetup list page** (new, derived): one slice of a group's past meetups in most-recent-first order, plus an indication of whether older meetups remain.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A member can go from opening a group to viewing the detail of any past meetup within that group in under 30 seconds for groups with up to 100 past meetups.
- **SC-002**: The first page of past meetups appears within the same time as the upcoming list does today (target: under 2 seconds on a typical mobile connection).
- **SC-003**: In test scenarios covering every edit and delete case, 0 notifications are delivered for changes to meetups that stay past, and 100% of the expected notifications are delivered for meetups moved into the future and for upcoming meetups.
- **SC-004**: For every meetup tested at and around the end-of-day cutoff, it appears in exactly one of the two lists (never both, never neither), and the Home tab agrees with Group Detail.
- **SC-005**: A group owner can correct a past meetup's location and save it on the first attempt, with no validation error, in 100% of manual test runs.

## Assumptions

- **Timezone fallback**: When a meetup has no timezone, classification uses the same app-wide default timezone the reminder scheduler already falls back to (America/Los_Angeles). The per-recipient profile timezone is not used for classification because the lists are shared by all members and must agree.
- **Cancel is not a feature**: meetups have no canceled state today (the only way to remove one is delete). If a cancel state is added later, canceling a past meetup must also send no push (the push check already runs before any cancel handling).
- **Timezone on edit**: today every edit overwrites the meetup's timezone with the editor's device timezone. After this feature, the timezone is only updated when the date/time is changed (FR-021a).
- **Hard deletes** already send no notification today; FR-025 confirms that behavior.
- **No data changes**: no new stored fields are expected. If the timezone-aware cutoff or paging needs a supporting database index or function to perform well, planning must call that out explicitly.
- **Permissions** are unchanged: viewing past meetups follows existing group membership rules; editing and deleting stay group-owner-only in the app (database-level rules are intentionally unchanged, per feature 006).
- **Out of scope**: canceling meetups, attendance history, photos or notes on past meetups, searching or filtering past meetups, and a past-meetups view on the Home tab.
- **Device verification**: because notification delivery is native behavior, a manual device check (edit/delete a past meetup → no push; move a past meetup into the future → push) is part of acceptance, alongside automated tests.
