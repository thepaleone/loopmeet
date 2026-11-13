# Tasks: Profile Screen Redesign

**Input**: Design documents from `/specs/022-profile-redesign/`
**Prerequisites**: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [quickstart.md](quickstart.md). There is no `contracts/`: no interface changes.

**Tests**: required by the constitution (Principle II). Each story's tests are written first and must fail before the change they guard. The ViewModel can't be constructed in `net10.0` tests, so rules are unit-tested through `ProfileText` and `ProfileLayout`, and wiring through source inspection (research D1). Device-only behaviour is in the quickstart.

**Organization**: one phase per user story, in spec priority order.

- **US1** rebuilds the page into rows and cards. It's the clipping fix and the MVP.
- **US2–US5** each add one behaviour on top of that layout.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: the user story the task belongs to (US1–US5)

**Paths:**

- App code is under `src/LoopMeet.App/`.
- Tests are under `tests/LoopMeet.App.Tests/`.
- Every source-inspection test reads files with the `AppContext.BaseDirectory` plus `"../../../../.."` repo-root pattern used in `RefreshSurfaceTests`.

---

## Phase 1: Setup

**Purpose**: make the new MAUI-free code compile into the test project.

- [X] T001 Move `enum NotificationPermissionState { Unknown, Granted, Denied }` out of `src/LoopMeet.App/Services/Notifications/NotificationPermissionService.cs` into a new `src/LoopMeet.App/Services/Notifications/NotificationPermissionState.cs`, with the same namespace and member order. Nothing else in the service changes (research D2).
- [X] T002 Create `src/LoopMeet.App/Features/Profile/ProfileText.cs` and `src/LoopMeet.App/Features/Profile/ProfileLayout.cs` as `public static class`es in namespace `LoopMeet.App.Features.Profile`, each with a `<summary>` naming feature 022 and stating that the file must stay free of `Microsoft.Maui.*`. In `tests/LoopMeet.App.Tests/LoopMeet.App.Tests.csproj`, add `<Compile Include … Link="Included\…">` entries for both, and for `NotificationPermissionState.cs`, next to the `AppVersionText.cs` entry. Run `dotnet test tests/LoopMeet.App.Tests`: it builds and passes, with 244 tests as before.

---

## Phase 2: Foundational

**Purpose**: the shared assets and the resource-key guard that every story's markup relies on.

- [X] T003 In `src/LoopMeet.App/Resources/Styles/Colors.xaml`, add a `<!-- Profile (022) -->` block after the Gray scale with the six colours from research D7:
  - `DestructiveTerracotta` `#B4513A`;
  - `DestructiveTerracottaDark` `#E8957F`;
  - `AmberTint` `#FFF1E0`;
  - `AmberTintText` `#8A4B00`;
  - `GuavaTint` `#FFE6EE`;
  - `TerracottaTint` `#FBE9E4`.
- [X] T004 [P] Add `src/LoopMeet.App/Resources/Images/ic_camera.svg`: the Bootstrap Icons `bi-camera` glyph (MIT), in the same 16×16 `viewBox` form as `ic_notification_bell.svg` (research D8).
- [X] T005 Create `tests/LoopMeet.App.Tests/Features/Profile/ProfilePageLayoutTests.cs` with:
  - a class summary naming feature 022;
  - a `ReadSource(repoRelativePath)` helper;
  - the first test, `ProfilePage_UsesOnlyResourceKeysThatExist`. It collects every `{StaticResource Key}` in `src/LoopMeet.App/Features/Profile/Views/ProfilePage.xaml` and asserts each key is defined, as `x:Key="Key"`, in `Resources/Styles/Colors.xaml`, `Resources/Styles/Styles.xaml` or the page itself. A missing key renders silently wrong (project memory).

  It passes against today's page.

**Checkpoint**: `dotnet test tests/LoopMeet.App.Tests` passes.

---

## Phase 3: User Story 1 - Every Profile action is reachable on any screen (Priority: P1) 🎯 MVP

**Goal**: every action is a full-width row in a card, or the header Save. Nothing is in a non-wrapping horizontal row. There's one column under 800 points (up to 600 wide) and two columns from 800 (up to 1100 wide).

