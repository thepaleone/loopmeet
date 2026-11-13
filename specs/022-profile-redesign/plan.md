# Implementation Plan: Profile Screen Redesign

**Branch**: `022-profile-redesign` | **Date**: 2026-11-13 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/022-profile-redesign/spec.md`. Design input: [design/](design/).

## Summary

Rebuild the Profile tab so no control can be clipped.

- **Actions become full-width rows in cards.** Today the actions sit in two `HorizontalStackLayout` button rows, which never wrap.
- **Save:** moves to the 011 header row, outside the `ScrollView`. It's active only when the display name has changed and isn't blank.
- **Header card:** shows the saved name, the email, and the "Member since Nov 2026" and "N groups" chips, which wrap.
- **Notifications row:** shows On or Off, refreshed on appear and on app resume.
- **Log out and Delete account:** get their own card. Delete account is terracotta.
- **Wide screens:** at 800 points or wider, the page switches to two columns.

All work is in `LoopMeet.App`. The rules live in two small MAUI-free static classes, so they can be unit-tested (research D1).

## Technical Context

**Language/Version**: C# 13 / .NET 10, XAML (MAUI).

**Primary Dependencies**:

- Microsoft.Maui.Controls 10.0.120 and CommunityToolkit.Mvvm 8.4.x, both already in the app.
- No new NuGet packages.

**Storage**: None. No data or contract changes ([data-model.md](data-model.md)).

**Testing**:

- xUnit in `LoopMeet.App.Tests` (`net10.0`), with no mocking framework.
- Unit tests for the MAUI-free `ProfileText` and `ProfileLayout`, linked into the test project like `AppVersionText`.
- Source-inspection tests for `ProfilePage.xaml`, its code-behind and `ProfileViewModel`.
- The [quickstart.md](quickstart.md) device matrix for clipping, text size, rotation and the keyboard.

**Target Platform**: iOS 15+ (iPhone and iPad) and Android (API 24+). Mac Catalyst renders the same page, and needs no specific work.

**Project Type**: Mobile app (MAUI), one feature module (`Features/Profile`).

**Performance Goals**: no measurable change. The page has the same data load and a handful more views. A layout switch only moves existing views.

**Constraints**:

- App only: no Api, Core, Infrastructure, Supabase or web changes.
- The App must not reference `LoopMeet.Core` (so no `AccountDeletionPolicy`).
- Clarified UI text is fixed: the Save description, the delete subtitle and the notification texts.
- Delete account text contrast must be at least 4.5:1.

**Scale/Scope**:

- One page rewritten: XAML and code-behind.
- One ViewModel extended.
- Two new static helper files and one enum moved to its own file.
- Six colours and one SVG icon.
- Four new test classes, two test classes updated, and `LoopMeet.App.Tests.csproj` links.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
| --- | --- | --- |
| I. Code Quality | Pass | The page is rebuilt rather than patched. No commented-out XAML: the reference file stays in `design/`, out of the build. Page-local styles keep the markup readable (D9). The two-column switch is the one added complexity, recorded below. |
| II. Tests Required | Pass | The defect (clipped actions) gets a regression test: `ProfilePage.xaml` has no `HorizontalStackLayout`. New rules get unit tests: the Save rule, the chips including "1 group", the three notification states, and the 800-point boundary. Wiring and fixed texts get source-inspection tests: each action's command, the Save description, the delete subtitle with no digits, the resume hook, and every `StaticResource` key existing. The device-only behaviour is in the quickstart matrix, the accepted coverage for native layout. All of it runs in CI under `app_tests`. |
| III. User Experience | Pass | Five prioritised stories with acceptance scenarios. Error messages are unchanged and already actionable ("Unable to save profile changes."). The edit survives a failed save and a rotation. Save has a screen-reader description. Text grows with the system size. |
| IV. Simplicity | Pass, one justified exception | No new service, interface or DI registration. `ProfileText` and `ProfileLayout` are pure static functions in the established `AppVersionText` pattern (D1), not abstractions. Styles stay page-local until a second user exists (D9). The two-column layout is the exception: see Complexity Tracking. |
| V. Modularity | Pass | Everything stays in `Features/Profile`, plus one colour block and one icon. The only cross-module change moves `NotificationPermissionState` into its own file, with no behaviour change (D2). No new dependencies, and no reference to `LoopMeet.Core`. |
| VI. Contract-First | Pass | No Api or data contract changes. The page's display contract (texts, rules and the layout breakpoint) is defined in [data-model.md](data-model.md) before implementation. |
| VII. Observability | Pass | Logging on save, load, avatar upload and the settings failure is unchanged. Sign-out logging stays in `SessionCoordinator` (D11). |
| Privacy and retention | Pass | No new data is collected, stored or shown. The email and name were already on this page. |

**Post-design re-check:** still Pass, with the one exception below. The design added no project, package, service or stored data.

## Project Structure

### Documentation (this feature)

```text
specs/022-profile-redesign/
├── design/              # input: README, captures, mockups, reference XAML, prompts
├── spec.md
├── plan.md
├── research.md
├── data-model.md        # no data or contract changes; display state only
├── quickstart.md        # device matrix
├── checklists/
│   └── requirements.md
└── tasks.md             # /speckit.tasks
```

There is no `contracts/` directory: no interface changes (data-model.md).

### Source Code (repository root)

```text
src/LoopMeet.App/
├── Features/Profile/
│   ├── ProfileText.cs                       # new, MAUI-free: chips, notification text, Save rule, delete subtitle
│   ├── ProfileLayout.cs                     # new, MAUI-free: 800-point rule, 600/1100 widths
│   ├── ViewModels/ProfileViewModel.cs       # + SavedDisplayName, CanSaveProfile, MemberSinceText, GroupCountText,
│   │                                        #   NotificationStatusText, RefreshNotificationStatus(); injects NotificationPermissionService
│   └── Views/
│       ├── ProfilePage.xaml                 # rewritten: header row, header card, Account card, session card, footer
│       └── ProfilePage.xaml.cs              # + OnSizeAllocated layout switch, Window.Resumed refresh
├── Services/Notifications/
│   ├── NotificationPermissionState.cs       # enum moved out of the service file (D2)
│   └── NotificationPermissionService.cs     # enum removed from this file, otherwise unchanged
└── Resources/
    ├── Styles/Colors.xaml                   # + DestructiveTerracotta(/Dark), AmberTint, AmberTintText, GuavaTint, TerracottaTint
    └── Images/ic_camera.svg                 # new (Bootstrap Icons bi-camera, MIT)

