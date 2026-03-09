# Tasks: UI Polish — Icons & Button Placement

**Input**: Design documents from `/specs/004-ui-polish/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, quickstart.md ✅

**Tests**: One unit test is explicitly required by the plan (LogoutCommand move). All other verification is manual acceptance testing per quickstart.md.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1–US5)

---

## Phase 1: Setup — Icon Assets

**Purpose**: Source and add the three new icon pairs to `src/LoopMeet.App/Resources/Images/`. These are prerequisites for US1, US4, and US5 (US2 and US3 need no new image assets and can start immediately in parallel).

- [ ] T001 [P] Source `ic_logout.svg` (e.g., Heroicons `arrow-right-on-rectangle`, MIT licence) and export `ic_logout_fallback.png` at 32×32px; add both to `src/LoopMeet.App/Resources/Images/`
- [ ] T002 [P] Source `ic_save.svg` (e.g., Heroicons `cloud-arrow-up` or `archive-box-arrow-down`, MIT licence) and export `ic_save_fallback.png` at 32×32px; add both to `src/LoopMeet.App/Resources/Images/`
- [ ] T003 [P] Source `ic_invite.svg` (e.g., Heroicons `envelope` or `user-plus`, MIT licence) and export `ic_invite_fallback.png` at 32×32px; add both to `src/LoopMeet.App/Resources/Images/`

**Checkpoint**: Three SVG + PNG fallback pairs present in `Resources/Images/`. US2 and US3 can start immediately without waiting for this phase.

---

## Phase 2: User Story 1 — Logout Moves to Profile Page (Priority: P1) 🎯 MVP

**Goal**: Remove the Logout button from the Groups page header and add it — with a logout icon — to the Profile page. The user can only sign out from the Profile page after this change.

**Independent Test**: Navigate to the Profile page as a signed-in user; confirm the Logout button with logout icon is present; tap it and confirm sign-out and navigation to login. Navigate to Groups page; confirm no Logout button exists.

**Prerequisite**: T001 (ic_logout asset) must be complete before T005.

### Implementation for User Story 1

- [ ] T004 [US1] Add `LogoutAsync()` method and `[RelayCommand]`-decorated `LogoutCommand` (guarded by `IsBusy`) to `src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs` — copy implementation from `GroupsListViewModel.LogoutAsync()` (`_authService.SignOutAsync()` then `Shell.Current.GoToAsync("//login")`)
- [ ] T005 [US1] Add Logout `<Button>` to `src/LoopMeet.App/Features/Profile/Views/ProfilePage.xaml` after the Change Password button, with `Text="Log out"`, `ImageSource="ic_logout_fallback.png"`, `ContentLayout="Left,8"`, `Command="{Binding LogoutCommand}"`, and a visually distinct (secondary/danger) style
- [ ] T006 [P] [US1] Remove the `LogoutCommand` binding `<Button>` and its parent two-column `<Grid>` from `src/LoopMeet.App/Features/Groups/Views/GroupsListPage.xaml`; replace with a plain `<Label Text="Groups" .../>` header
- [ ] T007 [P] [US1] Remove `LogoutAsync()` and `LogoutCommand` from `src/LoopMeet.App/Features/Groups/ViewModels/GroupsListViewModel.cs`; remove `IAuthService` constructor parameter only if no other command in this ViewModel uses it
- [ ] T008 [US1] Write unit test `LogoutCommand_SignsOutAndNavigatesAndIsNotOnGroupsViewModel` in `tests/LoopMeet.App.Tests/Features/Profile/ProfileViewModelTests.cs` verifying: (a) `LogoutCommand` exists on `ProfileViewModel` and invokes `_authService.SignOutAsync()`; (b) `GroupsListViewModel` no longer exposes a `LogoutCommand` property

**Checkpoint**: Profile page has a working Logout button with icon. Groups page has no Logout button. Unit test passes. US1 independently verified.

---

## Phase 3: User Story 2 — Floating "+" FAB on Groups Page (Priority: P2)

**Goal**: Replace the "Create Group" text button with a circular floating action button fixed at the bottom-right of the Groups page, always visible regardless of scroll position.

**Independent Test**: Open Groups page with a scrollable list; confirm circular "+" FAB is visible at bottom-right; scroll to the bottom; confirm FAB remains fixed and accessible; tap FAB and confirm Create Group flow opens.

**Prerequisite**: None (no new image assets required; can run in parallel with Phase 2 after T006/T007 touch GroupsListPage.xaml — coordinate to avoid merge conflicts if working simultaneously).

### Implementation for User Story 2

- [ ] T009 [US2] Restructure the root layout of `src/LoopMeet.App/Features/Groups/Views/GroupsListPage.xaml`: wrap the existing content (header + scroll area) in a single-row, single-column `<Grid>` so the FAB can float above it
- [ ] T010 [US2] Add bottom margin/padding (`Margin="0,0,0,80"` or equivalent) to the `CollectionView` or `ScrollView` in `src/LoopMeet.App/Features/Groups/Views/GroupsListPage.xaml` so the last list item is never permanently obscured by the FAB
- [ ] T011 [US2] Add the circular FAB `<Button>` as the last child of the root `<Grid>` in `src/LoopMeet.App/Features/Groups/Views/GroupsListPage.xaml`: `VerticalOptions="End"`, `HorizontalOptions="End"`, `Margin="0,0,16,32"`, `WidthRequest="56"`, `HeightRequest="56"`, `CornerRadius="28"`, `Text="+"`, `FontSize="28"`, `FontAttributes="Bold"`, `TextColor="White"`, `BackgroundColor="{AppThemeBinding Light={StaticResource Primary}, Dark={StaticResource PrimaryDark}}"`, `Command="{Binding CreateGroupCommand}"`
- [ ] T012 [US2] Remove the original `<Button Text="Create Group" .../>` from the page body in `src/LoopMeet.App/Features/Groups/Views/GroupsListPage.xaml`

**Checkpoint**: Groups page shows circular "+" FAB at bottom-right. No "Create Group" text button. FAB remains visible while scrolling. Tapping FAB opens Create Group flow. US2 independently verified.

---

## Phase 4: User Story 3 — Desktop Accept & Decline on Pending Invitations (Priority: P2)

**Goal**: Add the ✓ checkmark icon to the existing desktop-only Accept button and add a new desktop-only Decline button (🗑 icon) next to it, so desktop users can both accept and decline invitations without swiping.

**Independent Test**: On desktop idiom, open Pending Invitations; confirm each row shows "✓ Accept" and "🗑 Decline" buttons side by side; tap Accept on one invitation (confirm accepted); tap Decline on another (confirm declined). On mobile, confirm neither button is visible.

**Prerequisite**: None (independent file, no image assets needed). Can run in parallel with Phases 2 and 3.

### Implementation for User Story 3

- [ ] T013 [US3] In `src/LoopMeet.App/Features/Invitations/Views/PendingInvitationsPage.xaml`, wrap the existing desktop Accept `<Button>` in a `<HorizontalStackLayout Grid.Column="1" Spacing="8" IsVisible="{OnIdiom Phone=False, Tablet=False, Desktop=True, TV=True}">` container; move the `IsVisible` idiom binding from the button to the container
- [ ] T014 [US3] Update the Accept button `Text` to `"✓ Accept"` in `src/LoopMeet.App/Features/Invitations/Views/PendingInvitationsPage.xaml` (remove the now-redundant `IsVisible` from the button itself since the container controls visibility)
- [ ] T015 [US3] Add a Decline `<Button>` inside the `HorizontalStackLayout` in `src/LoopMeet.App/Features/Invitations/Views/PendingInvitationsPage.xaml` with `Text="🗑 Decline"`, `BackgroundColor="#DC2626"`, `TextColor="White"`, `Command="{Binding BindingContext.DeclineInvitationCommand, Source={x:Reference PendingInvitationsPageRoot}}"`, `CommandParameter="{Binding .}"`

**Checkpoint**: Desktop shows both "✓ Accept" and "🗑 Decline" buttons per invitation row. Mobile shows only swipe actions. Tapping each button fires the correct command. US3 independently verified.

---

## Phase 5: User Story 4 — Save Icon on Profile Page (Priority: P3)

**Goal**: Add a save icon to the existing Save button on the Profile page.

**Independent Test**: Open the Profile page; confirm the Save button displays the save icon alongside its "Save" label; tap it and confirm profile saves with no behavioural change.

**Prerequisite**: T002 (ic_save asset) must be complete.

### Implementation for User Story 4

- [ ] T016 [US4] Add `ImageSource="ic_save_fallback.png"` and `ContentLayout="Left,8"` to the existing Save `<Button>` in `src/LoopMeet.App/Features/Profile/Views/ProfilePage.xaml`

**Checkpoint**: Profile Save button shows save icon. Save functionality unchanged. US4 independently verified.

---

## Phase 6: User Story 5 — Invitation Icons on Group Detail & Send Invitation (Priority: P3)

**Goal**: Add appropriate invitation icons to the "Invite Member" button on the Group Detail page and the "Send Invite" button on the Send Invitation page.

**Independent Test**: Open Group Detail page as an owner; confirm the Invite Member button shows the invite icon. Navigate to the Send Invitation page; confirm the Send Invite button shows the invite icon. Both actions complete without regression.

**Prerequisite**: T003 (ic_invite asset) must be complete.

### Implementation for User Story 5

- [ ] T017 [P] [US5] Add `ImageSource="ic_invite_fallback.png"` and `ContentLayout="Left,8"` to the Invite Member `<Button>` in `src/LoopMeet.App/Features/Groups/Views/GroupDetailPage.xaml`
- [ ] T018 [P] [US5] Add `ImageSource="ic_invite_fallback.png"` and `ContentLayout="Left,8"` to the Send Invite `<Button>` in `src/LoopMeet.App/Features/Invitations/Views/InviteMemberPage.xaml`

**Checkpoint**: Both invite-related buttons show the ic_invite icon. No behavioural regression on either page. US5 independently verified.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: End-to-end validation across all platforms and final quality checks.

- [ ] T019 Run the app on all four target platforms (iOS, Android, macOS, Windows) and verify each item in the acceptance checklist in `specs/004-ui-polish/quickstart.md` — specifically confirm all icons render correctly in both light mode and dark mode on each platform
- [ ] T020 [P] Verify the Groups page FAB does not obscure any list item when scrolled to the bottom on a device with a small screen; adjust bottom margin if needed in `src/LoopMeet.App/Features/Groups/Views/GroupsListPage.xaml`
- [ ] T021 [P] Run `dotnet test tests/LoopMeet.App.Tests/` and confirm all tests pass including T008's new unit test
- [ ] T022 Update `specs/004-ui-polish/checklists/requirements.md` to mark all feature-readiness items as verified

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — all three icon tasks start immediately and run in parallel
- **Phase 2 (US1 P1)**: T004/T006/T007 can start immediately (no icon assets needed for ViewModel changes); T005 requires T001 (ic_logout)
- **Phase 3 (US2 P2)**: Can start immediately in parallel with Phase 2 — no assets, different file (GroupsListPage.xaml — coordinate with US1 T006 if working simultaneously)
- **Phase 4 (US3 P2)**: Can start immediately in parallel with Phases 2 and 3 — independent file
- **Phase 5 (US4 P3)**: Requires T002 (ic_save); otherwise independent
- **Phase 6 (US5 P3)**: Requires T003 (ic_invite); T017 and T018 are parallel (different files)
- **Phase 7 (Polish)**: Requires all story phases complete

### User Story Dependencies

- **US1 (P1)**: No story dependencies. T004 → T005 sequential; T006 and T007 parallel with T004.
- **US2 (P2)**: No story dependencies. T009 → T010 → T011 → T012 sequential (same file).
- **US3 (P2)**: No story dependencies. T013 → T014 → T015 sequential (same file).
- **US4 (P3)**: Depends only on T002 (icon asset). Single task.
- **US5 (P3)**: Depends only on T003 (icon asset). T017 and T018 are parallel (different files).

### Within Each User Story

- US1: ProfileViewModel changes (T004) must precede ProfilePage XAML binding (T005)
- US2: Layout restructure (T009) must precede FAB addition (T011) and margin addition (T010); old button removal (T012) last
- US3: Container creation (T013) must precede button updates (T014, T015)
- US4: Icon asset (T002) must be present before XAML references it (T016)
- US5: Icon asset (T003) must be present before XAML references it (T017, T018)

---

## Parallel Opportunities

### Immediate (no prerequisites at all)

```text
T001 (ic_logout asset)          ← in parallel
T002 (ic_save asset)            ← in parallel
T003 (ic_invite asset)          ← in parallel
T004 (ProfileViewModel logout)  ← in parallel
T006 (GroupsList remove button) ← in parallel
T007 (GroupsList remove cmd)    ← in parallel
T009 (Groups FAB layout)        ← in parallel
T013 (Invitations container)    ← in parallel
```

### After T001 completes

```text
T005 (ProfilePage logout button with icon)
```

### After T002 completes

```text
T016 (ProfilePage save icon)
```

### After T003 completes

```text
T017 (GroupDetail invite icon)  ← in parallel
T018 (InviteMember invite icon) ← in parallel
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete T001 (ic_logout asset)
2. Complete T004 → T005 (add Logout to Profile)
3. Complete T006 + T007 in parallel (remove from Groups)
4. Complete T008 (unit test)
5. **STOP and VALIDATE**: Profile has Logout with icon; Groups does not. Ship as MVP if ready.