**Independent Test**: quickstart rows A1–A18 and B8. Nothing is clipped, and every action works. A9 is the case that's clipped today.

### Tests for User Story 1 ⚠️ write first, confirm they fail

- [X] T006 [P] [US1] Create `tests/LoopMeet.App.Tests/Features/Profile/ProfileLayoutTests.cs` with these cases:
  - `UsesTwoColumns` is false for 0, 375 and 799.99, and true for 800, 956 and 1366;
  - `MaxContentWidth(false)` is 600, and `MaxContentWidth(true)` is 1100;
  - `TwoColumnMinWidth` is 800.
- [X] T007 [US1] In `ProfilePageLayoutTests.cs`, add:
  - `ProfilePage_HasNoHorizontalStackLayout`: the regression test for the clipped buttons (spec Background).
  - `ProfilePage_BindsEveryActionToItsExistingCommand`: a `[Theory]` over `SaveProfileCommand`, `OpenPasswordChangeCommand`, `OpenNotificationSettingsCommand`, `LogoutCommand`, `DeleteAccountCommand` and `PickAvatarCommand`, each appearing as `Command="{Binding <name>}"`.
  - `ProfilePage_KeepsSaveOutsideTheScrollView`: the first `SaveProfileCommand` comes before `<ScrollView`, and the page root is `<Grid RowDefinitions="Auto,*"`.
  - `ProfilePage_EditsTheDisplayNameInPlace`: an `Entry` with `Text="{Binding DisplayName}"` sits inside the Account card, after the "Account" section label.
  - `ProfilePage_ShowsChangePasswordOnlyWhenAllowed`: the row carrying `OpenPasswordChangeCommand` has `IsVisible="{Binding CanChangePassword}"`.
  - `ProfilePage_SwitchesColumnsByWidth`: the XAML has `x:Name="ContentGrid"`, `x:Name="DetailsColumn"` and `x:Name="HeaderRow"`. `ProfilePage.xaml.cs` overrides `OnSizeAllocated` and calls `ProfileLayout.UsesTwoColumns` and `ProfileLayout.MaxContentWidth`.
  - `ProfilePage_RowsGrowWithText`: no `HeightRequest=` on any element whose `Style` is `SettingsRow`, and the page-local `RowTitle` and `RowSubtitle` styles set `LineBreakMode` to `WordWrap`.
- [X] T008 [P] [US1] Update the existing tests deliberately (research D10):
  - In `tests/LoopMeet.App.Tests/Features/Profile/ProfileViewModelTests.cs`, rename `ProfilePage_HasCircularAvatarBesideDisplayName` to `ProfilePage_HasCircularAvatar` and `ProfilePage_ContainsLogoutButtonWithIcon` to `ProfilePage_ContainsLogoutRowWithIcon`. Their assertions don't change.
  - In `tests/LoopMeet.App.Tests/Features/RefreshSurfaceTests.cs` `Profile_ShowsTheVersionFromTheInstalledApp`, replace the "after the last `<Button`" assertion with: the `VersionText` label comes after the last `DeleteAccountCommand`, with the message "the version line must be the last line of the page".

  Run `dotnet test tests/LoopMeet.App.Tests`. T006 doesn't compile until T009, and T007's tests and the new `RefreshSurfaceTests` anchor fail.

### Implementation for User Story 1

- [X] T009 [P] [US1] Implement `ProfileLayout` in `src/LoopMeet.App/Features/Profile/ProfileLayout.cs`:
  - `public const double TwoColumnMinWidth = 800`;
  - `UsesTwoColumns(double width) => width >= TwoColumnMinWidth`;
  - `MaxContentWidth(bool twoColumns) => twoColumns ? 1100 : 600`.

  T006 passes.
