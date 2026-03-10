# Implementation Plan: UI Polish — Icons & Button Placement

**Branch**: `004-ui-polish` | **Date**: 2026-03-09 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/004-ui-polish/spec.md`

## Summary

Move the Logout button from the Groups page to the Profile page (with a logout icon); add a save icon to the Profile Save button; replace the Groups page Create Group text button with a circular floating action button (FAB) at bottom-right; add ✓/🗑 icons to the desktop Accept/Decline buttons on the Pending Invitations page (and add the missing Decline button for desktop); add invitation icons to the Group Detail and Send Invitation page buttons. All new image icons follow the existing SVG + PNG fallback convention used by the tab navigation.

## Technical Context

**Language/Version**: C# 13 / .NET 10
**Primary Dependencies**: Microsoft.Maui.Controls 10.0.30, CommunityToolkit.Maui 14.0.0, CommunityToolkit.Mvvm 8.4.0
**Storage**: N/A (UI-only changes; no data persistence involved)
**Testing**: xUnit 2.9.3 (`LoopMeet.App.Tests`, `LoopMeet.Api.Tests`, `LoopMeet.Core.Tests`)
**Target Platform**: iOS (net10.0-ios), Android (net10.0-android), macOS/Mac Catalyst (net10.0-maccatalyst), Windows (net10.0-windows10.0.19041.0)
**Project Type**: Cross-platform mobile/desktop app (MAUI)
**Performance Goals**: No performance impact expected; icon assets are small (< 5 KB each); FAB adds no measurable overhead
**Constraints**: All icons must render on all four platforms in light and dark mode; no third-party icon library may be added; desktop-only buttons must not appear on phone/tablet
**Scale/Scope**: 5 XAML views modified, 2 ViewModels modified, 3 new icon asset pairs added

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
| --------- | ------ | ----- |
| I — Code Quality | ✅ PASS | Changes are localised to XAML and two VMs. No dead code introduced. `LogoutCommand` is moved, not duplicated. |
| II — Tests Required | ✅ PASS | `LogoutCommand` move requires a unit test in `LoopMeet.App.Tests`. Manual acceptance scenarios from spec cover UI verification. |
| III — User Experience First | ✅ PASS | All five user stories have defined acceptance scenarios. UX improvements are the primary goal of this feature. |
| IV — Simplicity Over Cleverness | ✅ PASS | Native `Button.ImageSource` for icons; Grid overlay for FAB; inline Unicode for Accept/Decline icons. No unnecessary abstraction. |
| V — Modularity | ✅ PASS | Changes are confined to their respective Feature modules (Groups, Profile, Invitations). No cross-module dependencies introduced. |
| VI — Contract-First | ✅ PASS | No public API or cross-module contracts change. Internal ViewModel command move is a refactor within the Profile feature module. |
| VII — Observability | ✅ PASS | Logout action already has appropriate error handling in the existing implementation; this is preserved in the move. |
| Additional — UX Review | ✅ PASS | Acceptance scenarios and a quickstart manual test checklist are defined. |

**No violations. No Complexity Tracking table required.**

## Project Structure

### Documentation (this feature)

```text
specs/004-ui-polish/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks — not created here)
```

### Source Code (affected files)

```text
src/LoopMeet.App/
├── Resources/
│   └── Images/
│       ├── ic_logout.svg                  # NEW — logout icon vector
│       ├── ic_logout_fallback.png         # NEW — logout icon PNG fallback
│       ├── ic_save.svg                    # NEW — save icon vector
│       ├── ic_save_fallback.png           # NEW — save icon PNG fallback
│       ├── ic_invite.svg                  # NEW — invite/send icon vector
│       └── ic_invite_fallback.png         # NEW — invite/send icon PNG fallback
├── Features/
│   ├── Groups/
│   │   ├── Views/
│   │   │   ├── GroupsListPage.xaml        # MODIFIED — remove logout btn, FAB for create group
│   │   │   └── GroupDetailPage.xaml       # MODIFIED — add ic_invite to Invite Member btn
│   │   └── ViewModels/
│   │       └── GroupsListViewModel.cs     # MODIFIED — remove LogoutCommand
│   ├── Profile/
│   │   ├── Views/
│   │   │   └── ProfilePage.xaml           # MODIFIED — add logout btn, add save icon
│   │   └── ViewModels/
│   │       └── ProfileViewModel.cs        # MODIFIED — add LogoutCommand
│   └── Invitations/
│       └── Views/
│           ├── PendingInvitationsPage.xaml # MODIFIED — icon on Accept, add Decline btn
│           └── InviteMemberPage.xaml       # MODIFIED — add ic_invite to Send btn

