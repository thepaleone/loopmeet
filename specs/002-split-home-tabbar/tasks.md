---

description: "Task list for Home Tab Navigation Split implementation"

---

# Tasks: Home Tab Navigation Split

**Input**: Design documents from `/specs/002-split-home-tabbar/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/, quickstart.md

**Tests**: Included for app-side behavior because the constitution and plan require automated coverage for new behavior, plus manual MAUI smoke validation.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add minimal shared scaffolding for app-side tests and tab assets used across stories

- [X] T001 Create app-focused test project scaffold in `tests/LoopMeet.App.Tests/LoopMeet.App.Tests.csproj`
- [X] T002 [P] Add test project global usings in `tests/LoopMeet.App.Tests/GlobalUsings.cs`
- [X] T003 [P] Add API test doubles for app view-model tests in `tests/LoopMeet.App.Tests/TestDoubles/FakeGroupsApi.cs` and `tests/LoopMeet.App.Tests/TestDoubles/FakeInvitationsApi.cs`
- [X] T004 [P] Add static emoji-style tab icon assets in `src/LoopMeet.App/Resources/Images/tab_home.svg`, `src/LoopMeet.App/Resources/Images/tab_groups.svg`, and `src/LoopMeet.App/Resources/Images/tab_invitations.svg`
- [X] T005 Add `tests/LoopMeet.App.Tests` to `LoopMeet.slnx`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Shared navigation scaffolding that all user stories depend on

**⚠️ CRITICAL**: No user story work should begin until this phase is complete

- [X] T006 Create shared signed-in tab metadata/route constants in `src/LoopMeet.App/Features/Home/Models/SignedInTabs.cs`
- [X] T007 [P] Scaffold Home tab page shell in `src/LoopMeet.App/Features/Home/Views/HomePage.xaml` and `src/LoopMeet.App/Features/Home/Views/HomePage.xaml.cs`
- [X] T008 [P] Scaffold Pending Invitations tab page shell in `src/LoopMeet.App/Features/Invitations/Views/PendingInvitationsPage.xaml` and `src/LoopMeet.App/Features/Invitations/Views/PendingInvitationsPage.xaml.cs`
- [X] T009 [P] Scaffold tab view models in `src/LoopMeet.App/Features/Home/ViewModels/HomeViewModel.cs` and `src/LoopMeet.App/Features/Invitations/ViewModels/PendingInvitationsViewModel.cs`
- [X] T010 Update signed-in tab bar structure (title + icon for each tab) in `src/LoopMeet.App/AppShell.xaml`
- [X] T011 Update route registration and signed-in shell navigation hooks in `src/LoopMeet.App/AppShell.xaml.cs`
- [X] T012 Register Home and Pending Invitations tab pages/view models in `src/LoopMeet.App/MauiProgram.cs`

**Checkpoint**: Tab bar scaffolding exists and the app can compile with placeholder pages for all three tabs

---

## Phase 3: User Story 1 - Open New Home Screen (Priority: P1) 🎯 MVP

**Goal**: Signed-in users land on a new Home tab placeholder screen, with tab bar navigation available for Home/Groups/Invitations.

**Independent Test**: Sign in (or restore a session) and confirm the app lands on `Home`, the Home tab shows placeholder content, and the tab bar displays icon + text for all tabs.

### Tests for User Story 1

- [X] T013 [P] [US1] Add home placeholder view-model tests in `tests/LoopMeet.App.Tests/Features/Home/HomeViewModelTests.cs`

### Implementation for User Story 1

- [X] T014 [US1] Implement Home placeholder state/content in `src/LoopMeet.App/Features/Home/ViewModels/HomeViewModel.cs`
- [X] T015 [US1] Build Home placeholder UI layout and copy in `src/LoopMeet.App/Features/Home/Views/HomePage.xaml` and `src/LoopMeet.App/Features/Home/Views/HomePage.xaml.cs`
- [X] T016 [US1] Route post-auth and session-restore landing to `//home` in `src/LoopMeet.App/Features/Auth/AuthCoordinator.cs`, `src/LoopMeet.App/Features/Auth/ViewModels/LoginViewModel.cs`, `src/LoopMeet.App/Features/Auth/ViewModels/CreateAccountViewModel.cs`, and `src/LoopMeet.App/AppShell.xaml.cs`
- [X] T017 [US1] Align Home tab default selection and tab metadata with `specs/002-split-home-tabbar/contracts/tabbar-navigation.yaml` in `src/LoopMeet.App/AppShell.xaml`

