# Tasks: Meetup Interaction Improvements

**Input**: Design documents from `/specs/011-meetup-interactions/`
**Prerequisites**: plan.md (loaded), spec.md (3 user stories: P1–P3, 21 FRs), research.md (facts F1–F10, decisions D1–D9), data-model.md (read-model fields, INV-1..5, screen states), contracts/meetup-read-contracts.md (§1–§8), quickstart.md (27-row matrix)

**Tests**: Included — the Meetloop Constitution (Principle II) makes tests a required deliverable. Coverage is placed at the layer that owns the behavior: Api endpoint tests for the read-model resolution, a pure unit test for the FR-011 fallback, source-inspection tests for XAML/wiring (established repo pattern), and the quickstart device matrix for keyboard-overlap and native map launch.

**Organization**: Tasks are grouped by user story. **US1 (save icon) has no backend dependency and can ship first, in parallel with everything else.** Phase 2 delivers the shared read model that US2 and US3 both need.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1–US3 per spec.md. Setup/Foundational/Polish phases have no story label.

## Path Conventions

- Backend: `src/LoopMeet.Core/`, `src/LoopMeet.Infrastructure/`, `src/LoopMeet.Api/`
- Client: `src/LoopMeet.App/`
- Tests: `tests/LoopMeet.Api.Tests/`, `tests/LoopMeet.App.Tests/`

---

## Phase 1: Setup

**Purpose**: Establish a known-good baseline so any later failure is attributable, and confirm the one asset this feature reuses actually ships.

- [X] T001 Run `dotnet test LoopMeet.slnx -c Debug -p:SkipMaciOSTargets=true` from the repo root and record the passing count as the baseline (expected: 126 passing, 0 failing) in this file's Notes section.
- [X] T002 Confirm the reused save asset ships: verify `src/LoopMeet.App/Resources/Images/ic_save.png` exists and is covered by the `<MauiImage Include="Resources\Images\*" />` glob at `src/LoopMeet.App/LoopMeet.App.csproj:107`, so an icon-only button cannot render blank.

---

## Phase 2: Foundational — Shared Meetup Read Model

**Purpose**: The server-side denormalization (organizer display name, group name, group owner id) plus the client model members that US2 and US3 both consume.

**⚠️ Blocks US2 and US3 only — US1 (Phase 3) is fully independent and may proceed in parallel.**

**⚠️ Sequencing note**: T004 changes a repository signature that `MeetupQueryService` currently consumes, so the tree does not compile between T004 and T007. Land T004 → T005 → T006 → T007 as one coherent change rather than committing between them.

### Backend