tests/LoopMeet.App.Tests/
└── Features/
    └── Profile/
        └── ProfileViewModelTests.cs       # NEW or MODIFIED — test LogoutCommand
```

**Structure Decision**: Single-project MAUI app. All changes are within the existing `src/LoopMeet.App` project. No new projects, services, or infrastructure are added.

---

## Phase 0: Research

See [research.md](research.md) for full findings. Summary of decisions:

| Question | Decision |
| -------- | -------- |
| Icon delivery method | SVG + PNG fallback pairs in `Resources/Images/`, matching tab icon convention |
| Button icon implementation | Native `Button.ImageSource` + `ContentLayout="Left,8"` on existing `<Button>` elements |
| Accept/Decline desktop icons | Inline Unicode `✓` / `🗑` prepended to button Text — same characters as swipe actions |
| FAB implementation | `Grid` overlay with circular `<Button>` (56×56, CornerRadius=28) anchored bottom-right |
| Logout move | `LogoutCommand` moved from `GroupsListViewModel` → `ProfileViewModel`; no new service registration needed |
| Testing | Unit test for moved `LogoutCommand`; manual acceptance testing for UI on all platforms |

---

## Phase 1: Design

### Story 1 — Logout Moves to Profile Page

**Scope**: `GroupsListViewModel`, `GroupsListPage.xaml`, `ProfileViewModel`, `ProfilePage.xaml`, new `ic_logout` asset.

**Design**:

- `ProfileViewModel.LogoutAsync()`: identical logic to the removed `GroupsListViewModel.LogoutAsync()` — call `_authService.SignOutAsync()`, then `Shell.Current.GoToAsync("//login")`.
- `ProfileViewModel.LogoutCommand`: `[RelayCommand]` on `LogoutAsync()`, guarded by `IsBusy`.
- `ProfilePage.xaml`: Add `<Button>` for Logout after the Change Password button (end of the action button row). Use `ImageSource="ic_logout_fallback.png"` and `ContentLayout="Left,8"`. Style: secondary/danger visual — distinct from the primary Save button. Consider `BackgroundColor="Transparent"` with `TextColor` in the app's secondary/destructive colour, or a separate style.
- `GroupsListPage.xaml`: Remove the Logout `<Button>` and collapse the two-column header `<Grid>` back to a simple `<Label>` (no grid needed once logout is gone).
- `GroupsListViewModel.cs`: Remove `LogoutCommand` and `LogoutAsync()`. Remove `IAuthService` from constructor only if it's no longer used by any other command in this VM (verify before removing).

**Acceptance**: FR-001, FR-002, FR-003 — verified by Story 1 acceptance scenarios.

---

### Story 2 — Floating "+" FAB on Groups Page

**Scope**: `GroupsListPage.xaml` only (layout restructure).

**Design**:

- Wrap the page's existing root `VerticalStackLayout` + `ScrollView` in a `<Grid>` (single row, single column). The existing content occupies the full grid cell.
- Add bottom padding to the `CollectionView` (or `ScrollView`) — `Margin="0,0,0,80"` — so the last list item is never obscured by the FAB.
- Add the FAB as the last child of the `<Grid>` (renders on top):

  ```xml
  <Button
      VerticalOptions="End"
      HorizontalOptions="End"
      Margin="0,0,16,32"
      WidthRequest="56"
      HeightRequest="56"
      CornerRadius="28"
      Text="+"
      FontSize="28"
      FontAttributes="Bold"
      TextColor="White"
      BackgroundColor="{AppThemeBinding Light={StaticResource Primary}, Dark={StaticResource PrimaryDark}}"
      Command="{Binding CreateGroupCommand}" />
  ```

- Remove the old `<Button Text="Create Group" .../>` from the page body.

**Acceptance**: FR-005, FR-006, FR-007, FR-008 — verified by Story 2 acceptance scenarios.

---

### Story 3 — Save Icon on Profile Page

**Scope**: `ProfilePage.xaml` only.

**Design**:

- Locate the existing `<Button Text="Save" Command="{Binding SaveProfileCommand}" />`.
- Add `ImageSource="ic_save_fallback.png"` and `ContentLayout="Left,8"`.
- No ViewModel change required.

**Acceptance**: FR-004 — verified by Story 3 acceptance scenarios.

---

### Story 4 — Desktop Accept & Decline on Pending Invitations

**Scope**: `PendingInvitationsPage.xaml` only.

**Design**:

*Existing desktop Accept button* — update `Text`:

```xml
<Button
    Text="✓ Accept"
    IsVisible="{OnIdiom Phone=False, Tablet=False, Desktop=True, TV=True}"
    Command="{Binding BindingContext.AcceptInvitationCommand, Source={x:Reference PendingInvitationsPageRoot}}"
    CommandParameter="{Binding .}" />
