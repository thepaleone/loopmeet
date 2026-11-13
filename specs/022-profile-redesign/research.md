# Research: Profile Screen Redesign

Every open point from the spec's Technical Context is settled here. Facts about the code were read from the repo on 2026-11-13.

## D1. Where the testable rules live

**Decision:** put the display rules in two MAUI-free static classes next to `AppVersionText`, and have `ProfileViewModel` call them:

- `Features/Profile/ProfileText.cs` holds the chip texts, the notification status text, the Save rule and the delete subtitle.
- `Features/Profile/ProfileLayout.cs` holds the one- or two-column rule and the content widths.

Both are compiled into `LoopMeet.App.Tests` with a `<Compile Include … Link>` entry, like `AppVersionText.cs` and `MeetupOrganizerText.cs`.

**Rationale:**

- `LoopMeet.App.Tests` targets plain `net10.0`, and can't reference the MAUI app (its csproj says so). It compiles in only MAUI-free sources.
- `ProfileViewModel` can't be built in a test: its `VersionText` initializer calls `AppInfo.Current`, and its commands use `Shell`, `MediaPicker` and `Permissions`. The existing `ProfileViewModelTests` are therefore all source inspection.
- A pure static function isn't an abstraction in Principle IV's sense: no interface, no indirection, no DI. It's the pattern 012 (`MeetupOrganizerText`) and 018 (`AppVersionText`) already use for this exact problem.

**Alternatives considered:**

- *Construct the ViewModel with fakes:* blocked by the MAUI static calls. Wrapping `AppInfo`, `Shell` and the pickers in interfaces would add abstractions with one user each, which Principle IV rejects.
- *Test only by source inspection:* can't check "1 group" or the 800-point boundary, which are exactly the rules most likely to regress.

## D2. Notification state enum

**Decision:** move `NotificationPermissionState` from `NotificationPermissionService.cs` into its own file, `Services/Notifications/NotificationPermissionState.cs`, with the same namespace and members. Link that file into the tests.

**Rationale:** `ProfileText.NotificationStatus(NotificationPermissionState)` needs the enum. The service file can't be linked into the tests, because it uses `OneSignalSDK.DotNet` and MAUI `Preferences`. Moving a three-member enum changes no behaviour, and every existing use compiles unchanged.

**Alternative considered:** *`NotificationStatus(bool granted)`:* hides the three-state mapping that clarification (d) defined, so the test couldn't show that Denied and Unknown both read "Off".

## D3. Showing the current notification state

**Decision:**

- Inject the existing singleton `NotificationPermissionService` into `ProfileViewModel`.
- Add a public `RefreshNotificationStatus()` that sets `NotificationStatusText` from `CurrentState`.
- `ProfilePage.OnAppearing` calls it directly, before starting the load. `LoadCommand` is an `AsyncRelayCommand`, which ignores `Execute` while a previous load runs, so calling it from inside `LoadAsync` would skip the refresh after a quick tab switch.
- `ProfilePage` also subscribes to its window's `Resumed` event in `OnAppearing`, and unsubscribes in `OnDisappearing`. The handler calls `RefreshNotificationStatus()`.

**Rationale:**

- Returning from device Settings resumes the app. It doesn't navigate, so MAUI doesn't raise `OnAppearing` on the Profile page, and spec US4 scenario 3 would fail without the resume hook.
- `Window.Resumed` is already used for session renewal (`App.xaml.cs:27`), so it's a known-good signal on iOS and Android.
- `CurrentState` already reads the OS permission and falls back to the cached value (feature 008). No new service is needed.

**Alternative considered:** *a global resume broadcast:* nothing else needs it, so one subscriber doesn't justify it.

## D4. Save control and the Save rule

**Decision:**