- [X] T003 [P] Add `Task<IReadOnlyList<Membership>> ListMembersByGroupsAsync(IReadOnlyList<Guid> groupIds, CancellationToken cancellationToken = default)` to `src/LoopMeet.Core/Interfaces/IMembershipRepository.cs` and implement it in `src/LoopMeet.Infrastructure/Repositories/MembershipRepository.cs` as a single Postgrest query filtered on the group-id set, short-circuiting to an empty list for empty input (contract §3).
- [X] T004 Change `ListUpcomingByUserAsync` in `src/LoopMeet.Core/Interfaces/IMeetupRepository.cs` from `Task<IReadOnlyList<(Meetup Meetup, string GroupName)>>` to `Task<IReadOnlyList<Meetup>>`, and delete the now-redundant fetch-all-groups/`groupLookup` block at `src/LoopMeet.Infrastructure/Repositories/MeetupRepository.cs:75-85`, returning mapped meetups directly (research D5).
- [X] T005 Update the Api test fakes for T003/T004: `ListUpcomingByUserAsync` in `tests/LoopMeet.Api.Tests/Infrastructure/InMemoryMeetupRepository.cs` returns meetups only (drop its group-name resolution), and add `ListMembersByGroupsAsync` to `tests/LoopMeet.Api.Tests/Infrastructure/InMemoryMembershipRepository.cs` mirroring production semantics (ids with no rows simply absent).
- [X] T006 In `src/LoopMeet.Api/Contracts/MeetupContracts.cs`: add `MeetupListItemResponse` with all existing meetup fields plus `GroupName`, `CreatedByDisplayName` (both `string` defaulting to `string.Empty`) and `GroupOwnerUserId` (`Guid`); point both `MeetupsResponse.Meetups` and `UpcomingMeetupsResponse.Meetups` at it; delete `UpcomingMeetupResponse`. Leave `MeetupResponse`, `CreateMeetupRequest`, and `UpdateMeetupRequest` untouched — `MeetupResponse` stays the write echo (contract §1, research D6).
- [X] T007 Extend `src/LoopMeet.Api/Services/Meetups/MeetupQueryService.cs`: inject `IGroupRepository`, `IMembershipRepository`, `IUserRepository`; add one private resolution helper implementing contract §2's algorithm (three lookups per request — groups by distinct id, memberships by distinct group id, users by membership-filtered creator id — no per-row query); project both `GetGroupMeetupsAsync` and `GetUpcomingForUserAsync` through it into `MeetupListItemResponse`. A creator who is not a current member of that meetup's group MUST resolve to `string.Empty` (INV-1); missing group/membership/profile rows MUST yield empty values, never an exception (INV-2). Keep both cache keys and the 30s TTL unchanged. Extend both log lines with `organizersResolved` / `organizersUnresolved` counts (Constitution VII).
- [X] T008 Extend `tests/LoopMeet.Api.Tests/Endpoints/MeetupsEndpointsTests.cs`: seed `User` rows with display names (copy the pattern at `tests/LoopMeet.Api.Tests/Endpoints/GroupsEndpointsTests.cs:66-83` — meetup tests currently seed none, so creator ids point at nonexistent users). Add coverage for: `CreatedByDisplayName` populated on the group-scoped list for a current member; `CreatedByDisplayName` empty when the creator is not in the group's membership (departed-creator case, INV-1); `GroupName` present on the group-scoped list (previously absent); `GroupOwnerUserId` correct on both endpoints. Update `UpcomingReturnsMeetupsAcrossAllGroupsWithGroupName` for the new element type — its `GroupName` assertion must survive unchanged. Add one source-inspection assertion against `src/LoopMeet.Api/Services/Meetups/MeetupQueryService.cs` that both log lines contain `organizersResolved` and `organizersUnresolved`, so the Constitution VII commitment is a standing check rather than a one-time manual look (T033).

### Client model

- [X] T009 [P] Create `src/LoopMeet.App/Features/Meetups/MeetupOrganizerText.cs`: `public static class MeetupOrganizerText` with `public const string UnknownOrganizer = "A group member"` and `Format(string? displayName)` returning the constant for null/empty/whitespace and the name otherwise (contract §4, FR-011). Must have no `Microsoft.Maui.*` dependency so the test project can compile it directly.
- [X] T010 [P] Create `tests/LoopMeet.App.Tests/Features/Meetups/MeetupOrganizerTextTests.cs` covering: a real name passes through unchanged; null, empty string, and whitespace-only each yield `"A group member"`. Add a `<Compile Include="..\..\src\LoopMeet.App\Features\Meetups\MeetupOrganizerText.cs" Link="Included\MeetupOrganizerText.cs" />` entry to the existing MAUI-free include list in `tests/LoopMeet.App.Tests/LoopMeet.App.Tests.csproj`. (May be authored before T009 to see it fail first.)
- [X] T011 In `src/LoopMeet.App/Features/Meetups/Models/MeetupModels.cs`, add to `MeetupSummary`: `CreatedByDisplayName` (string, default empty), `GroupOwnerUserId` (Guid), computed `OrganizerDisplay => MeetupOrganizerText.Format(CreatedByDisplayName)`, and computed `CanOpenLocation => Latitude is not null && Longitude is not null` (contract §4). Use `{ get; set; }` to match every existing member of this class — contract §4's snippet shows `init`, but mixing accessors in one DTO is gratuitous inconsistency and both deserialize identically. Leave existing computed members (`HasLocation`, `LocationDisplay`, `DateTimeDisplay`) untouched so cards and the details screen cannot drift.

**Checkpoint**: Both list endpoints return the three new fields; `dotnet test` green; no client UI change yet.

---

## Phase 3: User Story 1 — Save a Meetup Without Fighting the Keyboard (Priority: P1) 🎯 MVP