- [X] T010 [US1] Rewrite `src/LoopMeet.App/Features/Profile/Views/ProfilePage.xaml`, following `design/ProfilePage.reference.xaml` and research D4–D9. Keep the `x:Class` and `Title="Profile"`, and set `BackgroundColor="{StaticResource AppBackgroundColor}"`.
  - **Page resources (D9):** the styles `SectionLabel`, `SettingsRow` (a Grid with `ColumnDefinitions="40,*,Auto"`, `ColumnSpacing="14"`, `Padding="16,12"` and `MinimumHeightRequest="56"`), `IconTile` (Border, 34×34, `RoundRectangle 10`), `RowTitle` (16, Bold, `TextMain`, `WordWrap`), `RowSubtitle` (13, `TextMuted`, `WordWrap`), `Chevron` (`›`, 22, `Gray400`) and `RowDivider` (BoxView, height 1, `Gray100`).
  - **Root:** `<Grid RowDefinitions="Auto,*">`.
  - **Row 0, `x:Name="HeaderRow"`:** a `Grid` with `ColumnDefinitions="*,Auto"`, `Padding="20,16,20,0"`, `MaximumWidthRequest="600"` and `HorizontalOptions="Fill"`. It holds a "Profile" label (24, Bold), and in column 1 the 011 Save label: `Text="💾"`, `FontSize="24"`, and a `TapGestureRecognizer` with `Command="{Binding SaveProfileCommand}"`. US2 adds the rule, description and dimming.
  - **Row 1:** a `ScrollView` containing `ContentGrid`, a `Grid` with `RowDefinitions="Auto,Auto"`, `ColumnDefinitions="*"`, `RowSpacing="16"`, `ColumnSpacing="20"`, `Padding="20,16,20,32"`, `MaximumWidthRequest="600"` and `HorizontalOptions="Fill"`.
  - **Header card** (in `ContentGrid` row 0), a `Border` with `BubbleCardBorderStyle`:
    - a 96×96 avatar `Grid` with a `TapGestureRecognizer` bound to `PickAvatarCommand`;
    - inside it, a `Border` with `StrokeShape="Ellipse"` holding the `UserInitial` label and the `AvatarUrl` image (`IsVisible="{Binding HasAvatar}"`, `AspectFill`);
    - an `ActivityIndicator` bound to `IsUploading`;
    - below the avatar, `DisplayName` and `Email` labels. US2 and US3 finish this card.
  - **`DetailsColumn`** (in `ContentGrid` row 1), a `VerticalStackLayout` with:
    - an "Account" `SectionLabel`;
    - an Account card (`BubbleCardBorderStyle`, `Padding="0"`):
      - a display-name row (`tab_profile.svg` tile on `AmberTint`; a "Display name" caption over a full-width `Entry Text="{Binding DisplayName}"`);
      - a divider;
      - a Change password row (`ic_change_pwd.png` on `AmberTint`, title, chevron, `IsVisible="{Binding CanChangePassword}"`, tap bound to `OpenPasswordChangeCommand`), and its divider, also bound to `CanChangePassword`;
      - a Notifications row (`ic_notification_bell_fallback.png` on `GuavaTint`, "Notifications" title, chevron, tap bound to `OpenNotificationSettingsCommand`);
    - a second card with `Margin="0,16,0,0"`:
      - a Log out row (`ic_logout.png` on `Gray100`, "Log out", tap bound to `LogoutCommand`);
      - a divider;
      - a Delete account row (`ic_delete_account.png` on `TerracottaTint`, "Delete account", chevron, tap bound to `DeleteAccountCommand`);
    - the `StatusMessage` label (`IsVisible="{Binding ShowStatus}"`, centred, `WordWrap`);
    - the `VersionText` label, unchanged from today, as the last element.

  No `HorizontalStackLayout` and no `<Button>`.
- [X] T011 [US1] In `src/LoopMeet.App/Features/Profile/Views/ProfilePage.xaml.cs`, add a `bool? _twoColumns` field and override `OnSizeAllocated(double width, double height)`. Call `base` first. If `width > 0` and `ProfileLayout.UsesTwoColumns(width)` differs from `_twoColumns`, store it, then:
  - **Two columns:**
    - `ContentGrid.ColumnDefinitions` = `2*`, `3*`, and `RowDefinitions` = `Auto`;
    - the header card goes in column 0, row 0, with `VerticalOptions="Start"`;
    - `DetailsColumn` goes in column 1, row 0.
  - **One column:**
    - one `*` column, and `Auto,Auto` rows;
    - the header card goes in row 0, and `DetailsColumn` in row 1, both in column 0.
  - **Both:** set `ContentGrid.MaximumWidthRequest` and `HeaderRow.MaximumWidthRequest` to `ProfileLayout.MaxContentWidth(_twoColumns.Value)`.

  Give the header card `x:Name="HeaderCard"`. Keep the existing constructor and the `OnAppearing` load.
