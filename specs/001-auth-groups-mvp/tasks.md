---

description: "Task list for Auth & Groups MVP implementation"
---

# Tasks: Auth & Groups MVP

**Input**: Design documents from `/specs/001-auth-groups-mvp/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required by the constitution; test tasks are included per user story.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [ ] T001 Create solution and project structure in src/LoopMeet.App, src/LoopMeet.Api, src/LoopMeet.Core, src/LoopMeet.Infrastructure, tests/LoopMeet.Api.Tests, tests/LoopMeet.Core.Tests, tests/LoopMeet.Infrastructure.Tests
- [ ] T002 Configure shared build settings in Directory.Build.props at repository root
- [ ] T003 [P] Add NuGet package references in src/LoopMeet.Api/LoopMeet.Api.csproj and src/LoopMeet.Infrastructure/LoopMeet.Infrastructure.csproj
- [ ] T004 [P] Add NuGet package references in src/LoopMeet.App/LoopMeet.App.csproj and src/LoopMeet.Core/LoopMeet.Core.csproj
- [ ] T005 [P] Add API configuration files at src/LoopMeet.Api/appsettings.json and src/LoopMeet.Api/appsettings.Development.json
- [ ] T006 [P] Add app configuration holder in src/LoopMeet.App/Services/AppConfig.cs

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

- [ ] T007 Create core domain models in src/LoopMeet.Core/Models/User.cs, Group.cs, Membership.cs, Invitation.cs, AuthIdentity.cs
- [ ] T008 Create validation rules in src/LoopMeet.Core/Validators/UserValidator.cs, GroupValidator.cs, InvitationValidator.cs
- [ ] T009 Implement EF Core DbContext and entity configs in src/LoopMeet.Infrastructure/Data/LoopMeetDbContext.cs and src/LoopMeet.Infrastructure/Data/Configurations/*.cs
- [ ] T010 Configure EF Core migrations and connection registration in src/LoopMeet.Api/Program.cs
- [ ] T011 Implement repository interfaces in src/LoopMeet.Core/Interfaces/*.cs and repositories in src/LoopMeet.Infrastructure/Repositories/*.cs
- [ ] T012 Add error response contract in src/LoopMeet.Api/Contracts/ErrorResponse.cs and exception middleware in src/LoopMeet.Api/Services/ErrorHandlingMiddleware.cs
- [ ] T013 Configure Serilog logging and request correlation in src/LoopMeet.Api/Program.cs
- [ ] T014 Implement Supabase JWT validation in src/LoopMeet.Api/Services/Auth/JwtValidationHandler.cs and wire auth in src/LoopMeet.Api/Program.cs
- [ ] T015 Add current-user context service in src/LoopMeet.Api/Services/Auth/CurrentUserService.cs
- [ ] T016 Add API client setup with Refit + Polly in src/LoopMeet.App/Services/ApiClient.cs
- [ ] T017 Configure MAUI Shell routes in src/LoopMeet.App/AppShell.xaml and src/LoopMeet.App/AppShell.xaml.cs
- [ ] T018 Configure text-only splash screen asset in src/LoopMeet.App/Resources/Splash/loopmeet_splash.svg and update src/LoopMeet.App/LoopMeet.App.csproj
- [ ] T019 [P] Add API test host in tests/LoopMeet.Api.Tests/Infrastructure/TestWebApplicationFactory.cs
- [ ] T020 [P] Add Postgres Testcontainers fixture in tests/LoopMeet.Api.Tests/Infrastructure/PostgresFixture.cs
- [ ] T021 [P] Add validator tests in tests/LoopMeet.Core.Tests/Validators/UserValidatorTests.cs and GroupValidatorTests.cs

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Sign In or Create Account (Priority: P1) 🎯 MVP

**Goal**: Users can sign in with email or Google, complete account creation, and land on the groups list.

**Independent Test**: Launch the app, complete email or Google sign-in, fill missing profile fields if needed, and reach the groups list screen.

### Tests for User Story 1

- [ ] T022 [P] [US1] Add API integration tests for profile upsert and required fields in tests/LoopMeet.Api.Tests/Endpoints/UserEndpointsTests.cs

### Implementation for User Story 1

- [ ] T023 [P] [US1] Define auth DTOs in src/LoopMeet.App/Features/Auth/Models/AuthModels.cs
- [ ] T024 [P] [US1] Implement Supabase auth service in src/LoopMeet.App/Features/Auth/AuthService.cs
- [ ] T025 [P] [US1] Build login UI in src/LoopMeet.App/Features/Auth/Views/LoginPage.xaml and src/LoopMeet.App/Features/Auth/ViewModels/LoginViewModel.cs
- [ ] T026 [US1] Build create account UI in src/LoopMeet.App/Features/Auth/Views/CreateAccountPage.xaml and src/LoopMeet.App/Features/Auth/ViewModels/CreateAccountViewModel.cs
- [ ] T027 [US1] Implement social profile pre-fill and required-field validation in src/LoopMeet.App/Features/Auth/ViewModels/CreateAccountViewModel.cs
- [ ] T028 [US1] Implement auth flow coordinator in src/LoopMeet.App/Features/Auth/AuthCoordinator.cs
- [ ] T029 [US1] Provision user profile on first API access in src/LoopMeet.Api/Services/Auth/UserProvisioningService.cs
- [ ] T030 [US1] Add API endpoint for user profile upsert in src/LoopMeet.Api/Endpoints/UserEndpoints.cs

**Checkpoint**: User Story 1 is functional and independently testable

---

## Phase 4: User Story 2 - Browse Groups and View Details (Priority: P2)

**Goal**: Users can see their groups (owned first, then member-only, sorted by name), pending invitations, and group details.

**Independent Test**: Load the groups list, verify sorting and invitations list, open a group detail, and see members.

### Tests for User Story 2

- [ ] T031 [P] [US2] Add API integration tests for groups list sorting in tests/LoopMeet.Api.Tests/Endpoints/GroupsEndpointsTests.cs
- [ ] T032 [P] [US2] Add API integration tests for invitations list in tests/LoopMeet.Api.Tests/Endpoints/InvitationsEndpointsTests.cs

### Implementation for User Story 2

- [ ] T033 [P] [US2] Implement group query service in src/LoopMeet.Api/Services/Groups/GroupQueryService.cs
- [ ] T034 [P] [US2] Implement invitation query service in src/LoopMeet.Api/Services/Invitations/InvitationQueryService.cs
- [ ] T035 [US2] Add GET /groups endpoint in src/LoopMeet.Api/Endpoints/GroupsEndpoints.cs
- [ ] T036 [US2] Add GET /groups/{groupId} endpoint in src/LoopMeet.Api/Endpoints/GroupsEndpoints.cs
- [ ] T037 [US2] Add GET /invitations endpoint in src/LoopMeet.Api/Endpoints/InvitationsEndpoints.cs
- [ ] T038 [P] [US2] Add groups API client in src/LoopMeet.App/Services/GroupsApi.cs
- [ ] T039 [P] [US2] Add invitations API client in src/LoopMeet.App/Services/InvitationsApi.cs
- [ ] T040 [P] [US2] Build groups list UI in src/LoopMeet.App/Features/Groups/Views/GroupsListPage.xaml and src/LoopMeet.App/Features/Groups/ViewModels/GroupsListViewModel.cs
- [ ] T041 [P] [US2] Build group detail UI in src/LoopMeet.App/Features/Groups/Views/GroupDetailPage.xaml and src/LoopMeet.App/Features/Groups/ViewModels/GroupDetailViewModel.cs
- [ ] T042 [US2] Add invitations section and empty-state prompt in src/LoopMeet.App/Features/Groups/Views/GroupsListPage.xaml

**Checkpoint**: User Story 2 is functional and independently testable

---

## Phase 5: User Story 3 - Create or Edit a Group (Priority: P3)

**Goal**: Owners can create groups and rename them, with duplicate-name validation.

**Independent Test**: Create a group, rename it, and verify duplicate names are rejected with a friendly message.

### Tests for User Story 3

- [ ] T043 [P] [US3] Add API integration tests for create/rename and duplicates in tests/LoopMeet.Api.Tests/Endpoints/GroupsWriteEndpointsTests.cs

### Implementation for User Story 3

- [ ] T044 [P] [US3] Implement group command service in src/LoopMeet.Api/Services/Groups/GroupCommandService.cs
- [ ] T045 [US3] Add POST /groups endpoint in src/LoopMeet.Api/Endpoints/GroupsEndpoints.cs
- [ ] T046 [US3] Add PATCH /groups/{groupId} endpoint in src/LoopMeet.Api/Endpoints/GroupsEndpoints.cs
- [ ] T047 [P] [US3] Build create group UI in src/LoopMeet.App/Features/Groups/Views/CreateGroupPage.xaml and src/LoopMeet.App/Features/Groups/ViewModels/CreateGroupViewModel.cs
- [ ] T048 [P] [US3] Build edit group UI in src/LoopMeet.App/Features/Groups/Views/EditGroupPage.xaml and src/LoopMeet.App/Features/Groups/ViewModels/EditGroupViewModel.cs
- [ ] T049 [US3] Wire group create/edit API calls in src/LoopMeet.App/Services/GroupsApi.cs

**Checkpoint**: User Story 3 is functional and independently testable

---

## Phase 6: User Story 4 - Invite Members by Email (Priority: P4)

**Goal**: Owners can invite by email, and recipients can accept invitations to join groups.

**Independent Test**: Send an invite, confirm it appears in the pending list, and accept it to join the group.

### Tests for User Story 4

- [ ] T050 [P] [US4] Add API integration tests for invitation create/accept in tests/LoopMeet.Api.Tests/Endpoints/InvitationsWriteEndpointsTests.cs

### Implementation for User Story 4

- [ ] T051 [P] [US4] Implement invitation command service in src/LoopMeet.Api/Services/Invitations/InvitationCommandService.cs
- [ ] T052 [US4] Add POST /groups/{groupId}/invitations endpoint in src/LoopMeet.Api/Endpoints/InvitationsEndpoints.cs
- [ ] T053 [US4] Add POST /invitations/{invitationId}/accept endpoint in src/LoopMeet.Api/Endpoints/InvitationsEndpoints.cs
- [ ] T054 [P] [US4] Build invite member UI in src/LoopMeet.App/Features/Invitations/Views/InviteMemberPage.xaml and src/LoopMeet.App/Features/Invitations/ViewModels/InviteMemberViewModel.cs
- [ ] T055 [US4] Wire invitation API calls in src/LoopMeet.App/Services/InvitationsApi.cs
- [ ] T056 [US4] Add accept-invitation action in src/LoopMeet.App/Features/Groups/ViewModels/GroupsListViewModel.cs
- [ ] T057 [US4] Add already-member messaging in src/LoopMeet.App/Features/Invitations/ViewModels/InviteMemberViewModel.cs and src/LoopMeet.App/Services/InvitationsApi.cs

**Checkpoint**: User Story 4 is functional and independently testable

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T058 [P] Add user-friendly error strings in src/LoopMeet.App/Resources/Strings.resx
- [ ] T059 Add API error code mapping in src/LoopMeet.Api/Services/ErrorCodeMapper.cs
- [ ] T060 Add API logging around group and invitation actions in src/LoopMeet.Api/Services/Groups/GroupCommandService.cs and src/LoopMeet.Api/Services/Invitations/InvitationCommandService.cs
- [ ] T061 [P] Add lightweight API response timing checks in tests/LoopMeet.Api.Tests/Performance/GroupsPerformanceTests.cs
- [ ] T062 Conduct UX review and record outcomes in specs/001-auth-groups-mvp/plan.md
- [ ] T063 Validate quickstart steps and update specs/001-auth-groups-mvp/quickstart.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - blocks all user stories
- **User Stories (Phase 3-6)**: Depend on Foundational phase completion
- **Polish (Phase 7)**: Depends on selected user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational; no dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational; integrates with US1 for auth
- **User Story 3 (P3)**: Can start after Foundational; uses groups list from US2
- **User Story 4 (P4)**: Can start after Foundational; uses groups and invitations list from US2

### Parallel Opportunities

- Setup tasks T003-T006 can run in parallel.
- Foundational tasks T007-T021 can be distributed by layer (Core/Infrastructure/API/App/Test).
- Within each story, parallelize UI and API tasks marked [P].

---

## Parallel Example: User Story 2

```bash
Task: "Implement group query service in src/LoopMeet.Api/Services/Groups/GroupQueryService.cs"
Task: "Implement invitation query service in src/LoopMeet.Api/Services/Invitations/InvitationQueryService.cs"
Task: "Build groups list UI in src/LoopMeet.App/Features/Groups/Views/GroupsListPage.xaml"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Validate User Story 1 independently

### Incremental Delivery

1. Setup + Foundational
2. User Story 1 → validate
3. User Story 2 → validate
4. User Story 3 → validate
5. User Story 4 → validate
