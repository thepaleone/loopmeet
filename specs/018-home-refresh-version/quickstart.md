# Quickstart: Pull to Refresh and App Version

These checks run on an Android device against staging. iOS runs the same checks once 013 builds can run on a device.

## Build

Build with a known build number so row 7 can be checked, for example `deploy/build-android-release.sh -b 5` or `deploy/deploy-android.sh`. The display version comes from `ApplicationDisplayVersion` in `src/LoopMeet.App/LoopMeet.App.csproj`.

Use two accounts: **A**, on the device, and **B**, on another device or the web dashboard, who owns a group A belongs to.

## Device checks

| # | Check | Expected | Result |
| --- | --- | --- | --- |
| 1 | A on Home. B creates a meetup in the shared group. A pulls down on Home. | The spinner shows, then stops, and the new meetup is listed without leaving Home (SC-001). | |
| 2 | A with no upcoming meetups pulls down on Home. | The spinner shows and stops, and "No upcoming meetings" stays. | |
| 3 | Airplane mode on. A pulls down on Home 10 times. | The spinner stops every time, and Home shows its empty state as for any failed load (SC-002). | |
| 4 | A on Groups. B adds A to another group. A pulls down on Groups. | The spinner shows and stops, and the group appears under groups A is a member of (SC-001). | |
| 5 | Airplane mode on. A pulls down on Groups, and also cold-launches straight to Groups. | The spinner stops. "Couldn't load your groups. Check your connection and try again." shows with OK, the groups already listed stay, and the app never closes (FR-009). | |
| 6 | Open Home and pull down at once, while the first load is still running. | Only one load runs (no duplicate cards), and the spinner stops when it finishes. | |
| 7 | Open Profile. | The bottom reads `Version {display version} ({build number})`, for example `Version 0.1.4 (5)` for a `-b 5` build of 0.1.4 (SC-003). | |
| 8 | Compare pull to refresh on Home, Groups and a group's detail screen. | Same gesture, same spinner (SC-004). | |
