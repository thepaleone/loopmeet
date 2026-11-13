# Profile screen redesign: design input

Reference material for the Profile redesign feature. `speckit-prompts.md` holds the prompts that start the feature. Everything here is input to the spec, not a decision: the spec and plan decide.

## Files

| File | What it is |
|---|---|
| `current/ios-profile.png`, `current/android-profile.png` | Today's Profile screen (1320×2868 iOS, 1080×2220 Android). The Android capture shows "Log out" cut off on the right. |
| `mockups/ios_portrait.png` | Proposed layout, iPhone portrait, nothing edited (Save inactive). |
| `mockups/ios_editing.png` | Same, after the display name is edited (Save active). |
| `mockups/android_portrait.png` | Proposed layout, Android portrait. |
| `mockups/ios_landscape.png` | Proposed layout, wide/landscape (optional two-column variant). |
| `mockups/before_after_android.png` | Side-by-side of today vs proposed. |
| `ProfilePage.reference.xaml` | Illustrative XAML for the proposed layout. Not compiled, and not wired to the ViewModel. |

The mockups are HTML renders, so fonts and icons are close to the app's, not exact. In the mockups, Save is in the navigation bar. The reference XAML instead follows the app's established pattern from 011 (a title row with an icon-only save, outside the ScrollView). The spec should pick one; the prompts ask for the 011 pattern.

## The problem

`ProfilePage.xaml` puts its five actions in two `HorizontalStackLayout` rows (Save / Change Password / Log out, then Delete account / Notification settings). A `HorizontalStackLayout` never wraps or shrinks its children. When the buttons are wider than the screen, the last ones are clipped off the right edge. Narrow phones, larger text sizes and translations make it worse. The Android capture shows it in portrait at default text size.

Other issues with today's screen:

- **Destructive actions:** Delete account and Log out look exactly like Save. All of them are solid amber buttons, the brand's primary-action colour.
- **Display name field:** the Entry sits beside the avatar in a narrow column, so long names are cramped.
- **Labels:** "User since: 2026-11-03" and "Groups: 5" read as debug output.
- **Duplicate title:** the "Profile" heading repeats the navigation title. (The app's other pages do the same, so this may stay.)

## Proposed layout (top to bottom)

1. **Header row:** "Profile" title and an icon-only Save, enabled only when there are unsaved changes (011 pattern).
2. **Header card**, centred:
   - A 96 px avatar using the style guide's amber-to-pink gradient with the user's initial, or their photo. Tapping it picks a photo, and a small camera badge signals that. An upload spinner shows while `IsUploading`.
   - Display name (bold) and email (muted) below the avatar.
   - Two small labels, "Member since Nov 2026" and "5 groups" (singular "1 group"), in a `FlexLayout` that wraps rather than clips.
3. **"Account" section card**, made of full-width rows (icon tile, text, chevron):
   - Display name, edited in place, full width.
   - Change password, only when `CanChangePassword`.
   - Notifications, with a subtitle showing the current permission state ("On"/"Off"). Opens device settings, as today.
4. **Second card:**
   - Log out (neutral).
   - Delete account in the style guide's terracotta. The style guide says to avoid harsh red. It opens the existing delete-account flow.
5. **Status message**, then the version text (`VersionText`), as today.

Content is capped at about 600 px wide and centred, so rows stay readable in landscape and on wide screens.

## Constraints found in the repo

- **Delete account subtitle:** don't put a day count in it. The grace period already exists twice, `request_account_deletion()` in SQL and `AccountDeletionPolicy.GracePeriod` (see CLAUDE.md, 016), and the App doesn't reference `LoopMeet.Core`. Use wording without a number, or read it from the Api's deletion preview.
- **Notification status:** `NotificationPermissionService.CurrentState` already exists. Re-read it when the page appears, since the user may change it in device settings.
- **Existing source-inspection tests** read `ProfilePage.xaml` and must keep passing, or be updated deliberately:
  - `ProfileViewModelTests`: `Ellipse`, `UserInitial`, `HasAvatar` and `LogoutCommand` must stay; `AvatarInput`, `Avatar URL` and `AvatarSource` must stay absent.
  - `RefreshSurfaceTests`: the label bound to `VersionText` must stay.
  - The ViewModel must keep the "Profile updated." status message.
- **Icons:** `ic_save`, `ic_change_pwd`, `ic_logout`, `ic_delete_account` and `ic_notification_bell` already exist. `tab_profile.svg` can serve the display-name row. A camera glyph is new.
- **Terracotta:** add it to `Resources/Styles/Colors.xaml` as a named colour. It isn't there yet.
- **CI:** `.github/scripts/ci-scope.sh` treats any path it doesn't list as "run everything", and `plan/` wasn't listed. This material was therefore moved from `plan/profile-redesign/` to `specs/022-profile-redesign/design/` in the spec commit, and `plan/` was deleted. `specs/*` selects no CI suites. (`speckit-prompts.md` keeps its original `plan/` paths as a record of the prompts.)
