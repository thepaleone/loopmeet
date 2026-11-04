# Tasks: Pull to Refresh on Home and Groups, and App Version on Profile

**Input**: [spec.md](./spec.md), [plan.md](./plan.md)
**Tests**: Required (Constitution II). Each story writes its tests first and sees them fail.

## Format: `[ID] [P?] [Story] Description`

## Phase 1: User Story 1, refresh Home (P1)

- [X] T001 [US1] In `tests/LoopMeet.App.Tests/Features/RefreshSurfaceTests.cs`, add source-inspection tests for Home:
  - `HomePage.xaml` wraps its `ScrollView` in a `RefreshView` bound to `RefreshCommand` and `IsRefreshing`.
  - `HomeViewModel.RefreshAsync` clears `IsRefreshing` in a `finally`.
  - Both `LoadAsync` and `RefreshAsync` go through `LoadOnceAsync`.
- [X] T002 [US1] Implement T001 in `src/LoopMeet.App/Features/Home/ViewModels/HomeViewModel.cs` and `Views/HomePage.xaml`:
  - Add `IsRefreshing` and `RefreshCommand`.
  - Add a single-flight `LoadOnceAsync`, with the current load body moved into `LoadCoreAsync`.

## Phase 2: User Story 2, refresh the Groups list (P2)

- [X] T003 [US2] Add the same tests for `GroupsListPage.xaml` and `GroupsListViewModel` to `RefreshSurfaceTests.cs`.
- [X] T004 [US2] Implement T003 in `src/LoopMeet.App/Features/Groups/ViewModels/GroupsListViewModel.cs` and `Views/GroupsListPage.xaml`. Replace `LoadAsync`'s `IsBusy` early-return with `LoadOnceAsync`, so a pull mid-load waits for that load.

## Phase 3: User Story 3, version on Profile (P3)

- [X] T005 [P] [US3] Create `tests/LoopMeet.App.Tests/Features/Profile/AppVersionTextTests.cs` with two cases:
  - `Format("0.1.3", "4")` returns `Version 0.1.3 (4)`.
  - Surrounding whitespace is trimmed.

  Link `src/LoopMeet.App/Features/Profile/AppVersionText.cs` in the test csproj.
- [X] T006 [US3] Add source-inspection tests:
  - `ProfileViewModel` builds `VersionText` from `AppInfo.Current.VersionString` and `AppInfo.Current.BuildString` through `AppVersionText.Format`.
  - `ProfilePage.xaml` binds a label to `VersionText` after its last button.
- [X] T007 [US3] Implement T005 and T006: `AppVersionText.cs`, `ProfileViewModel.VersionText`, and the label in `ProfilePage.xaml`.

## Phase 4: Polish

- [X] T008 Write `specs/018-home-refresh-version/quickstart.md` with Android device checks:
  - pull on Home and Groups (with a change made from another account)
  - airplane-mode refresh, where the spinner stops
  - a pull straight after opening the screen
  - the version line on a `-b` build
- [ ] T009 Run the full `dotnet test` and the Android build. Run quickstart.md on a device and record the results.

## Decisions

- [X] T010 [US2] Decide how a failed refresh behaves on the Groups list.
  - **Before:** a network error or timeout during any Groups load showed "Service Unavailable … Quit" and exited the app (`ShowApiUnavailableAndQuitAsync`). An offline pull to refresh would have closed the app.
  - **Decision (2026-11-04): never quit (FR-009).**
    - A failed load, first load or refresh, keeps the lists on screen and shows "Couldn't load your groups. Check your connection and try again." with OK.
    - The quit helper and its Mac Catalyst `using`s are removed.
    - Regression test: `RefreshSurfaceTests.GroupsList_NeverQuitsTheAppWhenALoadFails`.

## Notes

- `GroupsListViewModel.IsBusy` was removed. It only guarded `LoadAsync`, which `LoadOnceAsync` now replaces, and nothing bound to it.
- Home's `LoadAsync` had no guard, so `OnAppearing` and a pull could overlap and both clear and refill `UpcomingMeetups`, duplicating cards. The single-flight load fixes that too.

## Dependencies

- **Stories:** US1, US2 and US3 are independent.
- **Within a story:** the tests come before the implementation.
- **Files:** T001 and T003 both edit `RefreshSurfaceTests.cs`, so run them in order.
