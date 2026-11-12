# Implementation Plan: Pull to Refresh on Home and Groups, and App Version on Profile

**Branch**: `018-home-refresh-version` | **Date**: 2026-11-04 | **Spec**: [spec.md](./spec.md)

## Summary

This is a client-only change to the MAUI app.
- **Home and Groups:** both screens get the pull to refresh that group detail already has: a `RefreshView` around the page's `ScrollView`, bound to an `IsRefreshing` flag and a `RefreshCommand`, that clears the flag in a `finally`.
- **Profile:** the screen ends with `Version {VersionString} ({BuildString})`, read from `AppInfo` at runtime.

There are no Api, database or edge-function changes, and this light feature needs no research, data model or contracts.

## Technical Context

- **Stack**: C# 13 / .NET 10, the MAUI client only. Microsoft.Maui.Controls 10.0.70 and CommunityToolkit.Mvvm 8.4.2. No new packages.
- **Testing**: xUnit, with no mocking framework.
  - The version formatter is MAUI-free. It's compiled into `LoopMeet.App.Tests` through a linked file and unit tested.
  - The XAML and view-model wiring is checked by source inspection, following the project convention.
  - A quickstart device check on Android covers the native gesture and spinner. iOS waits for 013.
- **Platforms**: Android now and iOS later (013). `AppInfo.Current.VersionString` and `BuildString` map to `ApplicationDisplayVersion` and `ApplicationVersion`: on Android, the version name and version code; on iOS, `CFBundleShortVersionString` and `CFBundleVersion`.

## Design

### Refresh (Home, Groups)

Both screens follow the group detail pattern (`GroupDetailViewModel.RefreshMeetupsAsync`):

```xml
<RefreshView Command="{Binding RefreshCommand}" IsRefreshing="{Binding IsRefreshing}">
    <ScrollView> …existing content… </ScrollView>
</RefreshView>
```

```csharp
[RelayCommand]
private async Task RefreshAsync()
{
    try { await LoadOnceAsync(); }
    finally { IsRefreshing = false; }
}
```

**Single-flight load:** each view model keeps the load task it has in progress and returns that task to anyone who asks while it's still running (`LoadOnceAsync`).
- `OnAppearing`'s `LoadCommand` and a pull to refresh therefore never start a second load. A pull that arrives mid-load waits for that load, so the spinner stops when the data is actually there, as the spec's edge case requires.
- Today Home has no guard at all, so two overlapping loads can both clear the list and add to it, which duplicates cards.
- The Groups list's `IsBusy` early-return would stop the spinner at once while the first load is still running.

**Failure:** no new UI. Both loads already catch their exceptions and fall back to the screen's current failure state: Home shows its empty state, Groups logs and keeps its lists. The `finally` guarantees the spinner stops (FR-003).

**Groups skeletons during refresh:** `GroupsListViewModel.LoadCoreAsync` turns its skeletons on for every load, so a pull would briefly show skeletons as well as the spinner. That's left alone here. 019 changes skeletons to appear only when there's no data yet, which removes it.

### Version (Profile)

- `Features/Profile/AppVersionText.cs`: a MAUI-free `static string Format(string version, string build)`, returning `Version 0.1.3 (4)`. It's linked into `LoopMeet.App.Tests`.
- `ProfileViewModel.VersionText`: computed once as `AppVersionText.Format(AppInfo.Current.VersionString, AppInfo.Current.BuildString)`.
- `ProfilePage.xaml`: a small, centred, secondary-colour label at the end of the content, after the buttons.

## Constitution Check

*GATE: evaluated against Meetloop Constitution v0.1.0.*

| Gate | Principle | Status | Notes |
| ------ | ----------- | -------- | ------- |
| G1 | I. Code Quality | PASS | The new code reuses the existing group detail pattern. There are no TODOs and no dead code. |
| G2 | II. Tests Required | PASS | The formatter has unit tests. The XAML and view-model wiring has source-inspection tests. The native gesture is in the quickstart. |
| G3 | III. UX First | PASS | It uses the platform's standard refresh gesture and indicator, and the three list screens now behave alike. The version line is secondary text and doesn't compete with the Profile actions. |
| G4 | IV. Simplicity | PASS | There's no shared refresh base class: two view models with a few lines each don't justify one. The single-flight load is a field and one method. |
| G5 | V. Modularity | PASS | The changes stay inside `Features/Home`, `Features/Groups` and `Features/Profile`. |
| G6 | VI. Contract-First | N/A | No new interfaces or endpoints. |
| G7 | VII. Observability | PASS | The existing load logging covers refresh failures, because refresh calls the same load. |
| G8 | Privacy & retention constraints | N/A | No user data is stored or shown beyond what's already on screen. The version isn't personal data. |
| G9 | Quality gates | PASS | The CI path map runs App tests and the Android compile for `src/LoopMeet.App` changes. |

## Project Structure

```text
src/LoopMeet.App/
├── Features/Home/ViewModels/HomeViewModel.cs          # IsRefreshing, RefreshCommand, single-flight load
├── Features/Home/Views/HomePage.xaml                  # RefreshView
├── Features/Groups/ViewModels/GroupsListViewModel.cs  # IsRefreshing, RefreshCommand, single-flight load
├── Features/Groups/Views/GroupsListPage.xaml          # RefreshView
├── Features/Profile/AppVersionText.cs                 # new, MAUI-free
├── Features/Profile/ViewModels/ProfileViewModel.cs    # VersionText
└── Features/Profile/Views/ProfilePage.xaml            # version label
tests/LoopMeet.App.Tests/
├── Features/Profile/AppVersionTextTests.cs            # new
├── Features/RefreshSurfaceTests.cs                    # new, source inspection
└── LoopMeet.App.Tests.csproj                          # links AppVersionText.cs
specs/018-home-refresh-version/quickstart.md           # Android device checks
```
