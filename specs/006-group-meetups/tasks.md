# Tasks: Group Meetups

**Input**: Design documents from `/specs/006-group-meetups/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Included per Constitution Principle II — every new behavior includes automated tests at the appropriate level.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Database schema, core entity, infrastructure record, and API contracts

- [x] T001 Create Supabase migration for `meetups` table with RLS policies in `supabase/migrations/YYYYMMDDHHMMSS_add_meetups.sql` — include table creation (id, group_id, created_by_user_id, title, scheduled_at, place_name, place_address, latitude, longitude, place_id, created_at, updated_at), FK to groups(id) ON DELETE CASCADE, indexes on (group_id, scheduled_at) and (scheduled_at), enable RLS, and create four policies (select/insert/update/delete) scoped to group members via memberships join, per `data-model.md`
- [x] T002 [P] Create Meetup domain entity in `src/LoopMeet.Core/Models/Meetup.cs` — sealed class with init-only properties matching data-model.md (Id, GroupId, CreatedByUserId, Title, ScheduledAt, PlaceName, PlaceAddress, Latitude, Longitude, PlaceId, CreatedAt, UpdatedAt)
- [x] T003 [P] Create MeetupRecord Postgrest model in `src/LoopMeet.Infrastructure/Repositories/MeetupRecord.cs` — follows existing pattern in GroupRecord with [Table("meetups")], [PrimaryKey("id")], [Column(...)] attributes, inherits BaseModel
- [x] T004 [P] Create MeetupContracts DTOs in `src/LoopMeet.Api/Contracts/MeetupContracts.cs` — include MeetupResponse, CreateMeetupRequest, UpdateMeetupRequest, MeetupsResponse, UpcomingMeetupsResponse (with groupName field) per `contracts/meetups-api.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Backend services, API endpoints, Refit clients, app models, and DI registration that ALL user stories depend on

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T005 Create MeetupRepository in `src/LoopMeet.Infrastructure/Repositories/MeetupRepository.cs` — implement ListUpcomingByGroupAsync (filter scheduled_at > now(), order by scheduled_at asc), ListUpcomingByUserAsync (join memberships to get meetups across all user's groups, include group name), GetByIdAsync, InsertAsync, UpdateAsync, DeleteAsync — follow existing GroupRepository Postgrest query pattern
- [x] T006 Create MeetupQueryService in `src/LoopMeet.Api/Services/Meetups/MeetupQueryService.cs` — implement GetGroupMeetupsAsync(groupId) with 30s cache key `meetups:{groupId}`, and GetUpcomingForUserAsync(userId) with 30s cache key `home-meetups:{userId}` — follow GroupQueryService caching pattern, map MeetupRecord to MeetupResponse
- [x] T007 Create MeetupCommandService in `src/LoopMeet.Api/Services/Meetups/MeetupCommandService.cs` — implement CreateAsync (validate title non-empty/max 200, scheduled_at in future, location field consistency), UpdateAsync (validate same rules, update updated_at), DeleteAsync (hard delete) — invalidate `meetups:{groupId}` and `home-meetups:{userId}` caches on mutation, follow GroupCommandService pattern
- [x] T008 Create MeetupsEndpoints in `src/LoopMeet.Api/Endpoints/MeetupsEndpoints.cs` — register minimal API routes: GET `/groups/{groupId}/meetups`, POST `/groups/{groupId}/meetups` (returns 201), PATCH `/groups/{groupId}/meetups/{meetupId}`, DELETE `/groups/{groupId}/meetups/{meetupId}` (returns 204), GET `/meetups/upcoming` — all require authorization, follow GroupsEndpoints pattern
- [x] T009 [P] Create MeetupModels in `src/LoopMeet.App/Features/Meetups/Models/MeetupModels.cs` — include MeetupSummary (with calculated properties: HasLocation, LocationDisplay, DateDisplay, TimeDisplay, DateTimeDisplay, GroupName for home cards), MeetupsResponse, UpcomingMeetupsResponse, CreateMeetupRequest, UpdateMeetupRequest per data-model.md client-side models
- [x] T010 [P] Create IMeetupsApi Refit interface and MeetupsApi wrapper in `src/LoopMeet.App/Services/MeetupsApi.cs` — follow existing IGroupsApi/GroupsApi pattern with GetGroupMeetupsAsync, CreateMeetupAsync, UpdateMeetupAsync, DeleteMeetupAsync, GetUpcomingMeetupsAsync per `contracts/meetups-api.md`
- [x] T011 Register meetup services (MeetupRepository, MeetupQueryService, MeetupCommandService) and map MeetupsEndpoints in `src/LoopMeet.Api/Program.cs` — follow existing Groups service registration pattern
- [x] T012 Register IMeetupsApi Refit client and MeetupsApi wrapper in `src/LoopMeet.App/MauiProgram.cs` — use existing `AddLoopMeetApi<IMeetupsApi>(config)` pattern and add `builder.Services.AddSingleton<MeetupsApi>()`
- [x] T013 Register meetup page routes in `src/LoopMeet.App/AppShell.xaml.cs` — add `Routing.RegisterRoute("create-meetup", typeof(CreateMeetupPage))` and `Routing.RegisterRoute("edit-meetup", typeof(EditMeetupPage))`

**Checkpoint**: Foundation ready — all backend CRUD endpoints functional, Refit client wired up, routes registered. User story implementation can now begin.

---

## Phase 3: User Story 1 — Group Owner Creates a Meetup (Priority: P1) MVP

**Goal**: Group owner can tap an add-meetup FAB on the group detail page, fill in a form (title, date/time, optional location as TBD), and save a new meetup.

**Independent Test**: Create a meetup with title + date/time (location left as TBD) and verify it saves successfully via the API.

### Tests for User Story 1

- [x] T014 [P] [US1] Write integration tests for POST `/groups/{groupId}/meetups` in `tests/LoopMeet.Api.Tests/MeetupsEndpointsTests.cs` — test: successful creation returns 201, validation rejects empty title, validation rejects past scheduled_at, validation rejects invalid coordinates, non-member gets 403 — follow existing GroupsEndpointsTests pattern with TestWebApplicationFactory and InMemoryStore

### Implementation for User Story 1

- [x] T015 [P] [US1] Create CreateMeetupViewModel in `src/LoopMeet.App/Features/Meetups/ViewModels/CreateMeetupViewModel.cs` — ObservableObject with [ObservableProperty] for Title, SelectedDate, SelectedTime, PlaceName, PlaceAddress, Latitude, Longitude, PlaceId, IsBusy, ErrorMessage — [RelayCommand] SaveAsync that validates (title required, date/time must be future), calls MeetupsApi.CreateMeetupAsync, navigates back on success — accept groupId as query parameter via [QueryProperty]
- [x] T016 [P] [US1] Create CreateMeetupPage.xaml and code-behind in `src/LoopMeet.App/Features/Meetups/Views/CreateMeetupPage.xaml(.cs)` — form layout with Entry for title, DatePicker for date, TimePicker for time, location field (initially just a label showing "TBD" — autocomplete added in US5), Save button — use BubbleCardBorderStyle for form container, follow existing EditGroupPage layout patterns, bind to CreateMeetupViewModel
- [x] T017 [US1] Add second FAB (add meetup) to `src/LoopMeet.App/Features/Groups/Views/GroupDetailPage.xaml` — position above existing invite FAB (Margin="0,0,16,100"), 56x56, CornerRadius=28, use calendar/plus icon (ic_add_meetup.png), Primary/PrimaryDark background, IsVisible bound to IsOwner, Command bound to AddMeetupCommand
- [x] T018 [US1] Add AddMeetupCommand to `src/LoopMeet.App/Features/Groups/ViewModels/GroupDetailViewModel.cs` — [RelayCommand] that navigates to "create-meetup" route with groupId parameter, following existing InviteMemberCommand navigation pattern
- [x] T019 [US1] Add ic_add_meetup.png icon to `src/LoopMeet.App/Resources/Images/` — calendar-plus or event-add icon for the add meetup FAB

- [x] T048 [P] [US1] Write ViewModel unit tests for CreateMeetupViewModel in `tests/LoopMeet.App.Tests/CreateMeetupViewModelTests.cs` — test: SaveAsync rejects empty title, SaveAsync rejects past date/time, SaveAsync calls MeetupsApi.CreateMeetupAsync on valid input, IsBusy set during save, navigates back on success — mock MeetupsApi

**Checkpoint**: Group owner can tap the add-meetup FAB, fill in the form, and save a meetup. The meetup is persisted but not yet visible on the group page (that's US2).

---

## Phase 4: User Story 2 — View Upcoming Meetups on Group Page (Priority: P1)

**Goal**: All group members see upcoming meetups listed below the members section on the group detail page, sorted by date (soonest first). Past meetups excluded. Location displays as name or "TBD". Tapping a location opens the maps app.

**Independent Test**: Seed meetups (some future, some past) for a group and verify only future meetups appear sorted by date on the group detail screen.

### Tests for User Story 2

- [x] T020 [P] [US2] Write integration tests for GET `/groups/{groupId}/meetups` in `tests/LoopMeet.Api.Tests/MeetupsEndpointsTests.cs` — test: returns only future meetups, sorted by scheduled_at ascending, past meetups excluded, non-member gets 403 — add to existing test class from T014

### Implementation for User Story 2

- [x] T021 [US2] Add meetup loading to GroupDetailViewModel in `src/LoopMeet.App/Features/Groups/ViewModels/GroupDetailViewModel.cs` — add ObservableCollection<MeetupSummary> Meetups property, bool HasMeetups, call MeetupsApi.GetGroupMeetupsAsync(groupId) during LoadAsync, populate collection
- [x] T022 [US2] Add meetups list section to `src/LoopMeet.App/Features/Groups/Views/GroupDetailPage.xaml` — below the FlexLayout members section, add "Upcoming Meetups" label and BindableLayout (VerticalStackLayout) with MeetupSummary items, each card showing title, DateTimeDisplay, LocationDisplay using BubbleCardCompactBorderStyle, hide section when HasMeetups is false
- [x] T023 [US2] Add location tap-to-open-maps in GroupDetailViewModel in `src/LoopMeet.App/Features/Groups/ViewModels/GroupDetailViewModel.cs` — add [RelayCommand] OpenLocationAsync(MeetupSummary meetup) that calls `Map.Default.OpenAsync(meetup.Latitude.Value, meetup.Longitude.Value, new MapLaunchOptions { Name = meetup.PlaceName })` when HasLocation is true — add TapGestureRecognizer on location label in GroupDetailPage.xaml

- [x] T049 [P] [US2] Write ViewModel unit tests for GroupDetailViewModel meetup loading in `tests/LoopMeet.App.Tests/GroupDetailViewModelMeetupTests.cs` — test: LoadAsync populates Meetups collection, HasMeetups true when meetups exist, HasMeetups false when empty, OpenLocationAsync calls Map.Default.OpenAsync with correct coordinates — mock MeetupsApi

**Checkpoint**: All group members can see upcoming meetups on the group detail page. Owner can create and immediately see new meetups. Location tap opens maps app. US1 + US2 together form the MVP.

---

## Phase 5: User Story 3 — Home Page Shows Upcoming Meetups (Priority: P2)

**Goal**: Replace the home page placeholder card with a list of upcoming meetup cards from all of the user's groups, sorted by date. Each card shows title, date/time, location (or TBD), and group name. Empty state shows "no upcoming meetings" card.

**Independent Test**: Have a user in multiple groups with meetups, verify all upcoming meetups appear on home screen sorted by date, and verify empty state when no meetups exist.

### Tests for User Story 3

- [x] T024 [P] [US3] Write integration tests for GET `/meetups/upcoming` in `tests/LoopMeet.Api.Tests/MeetupsEndpointsTests.cs` — test: returns meetups across all user's groups with groupName populated, sorted by scheduled_at ascending, past meetups excluded, unauthenticated gets 401

### Implementation for User Story 3

- [x] T025 [US3] Update HomeViewModel in `src/LoopMeet.App/Features/Home/ViewModels/HomeViewModel.cs` — inject MeetupsApi, add ObservableCollection<MeetupSummary> UpcomingMeetups, bool HasUpcomingMeetups, bool ShowEmptyState — in LoadAsync call MeetupsApi.GetUpcomingMeetupsAsync(), populate collection, set ShowEmptyState when empty
- [x] T026 [US3] Replace placeholder card in `src/LoopMeet.App/Features/Home/Views/HomePage.xaml` — remove the existing placeholder Border (with house emoji and SupportingText), replace with: (1) BindableLayout of meetup cards (BubbleCardBorderStyle) each showing Title, DateTimeDisplay, LocationDisplay, GroupName — visible when HasUpcomingMeetups is true, (2) "No upcoming meetings" card (BubbleCardBorderStyle) visible when ShowEmptyState is true — reuse card styling from GroupDetailPage meetup cards
- [x] T027 [US3] Add location tap-to-open-maps on home page meetup cards in `src/LoopMeet.App/Features/Home/ViewModels/HomeViewModel.cs` — add [RelayCommand] OpenLocationAsync(MeetupSummary meetup) same pattern as GroupDetailViewModel, add TapGestureRecognizer on location label in HomePage.xaml

- [x] T050 [P] [US3] Write ViewModel unit tests for HomeViewModel meetup loading in `tests/LoopMeet.App.Tests/HomeViewModelMeetupTests.cs` — test: LoadAsync populates UpcomingMeetups collection, HasUpcomingMeetups/ShowEmptyState flags correct when meetups exist vs empty, OpenLocationAsync calls Map.Default.OpenAsync — mock MeetupsApi

**Checkpoint**: Home page shows all upcoming meetups across groups or "no upcoming meetings" card. Location tap opens maps.

---

## Phase 6: User Story 4 — Group Owner Edits a Meetup (Priority: P2)

**Goal**: Group owner can tap an upcoming meetup on the group detail page to open an edit form pre-populated with current values. They can change title, date/time, or location and save. Non-owners see view-only (no edit navigation).

**Independent Test**: Create a meetup, tap to edit, change each field, save, and verify changes are reflected in the group detail view.

### Tests for User Story 4

- [x] T028 [P] [US4] Write integration tests for PATCH `/groups/{groupId}/meetups/{meetupId}` in `tests/LoopMeet.Api.Tests/MeetupsEndpointsTests.cs` — test: successful update returns 200, partial update works (only title), clearing location (all null) works, validation rejects empty title, 404 for non-existent meetup

### Implementation for User Story 4

- [x] T029 [P] [US4] Create EditMeetupViewModel in `src/LoopMeet.App/Features/Meetups/ViewModels/EditMeetupViewModel.cs` — ObservableObject, accept groupId and meetupId as [QueryProperty], pre-populate fields from MeetupsApi.GetGroupMeetupsAsync (find by ID), [RelayCommand] SaveAsync that calls MeetupsApi.UpdateMeetupAsync, navigate back on success — reuse same validation logic as CreateMeetupViewModel (title required, future date)
- [x] T030 [P] [US4] Create EditMeetupPage.xaml and code-behind in `src/LoopMeet.App/Features/Meetups/Views/EditMeetupPage.xaml(.cs)` — same form layout as CreateMeetupPage but with Title="Edit Meetup" and pre-populated fields, Save button updates instead of creates — reuse identical styling (BubbleCardBorderStyle form container)
- [x] T031 [US4] Add tap gesture for owner edit navigation on meetup cards in `src/LoopMeet.App/Features/Groups/Views/GroupDetailPage.xaml` — add TapGestureRecognizer on the meetup card Border that calls EditMeetupCommand in GroupDetailViewModel, only when IsOwner is true — command navigates to "edit-meetup" route with groupId and meetupId parameters

**Checkpoint**: Group owner can tap a meetup card to edit. Non-owners see no edit affordance. Location changes (including clearing to TBD) work correctly.

---

## Phase 7: User Story 5 — Location Search with Maps Autocomplete (Priority: P2)

**Goal**: On the create/edit meetup forms, typing in the location field triggers a server-side Google Places autocomplete. Selecting a result stores place name, address, and coordinates. Graceful degradation when the service is unavailable.

**Independent Test**: Type a partial location name, verify autocomplete results appear, select one, save meetup, and verify stored location contains name, address, and coordinates.

### Tests for User Story 5

- [x] T032 [P] [US5] Write integration tests for Places proxy endpoints in `tests/LoopMeet.Api.Tests/PlacesEndpointsTests.cs` — test: GET `/places/autocomplete?query=central` returns predictions, query too short (<2 chars) returns 400, GET `/places/{placeId}` returns detail with coordinates — mock Google Places HTTP calls in test

### Implementation for User Story 5

- [x] T033 [P] [US5] Create PlacesContracts DTOs in `src/LoopMeet.Api/Contracts/PlacesContracts.cs` — PlacePrediction (placeId, mainText, secondaryText, description), PlaceAutocompleteResponse (predictions list), PlaceDetailResponse (placeId, name, formattedAddress, latitude, longitude), PlacesOptions (ApiKey) per `contracts/places-proxy-api.md`
- [x] T034 [P] [US5] Create IPlacesApi Refit interface and PlacesApi wrapper in `src/LoopMeet.App/Services/PlacesApi.cs` — AutocompleteAsync(query), GetPlaceDetailAsync(placeId) per `contracts/places-proxy-api.md`, follow existing GroupsApi wrapper pattern
- [x] T035 [US5] Create PlacesProxyService in `src/LoopMeet.Api/Services/Places/PlacesProxyService.cs` — inject HttpClient and PlacesOptions (API key), implement AutocompleteAsync (POST to `https://places.googleapis.com/v1/places:autocomplete` with X-Goog-Api-Key header), GetPlaceDetailAsync (GET `https://places.googleapis.com/v1/places/{placeId}` with field mask for displayName, formattedAddress, location) — return 503 on Google API failure
- [x] T036 [US5] Create PlacesEndpoints in `src/LoopMeet.Api/Endpoints/PlacesEndpoints.cs` — GET `/places/autocomplete` (query param, min 2 chars or 400), GET `/places/{placeId}` — require authorization, delegate to PlacesProxyService
- [x] T037 [US5] Register PlacesProxyService, PlacesOptions, and PlacesEndpoints in `src/LoopMeet.Api/Program.cs` — configure PlacesOptions from GOOGLE_PLACES_API_KEY environment variable/config, register HttpClient for PlacesProxyService
- [x] T038 [US5] Register IPlacesApi Refit client and PlacesApi wrapper in `src/LoopMeet.App/MauiProgram.cs` — `AddLoopMeetApi<IPlacesApi>(config)` and `builder.Services.AddSingleton<PlacesApi>()`
- [x] T039 [US5] Add autocomplete UI and logic to CreateMeetupViewModel and CreateMeetupPage — in ViewModel: add ObservableCollection<PlacePrediction> Predictions, string LocationSearchText with debounced (300ms) PlacesApi.AutocompleteAsync calls on text change, [RelayCommand] SelectPredictionAsync that calls PlacesApi.GetPlaceDetailAsync and populates PlaceName/PlaceAddress/Latitude/Longitude/PlaceId, string LocationErrorMessage for service unavailable — in XAML: add Entry for location search, CollectionView bound to Predictions (each showing mainText + secondaryText), selected prediction populates location fields, show error label when service unavailable
- [x] T040 [US5] Add autocomplete UI and logic to EditMeetupViewModel and EditMeetupPage — same pattern as T039, additionally allow clearing location (set all location fields to null, hide predictions, revert to TBD)

**Checkpoint**: Location autocomplete works in both create and edit forms. Selecting a place stores full details. Clearing location reverts to TBD. Service unavailability shows error message but allows saving without location.

---

## Phase 8: User Story 6 — Group Owner Deletes a Meetup (Priority: P2)

**Goal**: Group owner can swipe left on a meetup card in the group detail page to reveal a delete action (same SwipeView pattern as invitation accept/decline). A confirmation dialog appears before deletion. Non-owners see no swipe action.

**Independent Test**: Create a meetup, swipe to reveal delete, confirm deletion, verify meetup is removed from group detail and home page.

### Tests for User Story 6

- [x] T041 [P] [US6] Write integration tests for DELETE `/groups/{groupId}/meetups/{meetupId}` in `tests/LoopMeet.Api.Tests/MeetupsEndpointsTests.cs` — test: successful delete returns 204, subsequent GET excludes deleted meetup, 404 for non-existent meetup, non-member gets 403

### Implementation for User Story 6

- [x] T042 [US6] Wrap meetup cards in SwipeView in `src/LoopMeet.App/Features/Groups/Views/GroupDetailPage.xaml` — add SwipeView with LeftItems (SwipeItems Mode="Execute"), red background (#DC2626), delete trash emoji + "Delete" label, Command bound to DeleteMeetupCommand, CommandParameter bound to meetup item — SwipeView only shown when IsOwner is true (wrap in conditional), follow exact PendingInvitationsPage.xaml SwipeView pattern
- [x] T043 [US6] Add DeleteMeetupCommand to GroupDetailViewModel in `src/LoopMeet.App/Features/Groups/ViewModels/GroupDetailViewModel.cs` — [RelayCommand] that first shows `DisplayAlert("Delete Meetup", "Are you sure you want to delete this meetup?", "Delete", "Cancel")`, on confirm calls MeetupsApi.DeleteMeetupAsync(groupId, meetupId), removes meetup from Meetups collection
- [x] T044 [US6] Add desktop delete button fallback on meetup cards in `src/LoopMeet.App/Features/Groups/Views/GroupDetailPage.xaml` — HorizontalStackLayout with delete Button, IsVisible via `{OnIdiom Phone=False, Tablet=False, Desktop=True, TV=True}`, visible only when IsOwner — follow PendingInvitationsPage desktop button pattern

- [x] T051 [P] [US6] Write ViewModel unit tests for GroupDetailViewModel delete in `tests/LoopMeet.App.Tests/GroupDetailViewModelDeleteTests.cs` — test: DeleteMeetupCommand calls DisplayAlert for confirmation, confirmed delete calls MeetupsApi.DeleteMeetupAsync and removes from collection, cancelled delete does not call API — mock MeetupsApi and Page.DisplayAlert

**Checkpoint**: Owner can swipe-to-delete meetups with confirmation. Non-owners see no delete affordance. Desktop users see a delete button. Deleted meetups disappear from all views.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Quality, consistency, and cross-cutting improvements

- [x] T045 Verify light and dark theme styling on all new meetup UI (create/edit forms, group detail meetup list, home page meetup cards, SwipeView delete) — test on both themes and fix any missing AppThemeBinding colors
- [x] T046 [P] Add structured logging to MeetupCommandService in `src/LoopMeet.Api/Services/Meetups/MeetupCommandService.cs` — log meetup created/updated/deleted events with groupId, meetupId, userId using existing Serilog pattern
- [x] T052 [P] Add structured logging to PlacesProxyService in `src/LoopMeet.Api/Services/Places/PlacesProxyService.cs` — log autocomplete requests (query text, result count), place detail requests, and Google API errors/timeouts with Serilog structured logging pattern
- [x] T047 Run quickstart.md validation — verify migration applies cleanly, API starts, Refit clients connect, create/edit/delete/list flows work end-to-end

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 completion — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Phase 2 — creates meetups
- **US2 (Phase 4)**: Depends on Phase 2 — views meetups (can run parallel with US1, but US1+US2 together = MVP)
- **US3 (Phase 5)**: Depends on Phase 2 — home page cards
- **US4 (Phase 6)**: Depends on Phase 2 — edits meetups (benefits from US1/US2 being done for testing)
- **US5 (Phase 7)**: Depends on Phase 2, enhances US1 and US4 — adds autocomplete to create/edit forms
- **US6 (Phase 8)**: Depends on US2 (meetup list must exist to swipe on) — adds delete
- **Polish (Phase 9)**: Depends on all desired user stories being complete

### User Story Dependencies

- **US1 (P1)**: After Phase 2 — No story dependencies
- **US2 (P1)**: After Phase 2 — No story dependencies (parallel with US1)
- **US3 (P2)**: After Phase 2 — Independent (parallel with US1/US2)
- **US4 (P2)**: After Phase 2 — Independent (benefits from US1 for testing flow)
- **US5 (P2)**: After Phase 2 — Enhances US1 + US4 create/edit forms
- **US6 (P2)**: After US2 — Requires meetup list UI to exist in GroupDetailPage

### Within Each User Story

- Tests written first (TDD where applicable)
- Models/contracts before services
- Services before endpoints
- Backend before frontend
- Core implementation before integration

### Parallel Opportunities

- Phase 1: T002, T003, T004 can all run in parallel
- Phase 2: T009, T010 can run parallel with T005-T008 (different projects)
- After Phase 2: US1, US2, US3, US4 can all start in parallel
- US5: T032, T033, T034 can run in parallel
- US6: T041 can run parallel with T042-T044

---

## Parallel Example: Phase 1

```text
# All in parallel (different files, no dependencies):
T002: Create Meetup entity in src/LoopMeet.Core/Models/Meetup.cs
T003: Create MeetupRecord in src/LoopMeet.Infrastructure/Repositories/MeetupRecord.cs
T004: Create MeetupContracts in src/LoopMeet.Api/Contracts/MeetupContracts.cs
```

## Parallel Example: US1

```text
# Tests + ViewModel + Page in parallel (different files):
T014: Integration tests in tests/LoopMeet.Api.Tests/MeetupsEndpointsTests.cs
T015: CreateMeetupViewModel in src/LoopMeet.App/Features/Meetups/ViewModels/CreateMeetupViewModel.cs
T016: CreateMeetupPage.xaml in src/LoopMeet.App/Features/Meetups/Views/CreateMeetupPage.xaml
```

---

## Implementation Strategy

### MVP First (User Stories 1 + 2)

1. Complete Phase 1: Setup (migration, entity, record, contracts)
2. Complete Phase 2: Foundational (repository, services, endpoints, Refit client, DI)
3. Complete Phase 3: US1 — Owner creates meetup
4. Complete Phase 4: US2 — Members view meetups on group page
5. **STOP and VALIDATE**: Owner can create meetups, all members can view them
6. Deploy/demo — core meetup feature is functional

### Incremental Delivery

1. Setup + Foundational → Backend ready
2. US1 + US2 → Create and view meetups (MVP!)
3. US3 → Home page meetup cards
4. US4 → Edit meetups
5. US5 → Location autocomplete (enhances create/edit)
6. US6 → Swipe-to-delete
7. Polish → Theme verification, logging, E2E validation

### Recommended Execution Order (Single Developer)

Phase 1 → Phase 2 → US1 → US2 → US4 → US6 → US3 → US5 → Polish

Rationale: US1+US2 form the MVP. US4 (edit) and US6 (delete) complete the group detail experience. US3 (home page) is independent. US5 (autocomplete) is the most complex and enhances the already-functional create/edit forms.

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story is independently completable and testable after Phase 2
- Constitution Principle II: All test tasks are required, not optional
- Reuse existing UI patterns throughout: BubbleCardBorderStyle/CompactBorderStyle, SwipeView, FAB sizing, AppThemeBinding, OnIdiom desktop fallbacks
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