- [X] T012 [US1] Run `dotnet test tests/LoopMeet.App.Tests`: T005–T008 pass. Then build `dotnet build src/LoopMeet.App -f net10.0-ios -r iossimulator-arm64` and `dotnet build src/LoopMeet.App -c Debug -f net10.0-android`, with no new warnings. On an iPad simulator, check that `HeaderRow` and `ContentGrid` are centred at their maximum width. If MAUI places a `Fill` element with a maximum width at the start instead, use `HorizontalOptions="Center"` with `WidthRequest` set to the maximum width in `OnSizeAllocated` (capped at the page width), and record the outcome in the log below (analysis U3).

**Checkpoint**: the clipping is fixed. All actions are reachable in one or two columns. This is shippable alone.

---

## Phase 4: User Story 2 - Edit the display name and save it from a header that stays in view (Priority: P1)

**Goal**: Save is active only for a changed, non-blank name, has a screen-reader description and dims when inactive. The header card keeps the saved name while editing.

**Independent Test**: quickstart B1–B9.

### Tests for User Story 2 ⚠️ write first, confirm they fail

- [X] T013 [P] [US2] Create `tests/LoopMeet.App.Tests/Features/Profile/ProfileTextTests.cs` with `CanSave` cases from data-model.md:
  - ("Sarah Chen", "Sarah Chen") gives false;
  - ("Sarah Chen", "Sarah C.") gives true;
  - ("Sarah Chen", "") and ("Sarah Chen", "   ") give false;
  - ("Sarah Chen", "Sarah Chen ") gives false;
  - ("", "Sarah") gives true;
  - ("Sarah Chen", "sarah chen") gives true (ordinal comparison).
- [X] T014 [US2] In `ProfilePageLayoutTests.cs`, add:
  - `ProfilePage_SaveHasAccessibleDescriptionAndDimsWhenInactive`: the Save label has `SemanticProperties.Description="Save profile"` and a `DataTrigger` with `Binding="{Binding CanSaveProfile}" Value="False"` setting `Opacity` to `0.4`.
  - `ProfilePage_DisplayNameEntryIsNotGatedBySave`: the display-name `Entry` has no `ReturnCommand`. A return command that can't execute may disable the field in MAUI, which would stop typing while Save is inactive (analysis U1).
  - `ProfilePage_HeaderCardShowsTheSavedName`: the header card binds `SavedDisplayName`.
  - `ProfileViewModel_SaveRequiresAnUnsavedNonBlankName`: `ProfileViewModel.cs` contains `CanExecute = nameof(CanSaveProfile)`, `ProfileText.CanSave(SavedDisplayName, DisplayName)` and `SavedDisplayName = profile.DisplayName`, and `[NotifyCanExecuteChangedFor(nameof(SaveProfileCommand))]` appears at least three times.

### Implementation for User Story 2

- [X] T015 [P] [US2] Add `public static bool CanSave(string? savedName, string? editedName)` to `src/LoopMeet.App/Features/Profile/ProfileText.cs`. It returns true when `editedName?.Trim()` is non-empty and differs, by ordinal comparison, from `savedName`. T013 passes.
- [X] T016 [US2] In `src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs`:
  - Add `[ObservableProperty] private string _savedDisplayName = string.Empty;`.
  - Add `public bool CanSaveProfile => !IsBusy && ProfileText.CanSave(SavedDisplayName, DisplayName);`.
  - Put `[NotifyPropertyChangedFor(nameof(CanSaveProfile))]` and `[NotifyCanExecuteChangedFor(nameof(SaveProfileCommand))]` on `_isBusy`, `_displayName` and `_savedDisplayName`.
  - Change `SaveProfileAsync`'s attribute to `[RelayCommand(CanExecute = nameof(CanSaveProfile))]`.
  - In `Apply`, set `SavedDisplayName = profile.DisplayName`.

  Keep the `IsBusy` guard, the Api call, the "Profile updated." message and the error logging unchanged (FR-007).