**Checkpoint**: User Story 1 is functional and independently testable

---

## Phase 4: User Story 2 - View Groups in a Dedicated Screen (Priority: P2)

**Goal**: Groups are shown on a dedicated tab without pending invitations mixed into the screen, while preserving existing group actions.

**Independent Test**: Open the `Groups` tab and verify only owned/member groups appear, the groups empty state is groups-specific, and selecting a group still opens group detail.

### Tests for User Story 2

- [ ] T018 [P] [US2] Add groups-tab state regression tests in `tests/LoopMeet.App.Tests/Features/Groups/GroupsListViewModelTests.cs`

### Implementation for User Story 2

- [X] T019 [US2] Refactor groups-only loading state and remove pending invitation handling from `src/LoopMeet.App/Features/Groups/ViewModels/GroupsListViewModel.cs`
- [X] T020 [US2] Remove pending invitations UI section and keep groups-only empty state in `src/LoopMeet.App/Features/Groups/Views/GroupsListPage.xaml`
- [X] T021 [US2] Remove invitation shimmer references and preserve groups load behavior in `src/LoopMeet.App/Features/Groups/Views/GroupsListPage.xaml.cs`
- [X] T022 [US2] Add/adjust groups-tab logging and regression-safe group actions in `src/LoopMeet.App/Features/Groups/ViewModels/GroupsListViewModel.cs`

**Checkpoint**: User Story 2 is functional and independently testable

---

## Phase 5: User Story 3 - Manage Pending Invitations in a Dedicated Screen (Priority: P3)

**Goal**: Pending invitations are available on a dedicated `Invitations` tab with existing invitation detail and accept/decline actions preserved.

**Independent Test**: Open the `Invitations` tab, verify pending invitations load there, perform accept/decline, and confirm the completed invitation is removed from the pending list.

### Tests for User Story 3

- [ ] T023 [P] [US3] Add pending invitations view-model tests for load and accept/decline refresh in `tests/LoopMeet.App.Tests/Features/Invitations/PendingInvitationsViewModelTests.cs`

### Implementation for User Story 3

- [X] T024 [US3] Implement pending invitations loading/actions/empty state in `src/LoopMeet.App/Features/Invitations/ViewModels/PendingInvitationsViewModel.cs`
- [X] T025 [US3] Build pending invitations tab UI (list, actions, loading state, empty state) in `src/LoopMeet.App/Features/Invitations/Views/PendingInvitationsPage.xaml`
- [X] T026 [US3] Wire page binding, load lifecycle, and shimmer behavior in `src/LoopMeet.App/Features/Invitations/Views/PendingInvitationsPage.xaml.cs`
- [X] T027 [US3] Preserve invitation detail navigation and close behavior from the Invitations tab in `src/LoopMeet.App/Features/Invitations/ViewModels/InvitationDetailViewModel.cs` and `src/LoopMeet.App/Features/Invitations/ViewModels/PendingInvitationsViewModel.cs`
- [X] T028 [US3] Ensure invitations tab uses existing invitation contracts (`GET /invitations`, accept, decline) in `src/LoopMeet.App/Services/InvitationsApi.cs` and `src/LoopMeet.App/Features/Invitations/ViewModels/PendingInvitationsViewModel.cs`

**Checkpoint**: User Story 3 is functional and independently testable

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final UX, reliability, and documentation validation across all stories

