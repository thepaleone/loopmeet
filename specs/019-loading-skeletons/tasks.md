# Tasks: Consistent Loading Skeletons

**Input**: [spec.md](./spec.md), [plan.md](./plan.md)
**Tests**: Required (Constitution II). The tests come before the implementation, and are seen to fail first.

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Foundational, the rule and the component

- [X] T001 [P] Create `tests/LoopMeet.App.Tests/Controls/LoadingPlaceholdersTests.cs`, covering every combination of completed load and item count:
  - It shows only when there has been no completed load and there are no items.
  - Items keep it hidden, even before a load completes.
  - A completed load hides it, even when it found no items.

  Link `src/LoopMeet.App/Controls/LoadingPlaceholders.cs` in the test csproj.
- [X] T002 Create `src/LoopMeet.App/Controls/LoadingPlaceholders.cs`.
- [X] T003 Create `src/LoopMeet.App/Controls/SkeletonList.cs`:
  - `SkeletonVariant`, with the `HomeMeetupCard`, `GroupMeetupCard`, `MemberTile` and `GroupCard` variants.
  - `SkeletonList`, with the `Variant`, `Count` and `IsActive` bindable properties.
  - Themed colours, and a shimmer that runs only while it's active and loaded.

## Phase 2: User Story 1, Home (P1)

- [X] T004 [US1] In `tests/LoopMeet.App.Tests/Features/SkeletonSurfaceTests.cs`, add a source-inspection test for Home:
  - `HomePage.xaml` has `<controls:SkeletonList Variant="HomeMeetupCard" Count="3" IsActive="{Binding ShowMeetupsSkeleton}"`.
  - `HomeViewModel.ShowMeetupsSkeleton` uses `LoadingPlaceholders.ShouldShow`.
  - The load sets `_hasLoadedOnce` in a `finally`.
- [X] T005 [US1] Implement T004 in `HomeViewModel.cs` and `HomePage.xaml`.

## Phase 3: User Story 2, group detail (P2)

- [X] T006 [US2] Add source-inspection tests for group detail:
  - member tiles (`MemberTile`, 4, `ShowMembersSkeleton`)
  - upcoming (`GroupMeetupCard`, 2, `ShowUpcomingSkeleton`) and past (`GroupMeetupCard`, 2, `ShowPastSkeleton`)
  - each property goes through `LoadingPlaceholders.ShouldShow`
  - `_membersLoaded` is set in a `finally` around `GetGroupAsync`
- [X] T007 [US2] Implement T006 in `GroupDetailViewModel.cs` and `GroupDetailPage.xaml`. Raise the skeleton properties' change notifications whenever the loaded flags, the list mode or the lists change.

## Phase 4: User Story 3, the Groups list (P3)

- [X] T008 [US3] Add source-inspection tests for the Groups list:
  - `GroupsListPage.xaml` uses `SkeletonList` `GroupCard` for owned (3) and member (2) groups.
  - It no longer contains `Shimmer` names, `#E2E8F0`, `IsLoadingOwnedGroups` or `IsLoadingMemberGroups`.
  - `GroupsListPage.xaml.cs` no longer has `AnimateShimmer`.
  - The view model uses `LoadingPlaceholders.ShouldShow`.
- [X] T009 [US3] Implement T008:
  - Replace the five skeleton blocks and the `DataTrigger`s.
  - Remove the shimmer code-behind.
  - Replace `IsLoading*` with `_hasLoadedOnce`.

## Phase 5: Polish

- [ ] T010 Write `specs/019-loading-skeletons/quickstart.md`. Run its checks on an Android device over a slow connection:
  - a cold launch
  - opening a group, and switching to Past
  - the Groups tab
  - pulls to refresh, with no placeholders over real cards
  - a failed first load, where placeholders end and the empty state shows
  - light and dark themes
- [X] T011 Run the full `dotnet test`, the Android build and the iOS compile.

## Notes

- **Group detail's past list:** a failed first page leaves `_pastLoaded` false and sets `HasLoadMoreError`. `ShowPastSkeleton` therefore also requires `!HasLoadMoreError`, so the existing retry row replaces the placeholders instead of them staying on screen.
- **Members load:** `GetGroupAsync` sits inside a `try`/`finally` that sets `_membersLoaded`. A failed group load still ends the member placeholders, and the exception still propagates as before.
- **Groups list:** the `DataTrigger`s that hid both real lists during every load are gone. That's what made placeholders flash on refresh in 018.
- **Colours:** placeholder colours now come from the palette (`Gray200` light, `Gray600` dark; shimmer `White` / `Gray500`) instead of the hard-coded cool greys `#E2E8F0` and `#F8FAFC`.
- **Shimmer per shape:** the first version laid one band over each whole card, so it swept as a block the card's full height (seen on the simulator). Each bar and avatar circle now clips its own band, as the old Groups list name bar did, and a source-inspection check keeps it that way.
- **Checks:** `dotnet test` App 197 (22 new). The Android and iOS-simulator builds succeed with no new warnings.

## Dependencies

- **Phase 1** comes before every story.
- **Stories:** US1, US2 and US3 are independent after Phase 1.
- **Files:** T004, T006 and T008 all edit `SkeletonSurfaceTests.cs`, so run them in order.