tests/LoopMeet.App.Tests/
├── LoopMeet.App.Tests.csproj                # + links: ProfileText.cs, ProfileLayout.cs, NotificationPermissionState.cs
├── Features/Profile/
│   ├── ProfileTextTests.cs                  # new: unit
│   ├── ProfileLayoutTests.cs                # new: unit
│   ├── ProfilePageLayoutTests.cs            # new: source inspection
│   ├── ProfileColorContrastTests.cs         # new: unit, reads Colors.xaml (FR-017)
│   └── ProfileViewModelTests.cs             # two tests renamed (D10)
└── Features/RefreshSurfaceTests.cs          # version-line ordering anchor updated (D10)

CLAUDE.md                                    # Recent Changes line, edited by hand (update-agent-context.sh damages it)
```

**Structure Decision**: the existing MAUI feature-module layout. The helpers sit next to `AppVersionText.cs` in `Features/Profile/`, and the tests mirror them in `tests/LoopMeet.App.Tests/Features/Profile/`. No `TestDoubles` are needed, because the tested code is pure. The ViewModel itself can't be constructed off-device (D1).

**CI scope**: `src/LoopMeet.App/*` selects `app_tests build_app`, and `tests/LoopMeet.App.Tests/*` selects `app_tests`. `specs/*` and `CLAUDE.md` select nothing. No `ci-scope.sh` change is needed.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
| --- | --- | --- |
| A width-based two-column layout (`ProfileLayout` plus an `OnSizeAllocated` switch in the code-behind) | Clarification (c): the user chose two columns at 800 points or wider, as in `design/mockups/ios_landscape.png`, so wide screens aren't one narrow strip. | A single centred column (Principle IV's default) was offered and declined. `AdaptiveTrigger` measures the window rather than the page, and its rotation behaviour on Android is unverified here. Two duplicated layouts would lose the unsaved edit on a switch (research D5). |