**Goal**: The save action on both meetup forms becomes an icon in the top-right corner, level with the page title and outside the scrolling region, so the on-screen keyboard can never cover it. Validation, error display, and duplicate-submit protection are unchanged.

**Independent Test**: quickstart rows 1–6 — save with the keyboard up on both forms, unchanged validation message, no bottom button remaining, save row survives the location-search collapse, and the screen reader announces the icon's purpose.

**No dependency on Phase 2** — this slice is XAML-only and may be implemented and shipped first.

### Implementation for User Story 1

- [X] T012 [P] [US1] Restructure `src/LoopMeet.App/Features/Meetups/Views/CreateMeetupPage.xaml`: replace the root `ScrollView` with a two-row `Grid` (`RowDefinitions="Auto,*"`) whose first row is a two-column header (`ColumnDefinitions="*,Auto"`) holding the existing `Label Text="Create Meetup"` and a new icon-only save control using `ic_save.png` bound to the existing `SaveCommand`, and whose second row holds the original `ScrollView`/`VerticalStackLayout` content. The header row MUST sit outside the `ScrollView` (FR-002). Delete the bottom `<Button Text="Create Meetup" Command="{Binding SaveCommand}" />` at line 61 (FR-003). Set `SemanticProperties.Description="Save meetup"` on the icon (FR-005). Do not change the `ErrorMessage`/`HasError` label or the `ShowFormFields` collapse behavior.
- [X] T013 [P] [US1] Apply the identical restructure to `src/LoopMeet.App/Features/Meetups/Views/EditMeetupPage.xaml`: header row with the existing `Label Text="Edit Meetup"` plus the save icon, original content in row 2, delete the bottom `<Button Text="Save Changes" …>` at line 59, `SemanticProperties.Description="Save changes"`.
- [X] T014 [P] [US1] Create `tests/LoopMeet.App.Tests/Features/Meetups/MeetupInteractionSurfaceTests.cs` with source-inspection assertions (pattern: `tests/LoopMeet.App.Tests/Features/Auth/Session/SessionSurfaceTests.cs`) for both form XAML files: contains `ic_save.png` and `SemanticProperties.Description`; does **not** contain `Text="Create Meetup" Command` / `Text="Save Changes"` bottom buttons; the `SaveCommand` binding appears exactly once per file; and the header `Grid` row precedes the `ScrollView` in document order. Also assert against `src/LoopMeet.App/Features/Meetups/ViewModels/CreateMeetupViewModel.cs` and `EditMeetupViewModel.cs` that each `SaveAsync` still contains its `if (IsBusy)` re-entrancy guard — FR-004's duplicate-submit protection is the one preserved behavior with no other check, and a small corner icon is easier to double-tap than the former full-width button.
- [X] T015 [US1] Run `dotnet test LoopMeet.slnx -c Debug -p:SkipMaciOSTargets=true` — new assertions pass and the T001 baseline count is not regressed.
- [ ] T016 [US1] Device validation: quickstart rows **1–6** plus **1a** (rapid double-tap produces exactly one meetup / one update) on Android, and on iPhone if available, including the screen-reader announcement check.

**Checkpoint**: US1 independently shippable — the keyboard-overlap defect is fixed.

---

## Phase 4: User Story 2 — See Everything About a Meetup (Priority: P2)

**Goal**: A read-only meetup details screen showing title, date/time, location (or "TBD"), group, and organizer, reachable by tapping a Home page meetup card, with a map control on both the card and the details screen when the location is openable.

**Independent Test**: quickstart rows 7–15 and 24–26 — open details from Home, all five fields present, organizer resolves (and falls back for a departed creator), "TBD" with no map control when unset, map launch from both card and details, back returns to Home.

**Depends on**: Phase 2 (all five fields and the ownership id come from the extended contract). **Group Detail is deliberately untouched in this phase** — its card still taps straight to the edit form, so owners lose no capability before US3 lands.

### Implementation for User Story 2