- **Header:** a `Grid RowDefinitions="Auto,*"` with the title row in row 0 and the `ScrollView` in row 1, exactly as Create and Edit Meetup do (011).
- **Save control:** the 011 control: a `💾` `Label` with a `TapGestureRecognizer` bound to `SaveProfileCommand`, and `SemanticProperties.Description="Save profile"`. The Display name entry gets no `ReturnCommand`: a return command whose `CanExecute` is false may disable the field, which would stop typing while Save is inactive.
- **Rule:** `ProfileText.CanSave(savedName, editedName)` is true when `editedName.Trim()` is non-empty and differs, by ordinal comparison, from `savedName`.
- **ViewModel wiring:**
  - `[ObservableProperty] SavedDisplayName` is set in `Apply`.
  - `CanSaveProfile => !IsBusy && ProfileText.CanSave(SavedDisplayName, DisplayName)`.
  - `DisplayName`, `SavedDisplayName` and `IsBusy` carry `[NotifyPropertyChangedFor(nameof(CanSaveProfile))]` and `[NotifyCanExecuteChangedFor(nameof(SaveProfileCommand))]`.
  - `[RelayCommand(CanExecute = nameof(CanSaveProfile))]` on `SaveProfileAsync`.
- **Inactive look:** a `DataTrigger` on `CanSaveProfile` = `False` sets the Save label's `Opacity` to 0.4.
- **Header card name:** binds `SavedDisplayName`, so it keeps the saved name while the user edits (spec US3 scenario 7).

**Rationale:**

- `TapGestureRecognizer` checks `Command.CanExecute` before running, so an inactive Save does nothing.
- `IsBusy` in the rule blocks a double tap and a save during load.
- A failed save leaves `SavedDisplayName` unchanged, so Save stays active for a retry (spec edge case).
- The value sent to the Api is unchanged from today (FR-007).

**Alternatives considered:**

- *`ImageButton` with `ic_save.png`, as in the reference XAML:* `ic_save.png` is the cloud-upload glyph from today's button row, not the 011 disk. The spec asks for the 011 pattern.
- *A navigation-bar `ToolbarItem`:* rejected in clarification (a).

## D5. One or two columns

**Decision:**

- **Rule:** `ProfileLayout` holds the rule: `TwoColumnMinWidth = 800`, `UsesTwoColumns(width) => width >= 800`, and `MaxContentWidth(twoColumns)`, which is 1100 or 600.
- **Structure:** inside the `ScrollView`, one `Grid` (`x:Name="ContentGrid"`) holds two containers: the header card, and a `VerticalStackLayout` (`x:Name="DetailsColumn"`) with the Account card, the session card, the status message and the version text.
- **Switching:** `ProfilePage.OnSizeAllocated` asks `ProfileLayout` for the mode. When the mode changes, it sets:
  - `ContentGrid`'s column and row definitions;
  - `Grid.Column` and `Grid.Row` of the two containers;
  - `MaximumWidthRequest` of `ContentGrid` and of the header row.
- **Columns:** the header card's column is `2*` and the details column is `3*`, matching the mockup's proportions.

**Rationale:**

- Moving the same element instances between grid cells keeps the `Entry` and its unsaved text. Nothing is rebuilt, so the spec's "switch without losing unsaved edits" holds.
- Both columns sit in one `ScrollView`, so a long right column scrolls with the page (spec edge case).
- The rule and its boundary (799.9 versus 800) are unit-tested through `ProfileLayout`. The code-behind only applies the result.

**Alternatives considered:**

- *`VisualStateManager` with `AdaptiveTrigger MinWindowWidth="800"`:* declarative, but it measures the window rather than the page, and its rotation behaviour on Android isn't covered by anything in this repo. A failure would show only on devices.
- *Two separate layouts toggled with `IsVisible`:* duplicates the controls and loses the unsaved edit on a switch.

## D6. Text size and wrapping

**Decision:**

- **Font scaling:** keep MAUI's default `FontAutoScalingEnabled` (true), so labels follow iOS Dynamic Type and Android's font scale.
- **Rows:** set `MinimumHeightRequest="56"` and no `HeightRequest`. Titles and subtitles use `LineBreakMode="WordWrap"`.
- **Name and email:** only these use `MaxLines="1"` and `LineBreakMode="TailTruncation"`.
- **Chips:** in a `FlexLayout Wrap="Wrap" JustifyContent="Center"`, with padding and no fixed size.
- **Icon tiles (34 points), avatar (96) and camera badge (34):** these keep fixed sizes, because they hold glyphs, not text.

