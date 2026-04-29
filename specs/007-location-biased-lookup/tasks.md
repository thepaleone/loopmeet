# Tasks: Location-Biased Lookup

**Input**: Design documents from `/specs/007-location-biased-lookup/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Include automated tests because the constitution requires tests for new behavior and regression coverage.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Align extension points and shared contracts before feature work

- [X] T001 Add optional location-bias query parameters to app places API contract in `src/LoopMeet.App/Services/PlacesApi.cs`
- [X] T002 Add location-bias query parameters to API endpoint signature in `src/LoopMeet.Api/Endpoints/PlacesEndpoints.cs`
- [X] T003 [P] Add shared test doubles for places autocomplete request capture in `tests/LoopMeet.App.Tests/TestDoubles/FakePlacesApi.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core location-bias plumbing used by all user stories

**CRITICAL**: No user story work can begin until this phase is complete

- [X] T004 Implement optional `latitude`/`longitude`/`radiusMeters` pass-through in `src/LoopMeet.App/Services/PlacesApi.cs`
- [X] T005 Implement optional location-bias request body shaping for Google Places in `src/LoopMeet.Api/Services/Places/PlacesProxyService.cs`
- [X] T006 [P] Add API tests for query-only compatibility and optional bias parameters in `tests/LoopMeet.Api.Tests/Endpoints/PlacesEndpointsTests.cs`
- [X] T007 Add resilient fallback behavior when bias coordinates are missing/invalid in `src/LoopMeet.Api/Services/Places/PlacesProxyService.cs`

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Faster Nearby Address Suggestions (Priority: P1) MVP

**Goal**: Prioritize nearby suggestions during typing when location access is available

**Independent Test**: On create/edit flows with location allowed, short ambiguous queries return locally relevant suggestions near the top and remain selectable.

### Tests for User Story 1

- [X] T008 [P] [US1] Add create-flow autocomplete bias request test in `tests/LoopMeet.App.Tests/Features/Meetups/CreateMeetupViewModelTests.cs`
- [X] T009 [P] [US1] Add edit-flow autocomplete bias request test in `tests/LoopMeet.App.Tests/Features/Meetups/EditMeetupViewModelTests.cs`

### Implementation for User Story 1

- [X] T010 [US1] Implement location acquisition and bias-enabled autocomplete in `src/LoopMeet.App/Features/Meetups/ViewModels/CreateMeetupViewModel.cs`
- [X] T011 [US1] Implement location acquisition and bias-enabled autocomplete in `src/LoopMeet.App/Features/Meetups/ViewModels/EditMeetupViewModel.cs`
- [X] T012 [US1] Ensure selected place hydration remains intact with biased suggestions in `src/LoopMeet.App/Features/Meetups/ViewModels/CreateMeetupViewModel.cs`
- [X] T013 [US1] Ensure selected place hydration remains intact with biased suggestions in `src/LoopMeet.App/Features/Meetups/ViewModels/EditMeetupViewModel.cs`

**Checkpoint**: User Story 1 is functional and independently testable

---

## Phase 4: User Story 2 - Permission-Respecting Behavior (Priority: P2)

**Goal**: Request permission per platform rules and gracefully fallback when denied/unavailable

**Independent Test**: First interaction prompts for location access; deny/revoke/unavailable cases continue with non-biased suggestions without repeated disruptive prompts.

### Tests for User Story 2

- [X] T014 [P] [US2] Add permission-denied fallback autocomplete test for create flow in `tests/LoopMeet.App.Tests/Features/Meetups/CreateMeetupViewModelTests.cs`
- [X] T015 [P] [US2] Add permission-denied fallback autocomplete test for edit flow in `tests/LoopMeet.App.Tests/Features/Meetups/EditMeetupViewModelTests.cs`
- [ ] T016 [P] [US2] Add revoked/unavailable location fallback test in `tests/LoopMeet.App.Tests/Features/Meetups/CreateMeetupViewModelTests.cs`

### Implementation for User Story 2

- [X] T017 [US2] Implement permission-state aware search fallback and no-repeat prompt behavior in `src/LoopMeet.App/Features/Meetups/ViewModels/CreateMeetupViewModel.cs`
- [X] T018 [US2] Implement permission-state aware search fallback and no-repeat prompt behavior in `src/LoopMeet.App/Features/Meetups/ViewModels/EditMeetupViewModel.cs`
- [X] T019 [US2] Add user-facing fallback messaging for non-biased mode in `src/LoopMeet.App/Features/Meetups/ViewModels/CreateMeetupViewModel.cs`
- [X] T020 [US2] Add user-facing fallback messaging for non-biased mode in `src/LoopMeet.App/Features/Meetups/ViewModels/EditMeetupViewModel.cs`