- [X] T017 [US2] Create `src/LoopMeet.App/Features/Meetups/ViewModels/MeetupDetailViewModel.cs` per contract §5: `ApplyParameters(Guid groupId, Guid meetupId)`; `[RelayCommand] LoadAsync` calling `MeetupsApi.GetGroupMeetupsAsync(groupId)` and selecting by `meetupId`; bound state `Title`, `DateTimeDisplay`, `LocationDisplay`, `GroupName`, `OrganizerDisplay`, `CanOpenLocation`, `IsLoading`, `IsNotFound`, `HasError`; `[RelayCommand] OpenLocationAsync` using `Map.Default.OpenAsync` guarded by `CanOpenLocation`. Absence of the id sets `IsNotFound` (not an error); a thrown request sets `HasError` (data-model §4). Do **not** add `IsOwner` or an edit command here — those belong to US3.
- [X] T018 [US2] Create `src/LoopMeet.App/Features/Meetups/Views/MeetupDetailPage.xaml` and `.xaml.cs`: read-only presentation of the five fields with the map control visible only when `CanOpenLocation`, plus distinct `IsLoading` / `IsNotFound` ("This meetup is no longer available.") / `HasError` states. Use `[QueryProperty]` for `groupId`/`meetupId` and call `ApplyParameters` + `LoadCommand` from `OnAppearing` so every arrival re-reads (INV-5), following `src/LoopMeet.App/Features/Meetups/Views/EditMeetupPage.xaml.cs:5-53`. Verify every style/color resource key used exists in `Resources/Styles/` before referencing it. No delete action (FR-014), and no custom back handling — FR-013 is satisfied by Shell's default back navigation. No pull-to-refresh: `OnAppearing` re-reading is the specified freshness mechanism.
- [X] T019 [US2] Register the route and DI: add `Routing.RegisterRoute("meetup-detail", typeof(MeetupDetailPage));` to `src/LoopMeet.App/AppShell.xaml.cs` alongside the existing detail routes, and `AddTransient<MeetupDetailViewModel>()` + `AddTransient<MeetupDetailPage>()` to `src/LoopMeet.App/MauiProgram.cs`.
- [X] T020 [US2] Modify the meetup card template in `src/LoopMeet.App/Features/Home/Views/HomePage.xaml` (lines 36-63): add a `TapGestureRecognizer` on the card `Border` bound to a new `OpenMeetupDetailCommand` with `CommandParameter="{Binding .}"`; **remove** the `TapGestureRecognizer` from the location `Label`; add a map control on the location row (text glyph, matching the app's `🗑`/`✓` style — no new image asset) bound to `OpenLocationCommand` and `IsVisible="{Binding CanOpenLocation}"` (FR-019, FR-020, FR-021).
- [X] T021 [US2] In `src/LoopMeet.App/Features/Home/ViewModels/HomeViewModel.cs`: add `[RelayCommand] OpenMeetupDetailAsync(MeetupSummary? meetup)` navigating to `meetup-detail` with `groupId`/`meetupId` (contract §5), and change `OpenLocationAsync`'s guard to use `meetup.CanOpenLocation` instead of its inline `HasLocation && Latitude != null && Longitude != null` check.
- [X] T022 [P] [US2] Extend `tests/LoopMeet.App.Tests/Features/Meetups/MeetupInteractionSurfaceTests.cs` (created by T014 — if US2 lands before US1, create the file here with only these assertions): `HomePage.xaml` binds the card border to `OpenMeetupDetailCommand`, contains a `CanOpenLocation`-gated map control, and its location `Label` no longer contains a `TapGestureRecognizer`; `MeetupDetailPage.xaml` contains bindings for all five fields plus the not-found state; `AppShell.xaml.cs` registers `meetup-detail`; `MeetupDetailViewModel.cs` calls `GetGroupMeetupsAsync` and contains no delete command.
- [X] T023 [US2] Run `dotnet test LoopMeet.slnx -c Debug -p:SkipMaciOSTargets=true` — full suite green.
- [ ] T024 [US2] Device validation: quickstart rows **7–15** plus **24** (offline load), **25** (long values), **26** (place name without coordinates).

**Checkpoint**: US2 independently shippable — Home cards are no longer dead targets and every member can read full meetup information.

---

## Phase 5: User Story 3 — Owners Reach Edit From Details; Members Reach Details (Priority: P3)

**Goal**: Group Detail meetup cards open the details screen for all members, and the details screen shows an owner-only edit control. Swipe-to-delete is untouched.

**Independent Test**: quickstart rows 16–23 — owner taps card → details → pencil → edit form; non-owner taps card → details with no pencil; ownership identical from the Home entry point; swipe-to-delete unchanged for both roles; edited values fresh on return.

**Depends on**: Phase 2 (`GroupOwnerUserId`) and US2 (the details screen). The Group Detail re-route and the pencil MUST land together — re-routing alone would remove owners' only edit path.

### Implementation for User Story 3

- [X] T025 [US3] Extend `src/LoopMeet.App/Features/Meetups/ViewModels/MeetupDetailViewModel.cs`: add `IsOwner`, computed solely as `meetup.GroupOwnerUserId == AuthService.GetCurrentUserId()` — not the entry point, not `CreatedByUserId` (FR-016) — and `[RelayCommand] EditAsync` navigating to the existing `edit-meetup` route with `groupId`/`meetupId`. `IsOwner` MUST be false in the `IsNotFound` and `HasError` states so a missing meetup can never present an edit path (INV-4).
- [X] T026 [US3] Add the edit control to `src/LoopMeet.App/Features/Meetups/Views/MeetupDetailPage.xaml`: a pencil text glyph bound to `EditCommand` with `IsVisible="{Binding IsOwner}"` (FR-015, FR-017, FR-021).
- [X] T027 [US3] Modify the meetup card template in `src/LoopMeet.App/Features/Groups/Views/GroupDetailPage.xaml` (lines 67-130): change the `Border`'s `TapGestureRecognizer` from `EditMeetupCommand` to a new `OpenMeetupDetailCommand`; **remove** the location `Label`'s `TapGestureRecognizer`; add the same `CanOpenLocation`-gated map control as the Home card. Leave the enclosing `SwipeView`, its `IsOwner`-gated delete item, and the desktop-only delete button exactly as they are (FR-018).
- [X] T028 [US3] In `src/LoopMeet.App/Features/Groups/ViewModels/GroupDetailViewModel.cs`: add `[RelayCommand] OpenMeetupDetailAsync(MeetupSummary? meetup)` navigating to `meetup-detail` — this command MUST NOT be owner-gated (FR-008), unlike the retained `EditMeetupCommand` and `DeleteMeetupCommand` guards. Change `OpenLocationAsync`'s guard to `meetup.CanOpenLocation`.
- [X] T029 [P] [US3] Extend `tests/LoopMeet.App.Tests/Features/Meetups/MeetupInteractionSurfaceTests.cs` (created by T014; see T022's note): `MeetupDetailPage.xaml` gates the pencil on `IsOwner`; `GroupDetailPage.xaml` binds the card border to `OpenMeetupDetailCommand` (and no longer to `EditMeetupCommand`), still contains the `SwipeView` delete item gated on `IsOwner`, and its location `Label` has no `TapGestureRecognizer`; `GroupDetailViewModel.cs` guards `EditMeetupAsync`/`DeleteMeetupAsync` on `IsOwner` but does not guard `OpenMeetupDetailAsync`.
- [X] T030 [US3] Run `dotnet test LoopMeet.slnx -c Debug -p:SkipMaciOSTargets=true` — full suite green.
- [ ] T031 [US3] Device validation with two accounts (owner + non-owner member): quickstart rows **16–23**, including row 20 (edited title fresh on return to details) and row 23 (deleted-while-open).

**Checkpoint**: All three stories complete; non-owners have a working tap where they previously had none.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T032 [P] Confirm the location-guard de-duplication is complete: neither `src/LoopMeet.App/Features/Home/ViewModels/HomeViewModel.cs` nor `src/LoopMeet.App/Features/Groups/ViewModels/GroupDetailViewModel.cs` retains an inline `Latitude is not null && Longitude is not null` guard — both defer to `MeetupSummary.CanOpenLocation` (contract §4, Constitution I).
- [X] T033 Verify observability (Constitution VII) against `src/LoopMeet.Api/Services/Meetups/MeetupQueryService.cs`: exercise both list endpoints and confirm its log lines include `organizersResolved` and `organizersUnresolved`, and that the departed-creator case increments the unresolved count rather than failing silently.
- [ ] T034 Device validation: quickstart row **27** — the edit pencil and map glyphs render correctly on both Android and iOS (the reason glyphs were chosen over new image assets).
- [ ] T035 Open a pull request titled "feat(meetups): icon save, meetup details screen, owner-gated edit" summarizing the three stories and the read-model extension (including the `GroupOwnerUserId` addition and its rationale), with the 27-row quickstart matrix as a checklist and explicit confirmation that T015/T023/T030 suites and all device rows passed.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: T001 ∥ T002; no code dependencies.
- **Phase 2 (Foundational)**: T003 ∥ T009 ∥ T010 are independent. T004 → T005 → T006 → T007 must land as one change (the tree does not compile between T004 and T007). T008 requires T007. T011 requires T009.
- **Phase 3 (US1)**: depends on nothing beyond Phase 1. T012 ∥ T013 ∥ T014; then T015; then T016.
- **Phase 4 (US2)**: requires Phase 2 complete (T007 + T011). T017 → T018 → T019; T020 → T021; T022 after T018/T020 **and after T014** (shared test file); T023 after all; T024 last.
- **Phase 5 (US3)**: requires Phase 4 (T017/T018 exist) and Phase 2 (`GroupOwnerUserId`). T025 → T026; T027 → T028; T029 after those **and after T014**; T030; T031.
- **Phase 6 (Polish)**: T032 requires T021 and T028; T033 requires T007; T034 requires T026/T027; T035 last.

### Story-Level Parallelism

- **US1 runs parallel to Phase 2 + US2 + US3** — its production changes touch only the two meetup form XAML files, so there is no source conflict. Two engineers can split cleanly: one takes US1 end-to-end, the other takes Phase 2 → US2 → US3. **One shared file**: T014 creates `MeetupInteractionSurfaceTests.cs`, which T022 and T029 extend. Land T014 first, or let whichever task runs first create the file (noted in T022).
- US2 and US3 are sequential: US3 extends the view model and page that US2 creates.

### Within-Phase Parallelism

- **Phase 2**: T003 ∥ T009 ∥ T010 (distinct files); T004–T007 strictly sequential.
- **Phase 3**: T012 ∥ T013 ∥ T014.
- **Phase 4**: (T017 → T018 → T019) ∥ (T020 → T021), converging at T022.
- **Phase 5**: (T025 → T026) ∥ (T027 → T028), converging at T029.

---

## Implementation Strategy

### MVP first (User Story 1 only)

1. Phase 1 (T001–T002).
2. Phase 3 (T012–T016).
3. Demo: open Create Meetup, type in the title with the keyboard up, tap the corner icon, meetup saved — no scrolling, no keyboard dismissal.

That alone resolves the reported friction and ships without touching the backend, the cards, or navigation.

### Incremental delivery

Phase 2 → US2 gives every group member a working tap on Home and full meetup information (Group Detail still taps straight to edit, so owners lose nothing in the interim). US3 then re-routes Group Detail and adds the owner pencil together. Polish and the PR close it out.

### Risk notes

- **T007 is the highest-risk task** — it is the core resolution logic behind three new fields and both list endpoints. It has the densest automated coverage (T008) and is exercised by quickstart rows 8–10 and 18.
- **T006 changes a response element type** (`MeetupsResponse` / `UpcomingMeetupsResponse`). Client and API ship together and no external consumer exists, so no versioning is needed — but the client model (T011) and the existing upcoming-list assertion (T008) must move in step.
- **T012/T013 change each form's root layout** from `ScrollView` to `Grid`. Watch for regressions in the location-search collapse behavior (`ShowFormFields`) and in keyboard-avoidance on Android — quickstart row 5 targets exactly this.

## Notes

- T001 baseline count: 126 passing, 0 failing (recorded 2026-07-30). After implementation: 159 passing (+33: 5 Api read-model, 5 organizer-text, 23 surface assertions).
- Known limitation carried from research D8: the details screen can only display **upcoming** meetups, because both list endpoints filter to future meetups. A meetup that passes while open resolves to the not-found state. Displaying past meetups would need a get-meetup-by-id endpoint (deliberately deferred).
- Known pre-existing behavior carried from research D9: `home-meetups:{userId}` is never cache-invalidated (30s TTL only), so the Home _list_ can show a stale title for up to 30 seconds after an edit while the details screen is immediately correct. Not a defect introduced here; do not "fix" it as part of this feature.
