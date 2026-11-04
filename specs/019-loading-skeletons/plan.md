# Implementation Plan: Consistent Loading Skeletons

**Branch**: `019-loading-skeletons` | **Date**: 2026-11-04 | **Spec**: [spec.md](./spec.md)

## Summary

The MAUI app gets one reusable placeholder component, `SkeletonList`, which shows a fixed number of placeholder cards of one variant with a shared shimmer.
- Home, group detail and the Groups list show it only while a section has no data yet.
- The Groups list's five hand-written skeleton copies, and the shimmer code in its code-behind, are replaced by this component.

This is client-only. There are no Api, database or edge-function changes.

## Technical Context

- **Stack**: C# 13 / .NET 10, the MAUI client only. Microsoft.Maui.Controls 10.0.70, CommunityToolkit.Mvvm 8.4.2. No new packages.
- **Testing**:
  - **Unit:** the display rule (`LoadingPlaceholders.ShouldShow`) is MAUI-free, linked into `LoopMeet.App.Tests` and unit tested.
  - **Source inspection:** checks that each screen uses `SkeletonList` with the right variant and binding, and that the Groups list's old copies are gone.
  - **Device:** a quickstart check on Android over a slow connection covers what can only be seen on screen.

## Design

### Display rule

```csharp
// MAUI-free, unit tested.
public static class LoadingPlaceholders
{
    public static bool ShouldShow(bool hasCompletedLoad, int itemCount) => !hasCompletedLoad && itemCount == 0;
}
```

- **Placeholders show only when there's nothing to display.** If a section already has items, a reload never replaces them with placeholders, whatever the load flag says. That's FR-005.
- **A completed load ends them,** whether it found items, found nothing or failed. That's FR-006, and the existing empty state then shows.

Each view model exposes one `Show…Skeleton` property per section and raises its change notification when the flag or the count changes:

| Screen | Property | "Completed load" flag | Items |
| --- | --- | --- | --- |
| Home | `ShowMeetupsSkeleton` | `_hasLoadedOnce`, set in the load's `finally` | `UpcomingMeetups` |
| Groups list | `ShowOwnedGroupsSkeleton`, `ShowMemberGroupsSkeleton` | `_hasLoadedOnce` (one call loads both) | `OwnedGroups`, `MemberGroups` |
| Group detail | `ShowMembersSkeleton` | `_membersLoaded`, set after `GetGroupAsync` succeeds or fails | `Members` |
| Group detail | `ShowUpcomingSkeleton` | `IsUpcomingSelected && !_upcomingLoaded` | `Meetups` |
| Group detail | `ShowPastSkeleton` | `IsPastSelected && !_pastLoaded` | `PastMeetups` |

**Group detail's flags:** `_upcomingLoaded` and `_pastLoaded` mean "fresh for the list being shown", and loading one list resets the other's flag. Combining them with the item count means switching back to a list that still has cards shows the cards while it refreshes. A switch to a list that has nothing yet shows placeholders. `ResetListMode`, used when a different group opens, already clears the past list. `_membersLoaded` is per view model, and each open builds a new one.

**Groups list:** `IsLoadingOwnedGroups` and `IsLoadingMemberGroups` are removed. So are the `DataTrigger`s that hid both real lists during every load, which is what made placeholders flash on refresh.

### `SkeletonList` (`src/LoopMeet.App/Controls/`)

`SkeletonList` is a `ContentView` built in C#, with three bindable properties:

| Property | Meaning |
| --- | --- |
| `Variant` | `SkeletonVariant.HomeMeetupCard`, `GroupMeetupCard`, `MemberTile` or `GroupCard` |
| `Count` | how many placeholder cards to show |
| `IsActive` | bound to the section's `Show…Skeleton`; also drives `IsVisible` |

