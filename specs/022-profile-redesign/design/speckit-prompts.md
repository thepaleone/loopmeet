# Spec Kit prompts: Profile screen redesign

Run these in order from the repo root on a clean `main`. Each block is the text to paste after the command. Design input lives in `plan/profile-redesign/` (see its README).

---

## 1. `/speckit.specify`

```
Redesign the Profile tab so no control can be cut off, and give it a cleaner, more modern layout. Before writing the spec, read plan/profile-redesign/README.md, look at every image in plan/profile-redesign/current/ and plan/profile-redesign/mockups/, and read plan/profile-redesign/ProfilePage.reference.xaml (illustrative only, not a decision). Then, as the first step of this feature, move plan/profile-redesign/ into the new feature directory as design/ (for example specs/022-profile-redesign/design/) and delete plan/, so the material is kept with the spec and CI's docs rule (specs/* selects nothing) applies.

Problem: ProfilePage.xaml lays its five actions out in two HorizontalStackLayout rows (Save, Change Password, Log out; Delete account, Notification settings). HorizontalStackLayout never wraps or shrinks, so on narrow screens, in some orientations and at larger text sizes the last buttons are clipped off the right edge (the Android capture shows Log out cut off in portrait). Destructive actions (Log out, Delete account) look identical to Save, and "User since: 2026-11-03" / "Groups: 5" read like debug output.

What to build, following the mockups:
- A header row with the "Profile" title and an icon-only Save, using the same pattern as Create/Edit Meetup from 011: a row outside the ScrollView, so the keyboard can't cover it, with a SemanticProperties description. Save is enabled only when the display name has changed and is not blank.
- A header card: a large circular avatar (the style guide's amber-to-pink gradient with the initial, or the photo) with a camera badge. Tapping the avatar picks a photo as today, with the upload indicator. Below it, the display name and email (both truncate rather than wrap), and two chips, "Member since <Mon YYYY>" and "<n> groups" (singular "1 group"), in a layout that wraps instead of clipping.
- An "Account" card of full-width rows: the display name edited in place; Change password (only when CanChangePassword, as today); and Notifications, with a subtitle showing the current permission state from NotificationPermissionService, re-read every time the page appears, that opens device settings as today.
- A second card with Log out (neutral styling) and Delete account (the style guide's soft terracotta, never harsh red), which opens the existing delete-account flow. Its subtitle must not state a day count, because the grace period already exists in two places (016).
- The status message and the version text stay at the bottom.
- Content is capped at about 600 px wide and centred, so it reads well in landscape and on wide screens.
- Every row and chip must grow with the system text size instead of clipping, in both orientations, on iOS and Android.

Out of scope: any Api, Supabase or database change; new profile fields or actions; the change-password, delete-account and notification flows themselves; dark-mode redesign; restyling other pages (the Create/Edit Meetup header pattern is reused, not changed).
```

---

## 2. `/speckit.clarify` (optional)

Run it if the spec leaves any of these open. Otherwise skip it.

```
Confirm in the spec: (a) Save stays in the in-page header row (011 pattern) rather than the navigation bar shown in the mockups; (b) the exact Delete account subtitle wording, with no day count; (c) whether landscape gets the single centred column or the two-column variant in mockups/ios_landscape.png, defaulting to the single column (Principle IV); (d) what the Notifications row shows for each NotificationPermissionState.
```

---

## 3. `/speckit.plan`

```
Plan the Profile redesign under .specify/memory/constitution.md, using the | Principle | Status | Notes | Constitution Check table that specs from 004 onward use. Read CLAUDE.md first, especially "Conventions" and "Known gotchas".

Constraints to carry into the plan:
- App only: LoopMeet.App Profile feature (ProfilePage.xaml, ProfileViewModel, Resources). No Api, Core, Infrastructure, Supabase or web changes. The App does not reference LoopMeet.Core, so don't add that reference to reach AccountDeletionPolicy.
- ProfileViewModel stays a sealed partial ObservableObject with [ObservableProperty]/[RelayCommand] and Async-suffixed command methods. New state (unsaved-changes flag, member-since text, group-count text, notification status text) is plain properties on the ViewModel. Add no new service or abstraction unless it has two real users (Principle IV).
- Add the terracotta colour to Resources/Styles/Colors.xaml under a descriptive key. Add the camera glyph next to the existing icons, following their naming (svg plus fallback png, where that's the existing pattern). Reuse existing icons elsewhere.
- Keep, or deliberately update, the source-inspection tests that read ProfilePage.xaml: ProfileViewModelTests (Ellipse, UserInitial, HasAvatar, LogoutCommand present; AvatarInput, "Avatar URL", AvatarSource absent; "Profile updated." in the ViewModel) and RefreshSurfaceTests (the VersionText label).
- Structured logging stays on save, avatar upload and logout, as today (Principle VII).
- Testing (Principle II): xUnit, no mocking framework; hand-rolled fakes under tests/LoopMeet.App.Tests/TestDoubles/.
  - ViewModel unit tests: unsaved-changes and blank-name rules, chip text (including "1 group"), and notification status mapping.
  - Source-inspection tests: no HorizontalStackLayout holding actions in ProfilePage.xaml; each action is bound to its existing command; the save control has a SemanticProperties description; the delete subtitle contains no digits.
- quickstart.md: a manual device matrix for what can't be unit tested. Cover iPhone SE-size and 6.9" iPhone, a small and a large Android phone, portrait and landscape, default and largest system text size, a Google/Apple account (no Change password row), and notifications on, off and not yet asked. Pass condition: nothing is clipped and every action works.
- data-model.md or contracts: state that there are no data or contract changes.
```

---

## 4. `/speckit.tasks`

```
Generate tasks.md grouped by user story so each story ships independently. Write tests before implementation in each story, and include the quickstart device-matrix run as the final task. Keep tasks.md as a living log: tick [x] in place as work completes.
```

---

## 5. `/speckit.analyze`, then `/speckit.implement`

```
/speckit.analyze: check spec, plan and tasks against each other and against the constitution, and fix any gaps before implementing.
/speckit.implement: work through tasks.md story by story, keeping dotnet test green after each story, with no dead or commented-out code and no TODOs. The CI scope is src/LoopMeet.App/* and tests/LoopMeet.App.Tests/*; no ci-scope.sh change should be needed once plan/ has moved under specs/.
```
