# Feature Specification: Pull to Refresh on Home and Groups, and App Version on Profile

**Feature Branch**: `018-home-refresh-version`
**Created**: 2026-11-04
**Status**: Draft
**Input**: User description: "Home has no pull-to-refresh, unlike the group detail screen; add it so pulling down reloads upcoming meetups the same way, with the spinner always stopping even when the load fails. Add the same to the Groups list so it reloads owned and member groups; all three screens should behave alike. Show the installed app version at the bottom of Profile as 'Version 0.1.3 (4)': display version and store build number, read from the installed app. Out of scope: loading skeletons (019), any Api or database changes."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Refresh the Home screen by pulling down (Priority: P1)

A signed-in user is on Home and wants to see whether anything has changed, for example a meetup another member just created. They pull the list down, see the refresh indicator, and the upcoming meetups reload. Today the only way is to switch tabs and come back.

**Why this priority**: Home is the first screen after sign-in and the one people check most. Group detail already refreshes this way, so its absence on Home feels broken.

**Independent Test**: Create a meetup in one of the user's groups from another account, then pull down on Home. The new meetup appears without leaving the screen.

**Acceptance Scenarios**:

1. **Given** a user on Home, **When** they pull down, **Then** a refresh indicator shows and the upcoming meetups reload.
2. **Given** a meetup was added, changed or cancelled since Home loaded, **When** the user pulls down, **Then** Home shows the current list.
3. **Given** the reload fails (for example, no connection), **When** the user pulls down, **Then** the indicator stops, and Home behaves as it does today when loading fails.
4. **Given** a user with no upcoming meetups, **When** they pull down, **Then** the indicator shows and stops, and the "No upcoming meetings" message remains.

---

### User Story 2 - Refresh the Groups list by pulling down (Priority: P2)

A user who was just added to a group, or whose group was renamed, pulls down on the Groups tab and sees the current lists of groups they own and groups they're a member of.

**Why this priority**: It's the same gap as Home, and fixing it makes all three list screens (Home, Groups, group detail) behave alike.

**Independent Test**: Add the user to a group from another account, then pull down on the Groups tab. The group appears under the groups they're a member of.

**Acceptance Scenarios**:

1. **Given** a user on the Groups tab, **When** they pull down, **Then** a refresh indicator shows and both lists, owned groups and member groups, reload.
2. **Given** the reload fails, **When** the user pulls down, **Then** the indicator stops, and the screen behaves as it does today when loading fails.

---

### User Story 3 - See which version of the app is installed (Priority: P3)

A tester, or a user talking to support, opens Profile and reads the installed version at the bottom of the screen, for example "Version 0.1.3 (4)". This tells them which build they have, including builds with the same version but different build numbers.

**Why this priority**: It's useful for testing and support but doesn't change how anyone uses the app.

**Independent Test**: Install a build uploaded as version 0.1.3, build 4, and open Profile. The bottom of the screen reads "Version 0.1.3 (4)".

**Acceptance Scenarios**:

1. **Given** the installed build has version 0.1.3 and build number 4, **When** the user opens Profile, **Then** the bottom of the screen shows "Version 0.1.3 (4)".
2. **Given** a newer build is installed, **When** the user opens Profile, **Then** the text shows that build's version and number, with no other change to the app.

---

### Edge Cases

- **Pull while loading:** the user pulls down while the screen is already loading, for example straight after it appears. Only one reload runs at a time, and the indicator stops when it finishes.
- **Session ends:** the session expires during a refresh. The existing session handling applies, sending the user to sign-in, and the indicator doesn't stay spinning.
- **Account scheduled for deletion:** a refresh hits the Api's "scheduled for deletion" response. The existing scheduled-deletion screen takes over, as it does for any other request.
- **Short lists:** on Home and Groups, pull to refresh also works when the content is shorter than the screen.
- **Version on every build:** the version text appears for every build, including local development builds, which show whatever build number they were built with.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Pulling down on Home MUST reload the upcoming meetups, showing the platform's standard refresh indicator while it runs.
- **FR-002**: Pulling down on the Groups tab MUST reload both the owned-groups and member-groups lists, showing the same indicator.
- **FR-003**: The indicator MUST stop when the reload finishes, whether it succeeded or failed.
- **FR-004**: A failed refresh MUST leave the screen as a failed load does, and MUST NOT introduce error messages of its own. The one exception is the Groups list's network failure, which FR-009 replaces.
- **FR-005**: Pull to refresh on Home and Groups MUST look and behave like the existing pull to refresh on the group detail screen.
- **FR-006**: The bottom of Profile MUST show the installed app's version as `Version {display version} ({build number})`, for example `Version 0.1.3 (4)`.
- **FR-007**: The version and build number MUST be read from the installed app at runtime, never hard-coded, so each new build shows its own values without a code change.
- **FR-008**: This feature MUST NOT change the Api, the database or any stored data.
- **FR-009**: A failed Groups load, whether the first load or a refresh, MUST NOT close the app. It keeps whatever groups are listed and shows a dismissible message: "Couldn't load your groups. Check your connection and try again." Until now, a network error or timeout on this screen showed "Service Unavailable" and quit the app, so an offline pull to refresh would have closed it.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On Home and Groups, a change made from another account becomes visible after one pull down, without leaving the screen.
- **SC-002**: In 10 consecutive refreshes with the connection turned off, the indicator stops every time.
- **SC-003**: A tester can identify the installed build from the Profile screen alone, matching the version and build number of the upload, in under 10 seconds.
- **SC-004**: Home, Groups and group detail refresh the same way, from the same gesture with the same indicator, as checked on Android.

## Assumptions

- **Same mechanism everywhere:** Home and Groups refresh the way group detail does today. Home reloads only the upcoming meetups, and the greeting keeps coming from the cached profile as it does on every visit.
- **Build number:** the build number is the store build number passed when a release is built (the Android version code; on iOS the bundle version). Local debug builds show their default number.
- **Layout:** the version line is small, secondary text at the end of the Profile content, below everything else.
- **No new error UI:** refresh failures reuse each screen's current failure behaviour (Home shows its empty state). Adding error messages is out of scope.
- **Out of scope:** loading skeletons (019), refreshing other screens, and any Api or database changes.
- **Testing:** pull to refresh and the version label are native UI. They are verified with source-inspection tests, following project convention, plus a quickstart device check on Android. iOS is checked once 013 builds can run on a device.