### Incremental Delivery

1. Phase 1 + Phase 2 → US1 complete (Logout on Profile) → validate + deploy
2. Phase 3 + Phase 4 in parallel → US2 (FAB) + US3 (desktop Accept/Decline) → validate + deploy
3. Phase 5 + Phase 6 in parallel → US4 (save icon) + US5 (invite icons) → validate + deploy
4. Phase 7 → full cross-platform validation

### Parallel Team Strategy

With two developers after icon assets are ready (T001–T003):

- **Developer A**: US1 (Profile/Groups ViewModel + XAML) → US4 (save icon, same Profile page)
- **Developer B**: US2 (Groups FAB, different part of Groups page) → US3 (Invitations page) → US5 (Group Detail + Invite pages)

---

## Notes

- [P] tasks = different files, no unresolved dependencies — safe to run concurrently
- GroupsListPage.xaml is touched by both US1 (T006: remove logout header) and US2 (T009–T012: FAB). If working in parallel, coordinate to avoid conflicts — or complete US1's T006 first, then start US2.
- ProfilePage.xaml is touched by both US1 (T005: logout button) and US4 (T016: save icon). Sequence these or combine into a single edit.
- Unicode icons (✓, 🗑) for US3 need no image assets — tested cross-platform by the existing swipe actions.
- MAUI 10 automatically includes files placed in `Resources/Images/` as `MauiImage` — no `.csproj` edits needed for new icon assets.
- Run `dotnet test` after T008 and again during Phase 7 (T021) to catch any regressions.