```

*New desktop Decline button* — add alongside Accept. The two buttons need a container (`HorizontalStackLayout` or second `Grid` column). Update the desktop button container to be a `HorizontalStackLayout` or a two-column `Grid`:

```xml
<HorizontalStackLayout
    Grid.Column="1"
    Spacing="8"
    IsVisible="{OnIdiom Phone=False, Tablet=False, Desktop=True, TV=True}">
    <Button
        Text="✓ Accept"
        Command="{Binding BindingContext.AcceptInvitationCommand, Source={x:Reference PendingInvitationsPageRoot}}"
        CommandParameter="{Binding .}" />
    <Button
        Text="🗑 Decline"
        BackgroundColor="#DC2626"
        Command="{Binding BindingContext.DeclineInvitationCommand, Source={x:Reference PendingInvitationsPageRoot}}"
        CommandParameter="{Binding .}" />
</HorizontalStackLayout>
```

Note: Move the `IsVisible` binding to the wrapping `HorizontalStackLayout` so both buttons share the same idiom condition.

**Acceptance**: FR-009, FR-010, FR-011, FR-012 — verified by Story 4 acceptance scenarios.

---

### Story 5 — Invitation Icons on Group Detail & Send Invitation

**Scope**: `GroupDetailPage.xaml`, `InviteMemberPage.xaml`, new `ic_invite` asset.

**Design**:

- `GroupDetailPage.xaml`: Locate the "Invite Member" `<Button>`. Add `ImageSource="ic_invite_fallback.png"` and `ContentLayout="Left,8"`.
- `InviteMemberPage.xaml`: Locate the "Send Invite" `<Button>`. Add `ImageSource="ic_invite_fallback.png"` and `ContentLayout="Left,8"`.
- Same `ic_invite` asset is reused for both pages (both represent the "send an invitation" action).

**Acceptance**: FR-013, FR-014 — verified by Story 5 acceptance scenarios.

---

### Cross-Cutting: Icon Assets

All three new icon pairs must be sourced and added to `Resources/Images/` before or alongside the XAML changes that reference them. Recommended sources (MIT-licensed):

- **Heroicons** (heroicons.com): `arrow-right-on-rectangle` (logout), `cloud-arrow-up` or `archive-box-arrow-down` (save), `envelope` or `user-plus` (invite)
- Export SVG at 24×24 viewBox; export PNG fallback at 32×32px.
- File naming: `ic_logout.svg`, `ic_logout_fallback.png`, etc.

MAUI automatically includes files in `Resources/Images/` as `MauiImage` items (no `.csproj` edit required for MAUI 10).

---

### Testing Plan

| Test | Type | File | Verifies |
| ---- | ---- | ---- | -------- |
| `LogoutCommand_SignsOutAndNavigates` | Unit | `LoopMeet.App.Tests/.../ProfileViewModelTests.cs` | FR-002, FR-003 |
| `LogoutCommand_NotOnGroupsViewModel` | Unit | `LoopMeet.App.Tests/.../GroupsListViewModelTests.cs` | FR-001 |
| Manual: Profile page Logout button visible + functional | Acceptance | — | FR-002, FR-003, SC-001 |
| Manual: Groups page no Logout button | Acceptance | — | FR-001, SC-001 |
| Manual: FAB visible and fixed at bottom-right while scrolling | Acceptance | — | FR-006, FR-007, SC-002 |
| Manual: FAB tap opens Create Group | Acceptance | — | FR-008 |
| Manual: Save button icon visible on Profile | Acceptance | — | FR-004 |
| Manual: Desktop Accept has ✓ icon; Decline button with 🗑 present | Acceptance | — | FR-009, FR-010, SC-003 |
| Manual: Mobile/tablet shows no Accept/Decline buttons | Acceptance | — | FR-012 |
| Manual: Group Detail invite icon present | Acceptance | — | FR-013 |
| Manual: Send Invite icon present | Acceptance | — | FR-014 |
| Manual: All icons render in light + dark mode on all 4 platforms | Acceptance | — | FR-015, FR-016, SC-004 |
