# Data Model: UI Polish — Icons & Button Placement

**Branch**: `004-ui-polish` | **Date**: 2026-03-09

## Overview

This feature involves **no new data entities and no schema changes**. All changes are confined to the presentation layer (XAML views, ViewModels, and image assets). The existing data models — `UserProfile`, `GroupSummary`, `InvitationSummary` — remain unchanged.

## State Changes in ViewModels

Although no new persisted data is introduced, two ViewModels gain or lose state:

### GroupsListViewModel — Removed Behaviour

| Property / Command | Change | Reason |
|-------------------|--------|--------|
| `LogoutCommand` | **Removed** | Moved to `ProfileViewModel` |
| `LogoutAsync()` | **Removed** | Moved to `ProfileViewModel` |

No new properties are added. The `IAuthService` dependency that powered logout remains injected (used by other commands).

### ProfileViewModel — Added Behaviour

| Property / Command | Type | Description |
|-------------------|------|-------------|
| `LogoutCommand` | `AsyncRelayCommand` | Signs the user out and navigates to the login screen |
| `LogoutAsync()` | `private async Task` | Implementation: calls `_authService.SignOutAsync()`, clears any cached session state, then `Shell.Current.GoToAsync("//login")` |

`IAuthService` is already available in `ProfileViewModel`'s constructor (Supabase auth is used for password change). No new dependency injection registration is needed.

## Image Assets

New files added to `src/LoopMeet.App/Resources/Images/`:

| Asset | Format | Used In |
|-------|--------|---------|
| `ic_logout.svg` | SVG vector | Profile page Logout button `ImageSource` |
| `ic_logout_fallback.png` | PNG (runtime fallback) | Profile page Logout button on platforms without SVG |
| `ic_save.svg` | SVG vector | Profile page Save button `ImageSource` |
| `ic_save_fallback.png` | PNG (runtime fallback) | Profile page Save button |
| `ic_invite.svg` | SVG vector | Group Detail invite button & Send Invitation button |
| `ic_invite_fallback.png` | PNG (runtime fallback) | Group Detail & Send Invitation buttons |

All assets follow the naming convention established by the tab icons (`tab_xxx.svg` + `tab_xxx_fallback.png`).

## UI Component Inventory

### Modified Views

| View | File | Changes |
|------|------|---------|
| `GroupsListPage` | `Features/Groups/Views/GroupsListPage.xaml` | Remove Logout button from header; replace Create Group text button with circular FAB overlay |
| `ProfilePage` | `Features/Profile/Views/ProfilePage.xaml` | Add Logout button (with `ic_logout` icon); add `ImageSource` to Save button (`ic_save` icon) |
| `PendingInvitationsPage` | `Features/Invitations/Views/PendingInvitationsPage.xaml` | Add `✓` icon to desktop Accept button text; add desktop-only Decline button with `🗑` icon |
| `GroupDetailPage` | `Features/Groups/Views/GroupDetailPage.xaml` | Add `ic_invite` icon to Invite Member button |
| `InviteMemberPage` | `Features/Invitations/Views/InviteMemberPage.xaml` | Add `ic_invite` icon to Send Invite button |

### Modified ViewModels

| ViewModel | File | Changes |
|-----------|------|---------|
| `GroupsListViewModel` | `Features/Groups/ViewModels/GroupsListViewModel.cs` | Remove `LogoutCommand` and `LogoutAsync()` |
| `ProfileViewModel` | `Features/Profile/ViewModels/ProfileViewModel.cs` | Add `LogoutCommand` and `LogoutAsync()` |

### No Changes Required

| Component | Reason |
|-----------|--------|
| All API services | No data contract changes |
| AppShell.xaml | Routes and tab structure unchanged |
| MauiProgram.cs | No new service registrations |
| All other pages | Not in scope |
| All test infrastructure | Existing test helpers remain valid |
