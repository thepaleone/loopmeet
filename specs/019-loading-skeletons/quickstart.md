# Quickstart: Consistent Loading Skeletons

These checks run on an Android device against staging. iOS runs the same checks once 013 builds can run on a device.

## Setup

- Install a build of this branch.
- Placeholders show only while a section is loading for the first time. To see them clearly, slow the connection:
  - In Developer options, set **Network → Mobile data / Wi-Fi throttling**, if the device offers it.
  - Otherwise, use a slow hotspot.
- Use account **A**, which has upcoming meetups and belongs to a group with several members and past meetups.

## Device checks

| # | Check | Expected | Result |
| --- | --- | --- | --- |
| 1 | Force-stop the app and cold-launch it signed in. | Home shows 3 placeholder meetup cards with a shimmer under the greeting. They're never blank. The real cards then replace them in the same place, at about the same size (SC-001, SC-004). | |
| 2 | Cold-launch as a user with no upcoming meetups. | Placeholders show, then give way to "No upcoming meetings". | |
| 3 | Turn on airplane mode, then cold-launch. | Placeholders show, then give way to Home's empty state. They never stay on screen (FR-006). | |
| 4 | Open a group. | 4 placeholder member tiles and 2 placeholder meetup cards show, then the real members and meetups (US2). | |
| 5 | In that group, switch to Past Meetups for the first time. | 2 placeholder meetup cards show in the past list, then the real past meetups. | |
| 6 | Switch back to Upcoming, then to Past again. | Each list's existing cards show while they refresh. No placeholders cover them. | |
| 7 | Open the Groups tab for the first time since launch. | 3 placeholder group cards over the owned section and 2 over the member section, then the groups (US3). | |
| 8 | Pull to refresh 10 times on each of Home, Groups and the group screen. | Only the spinner shows, and the listed cards stay. No placeholders appear (SC-003). | |
| 9 | Switch tabs away and back on Home and Groups. | The listed cards stay, with no placeholders. | |
| 10 | Repeat rows 1, 4 and 7 in dark theme. | The placeholders are visible but subtle, and the shimmer is visible. All three screens look alike (SC-005). | |
