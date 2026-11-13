# Quickstart: Profile Redesign Device Checks

The automated tests cover the rules (Save, chip texts, notification text, the 800-point switch) and the page's structure. These checks cover what only a device shows: clipping, wrapping, text size, rotation and the keyboard.

**Pass condition for every row:** nothing is clipped at a screen edge, and every action visible in that state works.

## Setup

- **Accounts:**
  - an email and password account in 2+ groups;
  - one in exactly 1 group;
  - a Google or Apple account, which has no Change password row.
- **Text size:**
  - iOS: Settings → Accessibility → Display & Text Size → Larger Text, with Larger Accessibility Sizes on and the slider at maximum.
  - Android: Settings → Display → Font size at maximum, and Display size at maximum where offered.
- **Notification states:**
  - **On:** allowed.
  - **Off:** denied in device settings.
  - **Not yet asked:** a fresh install, before the first sign-in prompt is answered.
- **Builds:** a Debug build from this branch: `dotnet build src/LoopMeet.App -t:Run -f net10.0-ios` for a simulator or device, or `deploy/deploy-android.sh -c Debug`.

## A. Layout matrix

Open Profile, scroll to the bottom, and rotate. Record ✅, or what was clipped.

| # | Device | Orientation | Text size | Expected layout | Result |
| --- | --- | --- | --- | --- | --- |
| A1 | iPhone SE size (375 points wide) | portrait | default | one column | ✅ |
| A2 | iPhone SE size | portrait | largest | one column; rows and chips taller, nothing clipped | ✅ |
| A3 | iPhone SE size | landscape (667) | default | one column, centred, at most 600 wide | ✅ |
| A4 | iPhone SE size | landscape | largest | one column | ✅ |
| A5 | 6.9" iPhone (440 points wide) | portrait | default | one column | ✅ |
| A6 | 6.9" iPhone | portrait | largest | one column | ✅ |
| A7 | 6.9" iPhone | landscape (about 956) | default | two columns, header card on the left | ✅ |
| A8 | 6.9" iPhone | landscape | largest | two columns; the right column scrolls with the page | ✅ |
| A9 | small Android phone (about 360 dp, the clipped case today) | portrait | default | one column, Log out fully visible | ✅ |
| A10 | small Android phone | portrait | largest | one column | ✅ |
| A11 | small Android phone | landscape (under 800 dp) | default | one column | ✅ |
| A12 | small Android phone | landscape | largest | one column | ✅ |
| A13 | large Android phone (about 412 dp) | portrait | default | one column | ✅ |
| A14 | large Android phone | portrait | largest | one column | ✅ |
| A15 | large Android phone | landscape (over 800 dp) | default | two columns | ✅ |
| A16 | large Android phone | landscape | largest | two columns | ✅ |
| A17 | iPad | portrait and landscape | default | two columns, at most 1100 wide and centred | ✅ |
| A18 | iPad | landscape | largest | two columns | ✅ |

## B. Behaviour

| # | Check | Expected | Result |
| --- | --- | --- | --- |
| B1 | Open Profile, edit nothing, then tap into the display name and type | Save is dimmed and tapping it does nothing. The field accepts typing while Save is dimmed | ✅ |
| B2 | Change the display name | Save turns active. The header card still shows the saved name | ✅ |
| B3 | Clear the name, then type only spaces | Save is inactive both times | ✅ |
| B4 | Change the name, then type the original back | Save is inactive | ✅ |
| B5 | Change the name, then tap Save while the keyboard is open (portrait and landscape) | Save is visible above the keyboard. "Profile updated." appears, the header card shows the new name, and Save dims | ✅ |
| B6 | Change the name, then press Return or Done on the keyboard | The keyboard closes and nothing is saved. Save stays active | ✅ |
| B7 | Turn on airplane mode, change the name, tap Save | The existing error appears, the edit stays, and Save stays active. Turn airplane mode off and tap Save: it saves | ✅ |
| B8 | Change the name, then rotate across the two-column switch (6.9" iPhone or iPad) | The layout switches and the edited name is still in the field | ✅ |
| B9 | VoiceOver and TalkBack: move focus to Save | It's announced as "Save profile" | ✅ |
| B10 | Tap the avatar | The photo picker opens. After choosing a photo, the spinner shows on the avatar until the photo appears | ✅ |
| B11 | Account with a photo, and one without | The photo fills the circle, or the initial shows on the amber-to-pink gradient. The camera badge shows in both | ✅ |
| B12 | Account with a very long name and email | Each stays on one line with an ellipsis | ✅ |
| B13 | Account in 1 group, and one in 2 or more | "1 group" and "N groups". "Member since" shows the join month and year | ✅ |
| B14 | Email and password account, then a Google or Apple account | Change password is shown, then not shown. Tapping it opens the existing change-password page | ✅ |
| B15 | Notifications on | The subtitle reads "On · Manage in device settings" | ✅ |
| B16 | Notifications off (denied) | "Off · Turn on in device settings" | ✅ |
| B17 | Notifications not yet asked | "Off · Turn on in device settings" | ✅ |
| B18 | From Profile, tap Notifications, change the setting, and return to the app with the app switcher | The subtitle shows the new state without leaving Profile | ✅ |
| B19 | Tap Log out | Signed out, back at the sign-in page | ✅ |
| B20 | Tap Delete account | The existing delete-account page opens, and nothing is deleted. The subtitle reads "Review what's deleted before you confirm" | ✅ |
| B21 | Look at Log out and Delete account next to Save | Log out is neutral and Delete account is terracotta. Neither is amber | ✅ |
| B22 | Dark mode, one device | Every text is readable, including Delete account | ✅ |
| B23 | Scroll to the bottom | The status message (after a save) and then the version text are last | ✅ |

## Results

Record the date, build and device for each run below.

| Date | Build | Devices | Rows passed | Notes |
| --- | --- | --- | --- | --- |
| 2026-11-13 | `022-profile-redesign` at `2b85167` (Debug) | The A matrix devices (iPhone SE size, 6.9" iPhone, small and large Android phones, iPad) | 41 of 41 (A1–A18, B1–B23) | Run by the maintainer. A11, A13, A15 and B10 were left blank in the table and recorded as passed from the maintainer's report that every row passed. A17 confirms the iPad layout is centred, so T012's fallback wasn't needed. |