**Variants mirror the real cards.** They use the same border styles (`BubbleCardBorderStyle` or `BubbleCardCompactBorderStyle`), padding, margins and corner shapes, with grey bars where the text lines are and circles where the avatars are:
- `HomeMeetupCard`: the regular card with padding 16, and four bars for title, time, location and group.
- `GroupMeetupCard`: the compact card with padding 12,10, and three bars.
- `MemberTile`: the compact tile with padding 10,8: a 40 px circle and a name bar, wrapped in rows like the real members.
- `GroupCard`: the compact card with padding 12,10: a 48 px circle and a name bar.

**Colours** come from the palette and follow the theme: bars are `Gray200` in light mode and `Gray600` in dark mode, replacing the hard-coded `#E2E8F0` and `#F8FAFC`.

**Shimmer:** each card has a translucent band laid over it. The card's border clips it, and it sweeps across with `TranslateToAsync`, staggered between cards, which keeps today's Groups-list style.
- It runs only while `IsActive` is true and the control is loaded. `IsActive` turning false, or `Unloaded`, cancels it (FR-009).
- A screen whose first load is still running when you switch tabs keeps animating until that load ends. That's an accepted, short-lived case.

**Counts** follow the spec's assumptions:

| Screen | Placeholders |
| --- | --- |
| Home | 3 meetup cards |
| Group detail | 4 member tiles; 2 meetup cards per list |
| Groups list | 3 owned and 2 member cards |

## Constitution Check

*GATE: evaluated against Meetloop Constitution v0.1.0.*

| Gate | Principle | Status | Notes |
| ------ | ----------- | -------- | ------- |
| G1 | I. Code Quality | PASS | Five duplicated XAML blocks and the Groups list's named-element shimmer code are removed. There are no TODOs. |
| G2 | II. Tests Required | PASS | The display rule has unit tests. Each screen's wiring has source-inspection tests. The visuals are in the quickstart. |
| G3 | III. UX First | PASS | Every list screen gets the same loading state, and reloads no longer flash placeholders over real cards. |
| G4 | IV. Simplicity | PASS | One component serves 4 variants on 3 screens, which is well above the bar of two real uses. The rule is one line. |
| G5 | V. Modularity | PASS | The new `Controls/` folder holds UI that isn't tied to a feature. Each screen keeps its own rule inputs. |
| G6 | VI. Contract-First | N/A | No new endpoints. The component's bindable properties are its interface, documented above. |
| G7 | VII. Observability | N/A | No new flows. The existing load logging is unchanged. |
| G8 | Privacy & retention constraints | N/A | No data is stored or shown. |
| G9 | Quality gates | PASS | The CI path map runs App tests and the Android compile for `src/LoopMeet.App` changes. |

## Project Structure

```text
src/LoopMeet.App/
├── Controls/LoadingPlaceholders.cs                    # new, MAUI-free rule
├── Controls/SkeletonList.cs                           # new, the component and its variants
├── Features/Home/ViewModels/HomeViewModel.cs          # ShowMeetupsSkeleton
├── Features/Home/Views/HomePage.xaml                  # SkeletonList HomeMeetupCard ×3
├── Features/Groups/ViewModels/GroupsListViewModel.cs  # ShowOwned/MemberGroupsSkeleton; IsLoading* removed
├── Features/Groups/Views/GroupsListPage.xaml(.cs)     # SkeletonList GroupCard; old copies and shimmer removed
├── Features/Groups/ViewModels/GroupDetailViewModel.cs # ShowMembers/Upcoming/PastSkeleton
└── Features/Groups/Views/GroupDetailPage.xaml         # SkeletonList MemberTile ×4, GroupMeetupCard ×2 ×2
tests/LoopMeet.App.Tests/
├── Controls/LoadingPlaceholdersTests.cs               # new
├── Features/SkeletonSurfaceTests.cs                   # new, source inspection
└── LoopMeet.App.Tests.csproj                          # links LoadingPlaceholders.cs
specs/019-loading-skeletons/quickstart.md
```
