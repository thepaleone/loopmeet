# Feature Specification: Consistent Loading Skeletons

**Feature Branch**: `019-loading-skeletons`
**Created**: 2026-11-04
**Status**: Draft
**Input**: User description: "Only the Groups list shows skeleton placeholder cards while loading, and it shows them on every load, even a pull to refresh. Home and group detail show nothing while loading, which is most obvious on a cold launch. Make all three screens behave the same: while a screen has no data yet, show skeleton cards shaped and sized like that screen's real cards, with the same shimmer. Reloads and pull to refresh keep the current cards and use the refresh spinner instead. One reusable skeleton component with a variant per card type, used on all three screens and replacing the Groups list's hand-written copies. Out of scope: Api or database changes, other screens."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See that Home is loading on a cold launch (Priority: P1)

A user opens the app. Until their upcoming meetups arrive, Home shows placeholder cards the size of meetup cards, with a gentle shimmer, instead of a blank space under the greeting. When the data arrives, the placeholders are replaced by the real meetups, or by the "No upcoming meetings" message.

**Why this priority**: Home is the first screen after launch, and a blank Home is where the missing loading state is most noticeable. Users can't tell whether the app is working or empty.

**Independent Test**: On a slow connection, cold-launch the app signed in. Placeholder meetup cards show until the meetups load, and the real cards then take their place.

**Acceptance Scenarios**:

1. **Given** a cold launch with nothing loaded yet, **When** Home appears, **Then** placeholder meetup cards with a shimmer show below the greeting.
2. **Given** placeholders are showing, **When** the meetups arrive, **Then** the placeholders disappear and the real meetup cards show in the same place.
3. **Given** placeholders are showing, **When** the load finds no upcoming meetups, **Then** the placeholders disappear and "No upcoming meetings" shows.
4. **Given** placeholders are showing, **When** the load fails, **Then** the placeholders disappear and Home shows what it shows today when loading fails. Placeholders never stay on screen indefinitely.

---

### User Story 2 - See that a group is loading (Priority: P2)

A user opens a group. Until its details arrive, the screen shows placeholder member tiles and placeholder meetup cards, shaped like the real ones. The first time the user switches to Past Meetups, the past list shows placeholder meetup cards until it loads.

**Why this priority**: It's the second most visible blank screen. Opening a group is a frequent action, and an empty members area looks like a group with no members.

**Independent Test**: On a slow connection, open a group. Placeholder member tiles and meetup cards show until the real ones replace them.

**Acceptance Scenarios**:

1. **Given** a group opened for the first time in this visit, **When** the screen appears, **Then** placeholder member tiles and placeholder meetup cards show until the members and upcoming meetups arrive.
2. **Given** the upcoming list has loaded, **When** the user switches to Past Meetups for the first time, **Then** placeholder meetup cards show in the past list until it loads.
3. **Given** any section's load fails, **When** it ends, **Then** that section's placeholders disappear, and the section shows what it shows today on failure.

---

### User Story 3 - The Groups list loads the same way as the other screens (Priority: P3)

The Groups list keeps its placeholder group cards on the first load. They now share one look and shimmer with Home and group detail. When the user pulls to refresh, or comes back to the tab, the groups already shown stay on screen and only the refresh spinner shows. The placeholders don't flash in place of the real cards.

**Why this priority**: It already has a loading state. What's wrong is consistency, and the placeholders flashing on every reload.

**Independent Test**: Open the Groups tab: placeholders show, then the groups. Pull to refresh: the groups stay visible with the spinner, and no placeholders appear.

**Acceptance Scenarios**:

1. **Given** the Groups tab has not loaded yet, **When** it appears, **Then** placeholder group cards show under both "owned" and "member" headings until the groups arrive.
2. **Given** groups are already listed, **When** the user pulls to refresh or returns to the tab, **Then** the listed groups stay visible and no placeholders appear.

---

### Edge Cases

- **Reload of a screen with data:** placeholders never replace real cards. They only appear while a section has nothing to show yet.
- **Empty result on reload:** a section that loaded with no items (an empty state) and is then reloaded keeps its empty message during the reload. It doesn't switch to placeholders.
- **Slow first load plus a pull:** the user pulls to refresh during a first load that's still showing placeholders. The placeholders stay until the data arrives, and the spinner stops then.
- **Session ends or deletion is pending:** the existing session handling or scheduled-deletion screen takes over. Placeholders must not stay visible behind it on return.
- **Light and dark themes:** placeholders and their shimmer must be clearly visible but subtle in both.
- **Motion:** the shimmer must stop when a screen is no longer visible, so it doesn't keep animating off screen.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: While Home has no meetups to show yet (its first load), it MUST show placeholder meetup cards matching the size and layout of a real upcoming-meetup card.
- **FR-002**: While a group's members haven't loaded yet, group detail MUST show placeholder member tiles matching the real tile: an avatar circle with a name line.
- **FR-003**: While a meetup list on group detail (upcoming, or past on its first selection) has no data yet, it MUST show placeholder meetup cards matching the real meetup card.
- **FR-004**: While the Groups list has no groups yet, it MUST show placeholder group cards matching the real group card, for both the owned and member sections.
- **FR-005**: Placeholders MUST appear only while a section has no data to show yet. A reload, a pull to refresh, or returning to a screen that already shows data MUST keep the current content and MUST NOT show placeholders.
- **FR-006**: When a load ends, whether with data, empty or failed, that section's placeholders MUST disappear, and the section MUST show its existing result or failure state.
- **FR-007**: All placeholders MUST share one look and one shimmer, and come from a single reusable component with a variant for each card type: meetup card, member tile and group card. The Groups list's existing placeholders are replaced by this component.
- **FR-008**: Each screen MUST show a fixed, small number of placeholders per section, enough to fill the visible area without a full list's worth.
- **FR-009**: The shimmer MUST run only while placeholders are visible on a visible screen.
- **FR-010**: This feature MUST NOT change the Api, the database, what data is loaded, or when it's loaded.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On a cold launch, Home never shows an empty area below the greeting. From the moment Home appears, it shows either placeholders, meetups or the "No upcoming meetings" message.
- **SC-002**: On Home, Groups and group detail, a user can tell within 1 second of a screen appearing that content is loading.
- **SC-003**: In 10 consecutive pull-to-refresh actions on each of the three screens, placeholders never replace content that was already showing.
- **SC-004**: When the real content replaces the placeholders, the cards land in the same place at roughly the same size, with no large jump in layout. This is checked by eye on Android.
- **SC-005**: Placeholders on all three screens look the same and shimmer the same way. A reviewer comparing them side by side in light and dark themes sees one consistent style.

## Assumptions

- **Fixed placeholder counts:**

  | Screen | Placeholders |
  | --- | --- |
  | Home | 3 meetup cards |
  | Group detail | 4 member tiles, 2 meetup cards |
  | Groups list | 3 owned and 2 member cards, as today |

- **Shimmer:** the existing Groups list shimmer, a light band sweeping across each placeholder, is the style kept and shared.
- **Screen chrome stays real:** the greeting on Home, the group name on group detail and section headings show their real or cached values, not placeholders. Only the data cards and tiles are placeholders.
- **"No data yet"** means the section hasn't completed a load since the screen was opened, or since the app started for Home and Groups. A completed load with zero items counts as data, and its empty state shows.
- **Out of scope:** other screens (invitations, meetup detail, profile), and any change to how or when data is fetched.
- **Testing:** placeholders are native UI. The show-only-without-data rule and the component's use on each screen are verified by unit and source-inspection tests, following project convention, plus a quickstart device check on Android with a slow connection. iOS is checked when 013 builds can run on a device.