**Rationale:** FR-003 allows truncation only for the name and email. Everything else must grow, which needs content-sized rows.

**Risk:** `FlexLayout` inside a stack layout has had height-measurement bugs in earlier MAUI versions. Quickstart rows at the largest text size check that the wrapped chips don't overlap the content below. If they do, the fallback is a `VerticalStackLayout` of the two chips, which always stacks, as the landscape mockup already shows.

## D7. Colours

**Decision:** add to `Resources/Styles/Colors.xaml`, under a "Profile (022)" comment:

| Key | Value | Use | Contrast |
| --- | --- | --- | --- |
| `DestructiveTerracotta` | `#B4513A` | Delete account title and icon tint, light theme | 5.03:1 on the white card |
| `DestructiveTerracottaDark` | `#E8957F` | the same, dark theme | 6.20:1 on the `Gray900` card |
| `AmberTint` | `#FFF1E0` | chip and amber icon-tile background | |
| `AmberTintText` | `#8A4B00` | chip text | 6.13:1 on `AmberTint` |
| `GuavaTint` | `#FFE6EE` | Notifications icon tile | |
| `TerracottaTint` | `#FBE9E4` | Delete account icon tile | |

The page uses `AppThemeBinding` for the terracotta pair only. The tints and the chip text are used in both themes: each chip carries its own background, so its text stays readable, and a dark-mode redesign is out of scope.

**Rationale:**

- The reference XAML's `#C0573E` measures 4.49:1 on white, which just fails FR-017's 4.5:1. `#B4513A` is the nearest darker terracotta that passes.
- The style guide asks for a soft terracotta, not harsh red, and gives no value.
- Spec scope excludes a dark-mode redesign, but the page must stay readable there, which the light terracotta isn't (3.21:1 on `Gray900`).
- A source-inspection test checks that every `StaticResource` key the page uses exists, because a missing key renders silently wrong (project memory).

## D8. Camera glyph

**Decision:** add `Resources/Images/ic_camera.svg`, the Bootstrap Icons `bi-camera` glyph (MIT), the same family as the existing `ic_notification_bell.svg`. Reference it as `ic_camera.png`, the name `MauiImage` gives the generated bitmap.

**Rationale:** in-page icons are single files referenced as `.png`. The `*_fallback.png` pair exists for Shell tab icons. A single SVG scales cleanly for the 18-point badge glyph.

## D9. Page-local styles

**Decision:** the section label, settings row, icon tile, row title, row subtitle, chevron, chip and divider styles go in `ProfilePage.xaml`'s `ContentPage.Resources`, not in `Styles.xaml`. The cards reuse the global `BubbleCardBorderStyle`.

**Rationale:** Principle IV says shared code needs two users. These styles have one, and they can move to `Styles.xaml` when a second page uses them.

## D10. Existing source-inspection tests

| Test | Change |
| --- | --- |
| `ProfileViewModelTests.ProfilePage_HasCircularAvatarBesideDisplayName` | Renamed to `ProfilePage_HasCircularAvatar`: the avatar is no longer beside the name. The `Ellipse`, `UserInitial` and `HasAvatar` assertions stay. |
| `ProfileViewModelTests.ProfilePage_ContainsLogoutButtonWithIcon` | Renamed to `ProfilePage_ContainsLogoutRowWithIcon`. `LogoutCommand` and `ic_logout.png` stay. |
| `ProfileViewModelTests` (the other Profile tests) | Unchanged: `AvatarInput`, `Avatar URL` and `AvatarSource` stay absent, and the ViewModel keeps "Profile updated.". |
| `RefreshSurfaceTests.Profile_ShowsTheVersionFromTheInstalledApp` | "The version line must come after the last button" now always passes, because the page has no `<Button>`. Replace it with "after the Delete account row", which keeps the original intent: the version is the last line. |
| `DeleteAccountSurfaceTests.ProfileViewModel_NavigatesToDeleteAccount` | Unchanged. |

## D11. Logging

**Decision:** no new log statements. The existing `LogError` calls on save, profile load and avatar upload, `LogWarning` on the notification-settings failure, and the session coordinator's sign-out logging stay as they are (Principle VII). A state read and a layout switch aren't key flows.