**Checkpoint**: User Stories 1 and 2 work independently

---

## Phase 5: User Story 3 - Consistent Create/Edit Experience (Priority: P3)

**Goal**: Keep create and edit lookup behavior consistent for identical query and permission states

**Independent Test**: Same query + same permission state yields equivalent prediction visibility and interaction behavior in both pages.

### Tests for User Story 3

- [X] T021 [P] [US3] Add parity test for create/edit lookup behavior in `tests/LoopMeet.App.Tests/Features/Meetups/MeetupLocationLookupParityTests.cs`

### Implementation for User Story 3

- [X] T022 [US3] Extract shared lookup workflow helper to remove create/edit divergence in `src/LoopMeet.App/Features/Meetups/ViewModels/MeetupLocationLookupBehavior.cs`
- [X] T023 [US3] Wire create view model to shared lookup workflow in `src/LoopMeet.App/Features/Meetups/ViewModels/CreateMeetupViewModel.cs`
- [X] T024 [US3] Wire edit view model to shared lookup workflow in `src/LoopMeet.App/Features/Meetups/ViewModels/EditMeetupViewModel.cs`

**Checkpoint**: All user stories are independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, observability, and cleanup across stories

- [ ] T025 [P] Add autocomplete bias/fallback logging assertions in `tests/LoopMeet.Api.Tests/Endpoints/PlacesEndpointsTests.cs`
- [ ] T026 Add API/service error handling cleanup for autocomplete failures in `src/LoopMeet.Api/Services/Places/PlacesProxyService.cs`
- [ ] T027 [P] Update feature quick validation notes in `specs/007-location-biased-lookup/quickstart.md`
- [X] T028 Run full automated regression for affected suites in `tests/LoopMeet.App.Tests/` and `tests/LoopMeet.Api.Tests/`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: starts immediately
- **Phase 2 (Foundational)**: depends on Phase 1 and blocks all stories
- **Phase 3 (US1)**: depends on Phase 2
- **Phase 4 (US2)**: depends on Phase 2 and can run in parallel with US1 after foundation, but recommended after US1 for MVP-first delivery
- **Phase 5 (US3)**: depends on Phase 2 and should run after initial US1/US2 behavior exists for parity checks
- **Phase 6 (Polish)**: depends on completion of desired user stories

### User Story Dependencies

- **US1 (P1)**: independent after foundation
- **US2 (P2)**: independent after foundation; builds on same lookup entry points
- **US3 (P3)**: depends on both create/edit behavior being implemented to verify consistency

### Within Each User Story

- Write tests first, observe failure, then implement
- Update create/edit behavior in parallel where file boundaries permit
- Complete independent validation before moving on

### Parallel Opportunities

- T003 and T006 can run in parallel with other non-overlapping setup/foundation tasks
- T008 and T009 can run in parallel
- T014, T015, and T016 can run in parallel
- T022 can proceed while parity test scaffolding (T021) is prepared
- T025 and T027 can run in parallel during polish

---

## Parallel Example: User Story 1

```bash
Task: "Add create-flow autocomplete bias request test in tests/LoopMeet.App.Tests/Features/Meetups/CreateMeetupViewModelTests.cs"
Task: "Add edit-flow autocomplete bias request test in tests/LoopMeet.App.Tests/Features/Meetups/EditMeetupViewModelTests.cs"
```

```bash
Task: "Implement location acquisition and bias-enabled autocomplete in src/LoopMeet.App/Features/Meetups/ViewModels/CreateMeetupViewModel.cs"
Task: "Implement location acquisition and bias-enabled autocomplete in src/LoopMeet.App/Features/Meetups/ViewModels/EditMeetupViewModel.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2
2. Complete Phase 3 (US1)
3. Validate US1 independently using create/edit biased lookup scenarios
4. Demo/release MVP increment

### Incremental Delivery

1. Foundation complete
2. Deliver US1 (nearby relevance)
3. Deliver US2 (permission-respecting fallback)
4. Deliver US3 (cross-page parity)
5. Execute polish and full regression

### Parallel Team Strategy

1. One engineer handles API contract/service tasks (T002, T005, T006, T007)
2. One engineer handles create-flow app tasks
3. One engineer handles edit-flow app tasks
4. Converge on parity and polish tasks

---

## Notes

- [P] tasks indicate file-level independence
- [US#] labels maintain story traceability
- Keep behavior aligned with existing components and native Google capability usage
- Use checkpoint commits after meaningful milestones during implementation