- [ ] T029 [P] Run the signed-in tab manual smoke checklist and update verification notes in `specs/002-split-home-tabbar/quickstart.md`
- [ ] T030 [P] Record UX review outcomes for icon-over-title tab rendering and empty states in `specs/002-split-home-tabbar/plan.md`
- [X] T031 Add cross-tab reliability/error-handling polish in `src/LoopMeet.App/Features/Groups/ViewModels/GroupsListViewModel.cs` and `src/LoopMeet.App/Features/Invitations/ViewModels/PendingInvitationsViewModel.cs`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies - can start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 - blocks all user stories
- **Phase 3 (US1)**: Depends on Phase 2
- **Phase 4 (US2)**: Depends on Phase 2
- **Phase 5 (US3)**: Depends on Phase 2
- **Phase 6 (Polish)**: Depends on all targeted user stories being complete

### User Story Dependency Graph

```text
Phase 1 Setup -> Phase 2 Foundational
Phase 2 Foundational -> US1 (P1: Home placeholder + default landing)
Phase 2 Foundational -> US2 (P2: Groups-only tab screen)
Phase 2 Foundational -> US3 (P3: Invitations-only tab screen)
US1 recommended first for MVP demo
US2 and US3 can proceed in parallel after Foundational (and merge onto the shared tab shell)
Polish -> after US1/US2/US3 selected scope is complete
```

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational; no dependency on US2 or US3
- **US2 (P2)**: Can start after Foundational; does not require US1 content beyond shared tab shell scaffolding
- **US3 (P3)**: Can start after Foundational; does not require US2 refactor beyond shared tab shell scaffolding

### Within Each User Story

- Complete tests for the story before or alongside implementation (per plan/constitution expectations)
- Implement view model behavior before final UI wiring where possible
- Finish independent validation checklist before moving to the next story

### Parallel Opportunities

- Phase 1 tasks `T002`-`T004` can run in parallel after `T001`
- Phase 2 scaffolding tasks `T007`-`T009` can run in parallel after `T006`
- After Phase 2, US1/US2/US3 can be implemented in parallel by separate contributors
- Story test tasks `T013`, `T018`, and `T023` can be built in parallel with story scaffolding reviews

---

## Parallel Example: User Story 1

```bash
Task: "Add home placeholder view-model tests in tests/LoopMeet.App.Tests/Features/Home/HomeViewModelTests.cs"
Task: "Implement Home placeholder state/content in src/LoopMeet.App/Features/Home/ViewModels/HomeViewModel.cs"
```

---

## Parallel Example: User Story 2

```bash
Task: "Add groups-tab state regression tests in tests/LoopMeet.App.Tests/Features/Groups/GroupsListViewModelTests.cs"
Task: "Remove pending invitations UI section and keep groups-only empty state in src/LoopMeet.App/Features/Groups/Views/GroupsListPage.xaml"
```

---

## Parallel Example: User Story 3

```bash
Task: "Add pending invitations view-model tests for load and accept/decline refresh in tests/LoopMeet.App.Tests/Features/Invitations/PendingInvitationsViewModelTests.cs"
Task: "Build pending invitations tab UI (list, actions, loading state, empty state) in src/LoopMeet.App/Features/Invitations/Views/PendingInvitationsPage.xaml"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational tab shell scaffolding
3. Complete Phase 3: User Story 1 (Home placeholder + default signed-in landing)
4. Validate the Home tab flow independently on a signed-in session
5. Demo/review before continuing

### Incremental Delivery

1. Setup + Foundational tab shell
2. Add US1 (Home tab placeholder) -> validate
3. Add US2 (Groups-only tab) -> validate
4. Add US3 (Invitations tab) -> validate
5. Run Polish phase quickstart/UX checks

### Parallel Team Strategy

1. One developer completes Setup + Foundational
2. Then split stories by owner:
   - Developer A: US1
   - Developer B: US2
   - Developer C: US3
3. Merge after each story passes its independent test

---

## Notes

- All backend endpoints are reused; no API/server implementation tasks are required for this feature
- `specs/002-split-home-tabbar/contracts/home-navigation-api.yaml` maps the reused endpoints to US2/US3 behavior
- `specs/002-split-home-tabbar/contracts/tabbar-navigation.yaml` maps tab IDs/routes/icon-title rules to US1 and foundational shell tasks
- Prefer emoji-style static tab icons; do not implement animated GIF tab icons unless platform behavior is proven reliable