- [X] T017 [US2] In `ProfilePage.xaml`:
  - add `SemanticProperties.Description="Save profile"` and the `Opacity` `DataTrigger` to the Save label;
  - give the display-name `Entry` `ReturnType="Done"` and no `ReturnCommand`. Save is the header control only;
  - bind the header card's name label to `SavedDisplayName`.

  T014 passes.

**Checkpoint**: US1 and US2 work. Run `dotnet test tests/LoopMeet.App.Tests`.

---

## Phase 5: User Story 3 - See who you are at a glance (Priority: P2)

**Goal**: the gradient avatar with a camera badge, a one-line name and email, and the wrapping "Member since" and group-count chips.

**Independent Test**: quickstart B10–B13 and rows A2 and A10 for chip wrapping.

### Tests for User Story 3 ⚠️ write first, confirm they fail

- [X] T018 [P] [US3] In `ProfileTextTests.cs`, add:
  - `MemberSince`: `2026-11-03T10:00:00+00:00` gives "Member since Nov 2026", `2027-01-31T23:00:00-08:00` gives "Member since Jan 2027" (the value's own offset is used), and `2026-05-01` gives "Member since May 2026".
  - `GroupCount`: 0 gives "0 groups", 1 gives "1 group", 2 gives "2 groups", and 5 gives "5 groups".
- [X] T019 [US3] In `ProfilePageLayoutTests.cs`, add:
  - `ProfilePage_AvatarUsesTheBrandGradientAndCameraBadge`: a `LinearGradientBrush` with `GradientStop`s on `Primary` and `Secondary`, `ic_camera.png` referenced, and `src/LoopMeet.App/Resources/Images/ic_camera.svg` existing.
  - `ProfilePage_ShowsUploadProgressOnTheAvatar`: an `ActivityIndicator` with `IsRunning="{Binding IsUploading}"` is inside the avatar grid, the one carrying `PickAvatarCommand`.
  - `ProfilePage_NameAndEmailTruncateOnOneLine`: the labels bound to `SavedDisplayName` and `Email` each have `MaxLines="1"` and `LineBreakMode="TailTruncation"`.
  - `ProfilePage_SummaryChipsWrap`: a `FlexLayout` with `Wrap="Wrap"` contains bindings to `MemberSinceText` and `GroupCountText`.
  - `ProfilePage_HasNoDebugStyleLabels`: the XAML doesn't contain `User since` or `Groups:`.
  - `ProfileViewModel_DerivesSummaryTexts`: the ViewModel source contains `ProfileText.MemberSince(` and `ProfileText.GroupCount(`.

### Implementation for User Story 3

- [X] T020 [P] [US3] Add `MemberSince(DateTimeOffset userSince)` and `GroupCount(int count)` to `ProfileText.cs`:
  - `MemberSince` returns `"Member since " + userSince.ToString("MMM yyyy", CultureInfo.InvariantCulture)`.
  - `GroupCount` returns `count == 1 ? "1 group" : $"{count} groups"`.

  T018 passes.
- [X] T021 [US3] In `ProfileViewModel.cs`, replace `[ObservableProperty] _userSince` and `_groupCount` with `[ObservableProperty] private string _memberSinceText = string.Empty;` and `_groupCountText`. Set them in `Apply` from `ProfileText.MemberSince(profile.UserSince)` and `ProfileText.GroupCount(profile.GroupCount)`. The old properties' only reader was the old XAML, so they would be dead code (Principle I).
- [X] T022 [US3] In `ProfilePage.xaml`, finish the header card:
  - **Avatar:** the avatar `Border` gets a `LinearGradientBrush` background (`StartPoint="0,0"` to `EndPoint="1,1"`, `Primary` to `Secondary`), and the initial is white, 42, Bold.
  - **Camera badge:** a 34×34 white `Ellipse` border at `End,End`, with a light shadow and `ic_camera.png` at 18×18.
  - **Name and email:** the name label (`SavedDisplayName`, 21, Bold, centred, `MaxLines="1"`, `TailTruncation`), and the email label (`TextMuted`, 14, centred, `MaxLines="1"`, `TailTruncation`).
  - **Chips:** a `FlexLayout Wrap="Wrap" JustifyContent="Center"` with two chip `Border`s: `AmberTint` background, `RoundRectangle 999`, `Padding="12,5"`, `Margin="4"`, and a label in 13 Bold `AmberTintText`, bound to `MemberSinceText` and `GroupCountText`.

  T019 passes.

**Checkpoint**: run `dotnet test tests/LoopMeet.App.Tests` and the iOS simulator build. Check row A2 on a simulator at the largest text size: the chips wrap without overlapping (research D6 risk).

---

## Phase 6: User Story 4 - Know your notification state and reach its settings (Priority: P2)

**Goal**: the Notifications subtitle shows On or Off, refreshed each time the page appears and when the app resumes from Settings.

**Independent Test**: quickstart B15–B18.

### Tests for User Story 4 ⚠️ write first, confirm they fail

- [X] T023 [P] [US4] In `ProfileTextTests.cs`, add `NotificationStatus`:
  - `Granted` gives "On · Manage in device settings";
  - `Denied` gives "Off · Turn on in device settings";
  - `Unknown` gives "Off · Turn on in device settings".
- [X] T024 [US4] In `ProfilePageLayoutTests.cs`, add:
  - `ProfilePage_NotificationsRowShowsTheCurrentState`: the XAML binds `NotificationStatusText`.
  - `ProfilePage_RefreshesNotificationStateOnEveryAppearance`: the ViewModel source contains `RefreshNotificationStatus()`, `_notificationPermissionService.CurrentState` and `ProfileText.NotificationStatus(`, and `ProfilePage.xaml.cs` `OnAppearing` calls `RefreshNotificationStatus()` directly, not through `LoadCommand`.
  - `ProfilePage_RefreshesNotificationStateWhenTheAppResumes`: `ProfilePage.xaml.cs` contains `Resumed +=` in `OnAppearing`, `Resumed -=` in `OnDisappearing`, and a handler calling `RefreshNotificationStatus()`.

### Implementation for User Story 4

- [X] T025 [P] [US4] Add `NotificationStatus(NotificationPermissionState state)` to `ProfileText.cs`, using a `switch` expression: `Granted` gives "On · Manage in device settings", and anything else gives "Off · Turn on in device settings". T023 passes.
- [X] T026 [US4] In `ProfileViewModel.cs`:
  - inject `NotificationPermissionService notificationPermissionService` as the constructor's last service before `ILogger`, stored in `_notificationPermissionService` (already a singleton in `MauiProgram.cs`);
  - add `[ObservableProperty] private string _notificationStatusText = string.Empty;`;
  - add `public void RefreshNotificationStatus() => NotificationStatusText = ProfileText.NotificationStatus(_notificationPermissionService.CurrentState);`;
- [X] T027 [US4] In `ProfilePage.xaml.cs`:
  - add a `Window? _subscribedWindow` field;
  - in `OnAppearing`, call `viewModel.RefreshNotificationStatus()` directly, before the existing `LoadCommand.Execute`. The load command ignores calls while a load runs, so the refresh can't go through it (analysis U2). Then subscribe `OnWindowResumed` to `Window.Resumed` when `Window` isn't null, and remember the window;
  - override `OnDisappearing` to unsubscribe from the remembered window and clear it;
  - `OnWindowResumed` calls `(BindingContext as ProfileViewModel)?.RefreshNotificationStatus()`.
- [X] T028 [US4] In `ProfilePage.xaml`, change the Notifications row's text column to a `VerticalStackLayout` with the "Notifications" `RowTitle` and a `RowSubtitle` bound to `NotificationStatusText`. T024 passes.

**Checkpoint**: run `dotnet test tests/LoopMeet.App.Tests`.

---

## Phase 7: User Story 5 - Tell destructive actions apart from routine ones (Priority: P2)

**Goal**: Log out is neutral, and Delete account is terracotta, with the fixed subtitle and enough contrast in both themes.

**Independent Test**: quickstart B19–B22.

### Tests for User Story 5 ⚠️ write first, confirm they fail

- [X] T029 [P] [US5] In `ProfileTextTests.cs`, add `DeleteAccountSubtitle_HasNoDuration`: the constant equals "Review what's deleted before you confirm" and contains no digit (FR-016).
- [X] T030 [P] [US5] Create `tests/LoopMeet.App.Tests/Features/Profile/ProfileColorContrastTests.cs`. It parses `src/LoopMeet.App/Resources/Styles/Colors.xaml` for `DestructiveTerracotta`, `DestructiveTerracottaDark`, `White`, `Gray900`, `AmberTintText` and `AmberTint`, computes the WCAG 2.x contrast ratio with a private helper, and asserts at least 4.5 for each pair (FR-017):
  - `DestructiveTerracotta` on `White`;
  - `DestructiveTerracottaDark` on `Gray900`;
  - `AmberTintText` on `AmberTint`.
- [X] T031 [US5] In `ProfilePageLayoutTests.cs`, add:
  - `ProfilePage_DeleteAccountUsesTerracottaAndTheSharedSubtitle`: the Delete account title's `TextColor` is `{AppThemeBinding Light={StaticResource DestructiveTerracotta}, Dark={StaticResource DestructiveTerracottaDark}}`, and the subtitle uses `{x:Static profile:ProfileText.DeleteAccountSubtitle}`.
  - `ProfilePage_DestructiveRowsDoNotUsePrimary`: the slice from `LogoutCommand` to the end of the Delete account row contains no `StaticResource Primary}`.

### Implementation for User Story 5

- [X] T032 [P] [US5] Add `public const string DeleteAccountSubtitle = "Review what's deleted before you confirm";` to `ProfileText.cs`, with a comment that the grace period lives in SQL and `AccountDeletionPolicy` and must not be repeated here (016). T029 passes, and T030 passes against T003's colours.
- [X] T033 [US5] In `ProfilePage.xaml`:
  - add `xmlns:profile="clr-namespace:LoopMeet.App.Features.Profile"`;
  - give the Delete account row a `VerticalStackLayout` with the title in the terracotta `AppThemeBinding` and a `RowSubtitle` with `Text="{x:Static profile:ProfileText.DeleteAccountSubtitle}"`;
  - keep Log out's title in `RowTitle` (`TextMain`) on its `Gray100` tile.

  T031 passes.

**Checkpoint**: all five stories are done. Run `dotnet test tests/LoopMeet.App.Tests`.

---

## Phase 8: Polish & Cross-Cutting Concerns

- [X] T034 [P] In `CLAUDE.md`, edited by hand (`update-agent-context.sh` damages it):
  - Add a `022-profile-redesign` line at the top of Recent Changes: the Profile tab is rebuilt as rows and cards, with one or two columns at 800 points; `ProfileText` and `ProfileLayout` are MAUI-free and linked into `LoopMeet.App.Tests`; there are new colours in `Colors.xaml` and `ic_camera.svg`; and there are no Api or schema changes.
  - Add a gotcha: the Profile page's display rules live in `ProfileText` and `ProfileLayout` because the ViewModel can't be constructed in tests; keep those files MAUI-free.
- [X] T035 Run every suite: Api, App, Core, Infrastructure, `deno test` for `web/` and `supabase/functions/`, and `bash .github/scripts/ci-scope.test.sh`. Then build the iOS simulator and Android Debug builds, and confirm there are no new warnings beyond the existing Settings.plist, CA1422, CS0618 and XA0033. Record the counts in the log below.
- [ ] T036 Maintainer: run the [quickstart.md](quickstart.md) device matrix (A1–A18, B1–B23) and record the results in its Results table.
- [ ] T037 Check the design input against what was built: open `design/mockups/ios_portrait.png` and `ios_landscape.png` next to simulator screenshots, and note any intentional difference in the log below. Known differences: Save is in the title row, and Save is the 011 💾 label.

---

## Dependencies & Execution Order

- **Setup (T001–T002)** comes first. Everything links against these files.
- **Foundational (T003–T005)** blocks every story: the colours and icon are used by the markup, and T005 creates the shared test class.
- **US1 (T006–T012)** blocks US2–US5. It creates the page structure they extend.
- **US2–US5** touch the same three files (`ProfilePage.xaml`, `ProfileViewModel.cs` and `ProfileText.cs`), so run them one after another in priority order: US2, US3, US4, US5. Within a story, the tests come before the implementation.
- **Polish (T034–T037)** comes after US5. T036 needs a build on devices.

## Parallel Opportunities

```text
Phase 2:  T003 (Colors.xaml)  ‖  T004 (ic_camera.svg)
US1:      T006 (ProfileLayoutTests)  ‖  T008 (existing test updates)  →  T009 (ProfileLayout)  ‖  T007 (page tests)
Each of US2–US5: the ProfileText unit test  ‖  the page/ViewModel inspection test, then the ProfileText change  ‖  nothing else
US5:      T029  ‖  T030  ‖  T031
Polish:   T034  ‖  T035
```

## Implementation Strategy

1. **MVP (US1):** Phases 1–3 fix the clipping on their own. Stop at the US1 checkpoint and run quickstart A9 if an early build is wanted.
2. **Incremental:** add US2, the Save rule. It's P1 and should ship with US1, because US1's header Save has no rule yet. Then add US3, US4 and US5, running the App suite at each checkpoint.
3. **One PR:** the whole feature is app-only and touches one page, so it ships as one PR after T035, with T036's device results recorded in a follow-up docs commit if they come later, as 021 did.

## Execution Log

| Task | Date | Notes |
| --- | --- | --- |
| T001–T002 | 2026-11-13 | Enum moved, `ProfileText` and `ProfileLayout` created and linked. App suite 227 passing: this branch is based on `main`, which doesn't include 021's 17 tests, so 244 in the task text doesn't apply here. |
| T003–T005 | 2026-11-13 | Six colours added (CRLF kept in `Colors.xaml`). `ic_camera.svg` is Bootstrap Icons 1.11.3 `camera.svg`, the same release as the existing bell (diffed). Resource-key guard passes. |
| T006–T008 | 2026-11-13 | `ProfileLayoutTests` failed to compile first. Five of the seven page tests failed against the old page; `ShowsChangePasswordOnlyWhenAllowed` and the re-anchored `RefreshSurfaceTests` check also held there. The first test anchored on `<Grid` and matched `<Grid.GestureRecognizers>`; fixed to `<Grid `. |
| T009–T011 | 2026-11-13 | Page rewritten; the column switch is in `OnSizeAllocated`. Display-name tile uses `tab_profile_fallback.png`, as the Shell does. |
| T012 | 2026-11-13 | App 249 passed. iOS simulator and Android Debug builds succeed with only existing warnings. **Pending:** the iPad centring check needs a signed-in session on a simulator, so it moves to the maintainer's quickstart run (A17). |
| T013–T017 | 2026-11-13 | `CanSave` failed to compile first; three page/ViewModel tests failed, then passed. App 261. |
| T018–T022 | 2026-11-13 | Chip texts failed to compile first; five page/ViewModel tests failed, then passed. The `-08:00` January case confirms the value's own offset is used. `UserSince` and `GroupCount` removed from the ViewModel (only the old XAML read them). App 275; iOS build has only the existing four warnings. |
| T023–T028 | 2026-11-13 | `NotificationStatus` failed to compile first. `OnAppearing` refreshes directly before the load, and `Window.Resumed` is subscribed in `OnAppearing` and removed in `OnDisappearing`. App 281. |
| T029–T033 | 2026-11-13 | `DeleteAccountSubtitle` failed to compile first. T030 passed against T003's colours as planned. T031's page tests were written before T033 but not run in between; the earlier page had no terracotta or subtitle, so they couldn't have passed. App 287. |
| T034 | 2026-11-13 | `CLAUDE.md`: one Recent Changes line and one gotcha, by hand. |
| T035 | 2026-11-13 | Api 145, App 287, Core 29, Infrastructure 1, web 60, edge functions 107: all pass. Both `deno lint` runs clean. `ci-scope.test.sh` 22 cases pass. iOS simulator (full rebuild) has only the Settings.plist, CA1422 and two CS0618 warnings. Android Debug has only the existing CS0618, CS8602 (`GroupDetailViewModel`, `MainActivity`) and XA0033. |
| T037 | 2026-11-13 | **Pending:** comparing against the mockups needs the signed-in app on a simulator; no UI automation is available here. To do with T036. |
