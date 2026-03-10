# Quickstart: UI Polish — Icons & Button Placement

**Branch**: `004-ui-polish` | **Date**: 2026-03-09

## What This Feature Does

This feature polishes the app's UI by:
1. Moving the Logout button from the Groups page to the Profile page (with a logout icon)
2. Adding a save icon to the Profile page Save button
3. Converting the Groups page Create Group button into a floating circular "+" action button
4. Adding check/decline icons to the desktop Accept/Decline buttons on the Pending Invitations page (also adding the missing Decline button for desktop)
5. Adding invitation icons to the Group Detail "Invite Member" button and Send Invitation "Send Invite" button

## Prerequisites

- .NET 10 SDK installed
- MAUI workload: `dotnet workload install maui`
- Platform SDK for your target (Xcode for iOS/macOS, Android SDK for Android, Windows for WinUI)
- All existing tests passing: `dotnet test`

## Icon Assets to Create

Before any XAML changes, create and add the three new icon pairs to `src/LoopMeet.App/Resources/Images/`:

| Files to add | Description |
|---|---|
| `ic_logout.svg` + `ic_logout_fallback.png` | Person with arrow-out (logout) |
| `ic_save.svg` + `ic_save_fallback.png` | Floppy disk or cloud-with-tick (save) |
| `ic_invite.svg` + `ic_invite_fallback.png` | Envelope or person-with-plus (invite/send) |

Reference SVG sources: [Heroicons](https://heroicons.com), [Phosphor Icons](https://phosphoricons.com), or [Material Symbols](https://fonts.google.com/icons) — all provide MIT-licensed icons. Export PNG at 32×32px for the fallback.

Declare each in the `.csproj` (already handled automatically by MAUI for files in `Resources/Images/`).

## Implementation Order

Work through these changes in order — each is independently releasable:

### Step 1 — Move Logout to Profile (P1)

1. **ProfileViewModel**: Add `IAuthService` if not already injected, add `LogoutCommand` + `LogoutAsync()` (copy implementation from `GroupsListViewModel.LogoutAsync()`).
2. **ProfilePage.xaml**: Add a `<Button Text="Log out" ImageSource="ic_logout_fallback.png" ContentLayout="Left,8" Command="{Binding LogoutCommand}" />`.
3. **GroupsListViewModel**: Remove `LogoutCommand` and `LogoutAsync()`.
4. **GroupsListPage.xaml**: Remove the Logout button and its containing grid column.

### Step 2 — FAB on Groups Page (P2)

1. Wrap the Groups page root `VerticalStackLayout` / `ScrollView` in a `Grid`.
2. Ensure the `CollectionView`/`ScrollView` has `Margin="0,0,0,80"` or equivalent bottom padding to leave room for the FAB.
3. Add a circular `<Button>` as the last child of the `Grid` with `VerticalOptions="End"`, `HorizontalOptions="End"`, `Margin="0,0,16,32"`, `WidthRequest="56"`, `HeightRequest="56"`, `CornerRadius="28"`, `FontSize="28"`, `FontAttributes="Bold"`, `Text="+"`, `Command="{Binding CreateGroupCommand}"`.
4. Remove the original `<Button Text="Create Group" .../>` text button.

### Step 3 — Save Icon on Profile (P3)

1. **ProfilePage.xaml**: Add `ImageSource="ic_save_fallback.png"` and `ContentLayout="Left,8"` to the existing Save `<Button>`.

### Step 4 — Desktop Accept/Decline Icons on Invitations (P2)

1. **PendingInvitationsPage.xaml** — existing desktop Accept button:
   - Change `Text="Accept"` → `Text="✓ Accept"` (prepend Unicode checkmark inline).
2. Add a new desktop-only Decline `<Button>` next to the Accept button:
   - `Text="🗑 Decline"`
   - `IsVisible="{OnIdiom Phone=False, Tablet=False, Desktop=True, TV=True}"`
   - `Command="{Binding BindingContext.DeclineInvitationCommand, Source={x:Reference ...}}"` (same binding as swipe Decline)
   - `CommandParameter="{Binding .}"`
   - Style: secondary/destructive visual (red background or outlined — align with design).

### Step 5 — Invitation Icons on Group Detail & Send Invitation (P3)

1. **GroupDetailPage.xaml** — Invite Member button: add `ImageSource="ic_invite_fallback.png"` and `ContentLayout="Left,8"`.
2. **InviteMemberPage.xaml** — Send Invite button: add `ImageSource="ic_invite_fallback.png"` and `ContentLayout="Left,8"`.

## Running the App

```bash
# iOS Simulator
dotnet build src/LoopMeet.App -f net10.0-ios && dotnet run --project src/LoopMeet.App -f net10.0-ios

# Android Emulator
dotnet run --project src/LoopMeet.App -f net10.0-android

# macOS (Mac Catalyst)
dotnet run --project src/LoopMeet.App -f net10.0-maccatalyst

# Windows
dotnet run --project src/LoopMeet.App -f net10.0-windows10.0.19041.0
```

## Running Tests

```bash
# All tests
dotnet test

# App tests only
dotnet test tests/LoopMeet.App.Tests/
```

## Acceptance Checklist (Manual)

After implementing each step, verify against the spec acceptance scenarios:

- [ ] Profile page shows Logout button with logout icon; tapping it signs out
- [ ] Groups page has no Logout button
- [ ] Groups page shows circular "+" FAB at bottom-right; FAB stays visible when scrolling
- [ ] Tapping FAB opens Create Group flow
- [ ] Profile page Save button shows save icon
- [ ] Desktop: each invitation row has Accept button with ✓ icon and Decline button with 🗑 icon
- [ ] Mobile/tablet: only swipe actions are shown (no Accept/Decline buttons)
- [ ] Group Detail "Invite Member" button shows invite icon
- [ ] Send Invitation "Send Invite" button shows invite icon
- [ ] All icons render in light mode and dark mode on all four platforms
- [ ] No functional regression on any button action
