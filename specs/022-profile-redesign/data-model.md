# Data Model: Profile Screen Redesign

**There are no data or contract changes.** This feature changes no Api endpoint, request or response shape, no Supabase table, function or policy, and no stored preference. `UserProfileResponse` and `UserProfileCache` are unchanged. The page reads only what the existing profile load already returns, plus the existing notification permission state.

There is no `contracts/` directory, because no interface changes.

What follows is the page's display state: values derived on the device and never stored.

## Profile summary (from `UserProfileResponse`, unchanged)

| Field | Source | Used for |
| --- | --- | --- |
| `DisplayName` | profile load | the Display name entry (editable) |
| `SavedDisplayName` | the last loaded or saved `DisplayName` | the header card name, the Save rule |
| `Email` | profile load | the header card, one line, truncated |
| `AvatarUrl`, `HasAvatar`, `UserInitial` | profile load (unchanged) | the avatar photo, or the initial on the gradient |
| `UserSince` | profile load | `MemberSinceText` |
| `GroupCount` | profile load | `GroupCountText` |
| `CanChangePassword` | profile load | shows the Change password row |

## Derived texts (`ProfileText`, MAUI-free, unit-tested)

| Property | Rule | Examples |
| --- | --- | --- |
| `MemberSinceText` | `"Member since " + UserSince.ToString("MMM yyyy", CultureInfo.InvariantCulture)` | 2026-11-03 gives "Member since Nov 2026" |
| `GroupCountText` | `n == 1 ? "1 group" : $"{n} groups"` | 0 gives "0 groups", 1 gives "1 group", 5 gives "5 groups" |
| `NotificationStatusText` | Granted gives "On · Manage in device settings". Denied and Unknown give "Off · Turn on in device settings". | |
| `DeleteAccountSubtitle` | the constant "Review what's deleted before you confirm" | no digits (FR-016) |

`MemberSince` formats `UserSince` in whatever offset the Api returns it in, not the device's time zone. So if that offset is UTC, someone who joined late on 31 October in a time zone behind UTC sees "Nov". Today's "User since" date behaves the same way, and the difference is at most one month at a month's edge, so it's accepted (analysis A1).

## Save rule

`CanSaveProfile = !IsBusy && ProfileText.CanSave(SavedDisplayName, DisplayName)`, where `CanSave(saved, edited)` is true when `edited.Trim()` is non-empty and not equal, by ordinal comparison, to `saved`.

| Saved | Edited | Busy | Save |
| --- | --- | --- | --- |
| "Sarah Chen" | "Sarah Chen" | no | inactive |
| "Sarah Chen" | "Sarah C." | no | active |
| "Sarah Chen" | "" or "   " | no | inactive |
| "Sarah Chen" | "Sarah Chen " | no | inactive (only spaces differ) |
| "Sarah Chen" | "Sarah C." | yes (loading or saving) | inactive |

**State changes:**

- A successful save sets `SavedDisplayName` to the saved value, so Save becomes inactive.
- A failed save changes nothing, so Save stays active for a retry.

## Layout mode (`ProfileLayout`, MAUI-free, unit-tested)

| Page width | Mode | Maximum content width |
| --- | --- | --- |
| under 800 | one column | 600 |
| 800 or more | two columns: the header card left (`2*`), the details right (`3*`) | 1100 |

**Switching:** the mode is re-evaluated on every size change. Switching moves the existing views between grid cells, and keeps the unsaved `DisplayName`.
