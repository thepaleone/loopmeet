# Tasks: Profile Tab and Profile Management

**Input**: Design documents from `/Users/joel/projects/palehorse/loopmeet/specs/003-profile-settings-tab/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Required. The feature spec/plan explicitly requires automated test updates for all changed functionality.

## Format: `[ID] [P?] [Story] Description`

- [P] = task can run in parallel (different files, no dependency on incomplete tasks)
- [Story] = user story label (`[US1]`, `[US2]`, `[US3]`, `[US4]`) for story-phase tasks only
- Every task includes a concrete file path

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create profile feature scaffolding and test harness updates used by all stories.

- [X] T001 Create profile feature module scaffolding in `src/LoopMeet.App/Features/Profile/Models/ProfileModels.cs`, `src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs`, `src/LoopMeet.App/Features/Profile/ViewModels/ChangePasswordViewModel.cs`, `src/LoopMeet.App/Features/Profile/Views/ProfilePage.xaml`, `src/LoopMeet.App/Features/Profile/Views/ProfilePage.xaml.cs`, `src/LoopMeet.App/Features/Profile/Views/ChangePasswordPage.xaml`, and `src/LoopMeet.App/Features/Profile/Views/ChangePasswordPage.xaml.cs`
- [X] T002 Add profile tab icon asset placeholders in `src/LoopMeet.App/Resources/Images/tab_profile.svg` and `src/LoopMeet.App/Resources/Images/tab_profile_fallback.png`
- [X] T003 Create profile/auth app test files in `tests/LoopMeet.App.Tests/Features/Profile/ProfileViewModelTests.cs`, `tests/LoopMeet.App.Tests/Features/Profile/ChangePasswordViewModelTests.cs`, and `tests/LoopMeet.App.Tests/Features/Auth/LoginViewModelTests.cs`
- [X] T004 Update app test project links and new test doubles in `tests/LoopMeet.App.Tests/LoopMeet.App.Tests.csproj`, `tests/LoopMeet.App.Tests/TestDoubles/FakeUsersApi.cs`, and `tests/LoopMeet.App.Tests/TestDoubles/FakeAuthService.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core persistence/contracts/services required before user story implementation.

**CRITICAL**: No user story work starts until this phase is complete.

- [X] T005 Create additive Supabase migration for avatar-source fields in `supabase/migrations/20260226170000_add_user_profile_avatar_source_fields.sql`
- [X] T006 Update user domain/persistence models for avatar metadata in `src/LoopMeet.Core/Models/User.cs` and `src/LoopMeet.Infrastructure/Supabase/Models/UserRecord.cs`
- [X] T007 Update user repository mapping for new avatar fields in `src/LoopMeet.Infrastructure/Repositories/UserRepository.cs`
- [X] T008 Update API profile contracts and add password request contract in `src/LoopMeet.Api/Contracts/UserProfileRequest.cs`, `src/LoopMeet.Api/Contracts/UserProfileResponse.cs`, and `src/LoopMeet.Api/Contracts/PasswordChangeRequest.cs`
- [X] T009 Update app DTOs for expanded profile/password payloads in `src/LoopMeet.App/Features/Auth/Models/AuthModels.cs` and `src/LoopMeet.App/Features/Profile/Models/ProfileModels.cs`
- [X] T010 Update users API client signatures for profile patch and password change in `src/LoopMeet.App/Services/UsersApi.cs`
- [X] T011 Create shared avatar precedence helper in `src/LoopMeet.Api/Services/Auth/ProfileAvatarResolver.cs`
- [X] T012 Create profile projection helper for user-since/group-count read model in `src/LoopMeet.Api/Services/Auth/UserProfileProjectionService.cs`

**Checkpoint**: Foundation complete; user stories can be implemented.

---

## Phase 3: User Story 1 - Open and View Profile Tab (Priority: P1) MVP

**Goal**: Add `Profile` as the last signed-in tab and render profile summary with required fields/actions.

**Independent Test**: Sign in, verify `Profile` is the last tab, open it, and confirm display name/avatar placeholder, `User since`, group count, and action entry points.

### Tests for User Story 1

- [X] T013 [P] [US1] Add app tests for profile tab route and summary load state in `tests/LoopMeet.App.Tests/Features/Profile/ProfileViewModelTests.cs`
- [X] T014 [P] [US1] Add API tests for profile summary response fields (`userSince`, `groupCount`, `avatarSource`) in `tests/LoopMeet.Api.Tests/Endpoints/UserEndpointsTests.cs`

### Implementation for User Story 1

- [X] T015 [US1] Add profile route/path/icon constants in `src/LoopMeet.App/Features/Home/Models/SignedInTabs.cs`
- [X] T016 [US1] Append `Profile` tab as the final tab in `src/LoopMeet.App/AppShell.xaml`
- [X] T017 [US1] Register profile navigation routes in `src/LoopMeet.App/AppShell.xaml.cs`
- [X] T018 [US1] Register profile page/viewmodel dependencies in `src/LoopMeet.App/MauiProgram.cs`
- [X] T019 [US1] Implement profile summary query (including user-since and group-count projection) in `src/LoopMeet.Api/Services/Auth/UserProvisioningService.cs` and `src/LoopMeet.Api/Services/Auth/UserProfileProjectionService.cs`
- [X] T020 [US1] Update `GET /users/profile` response mapping in `src/LoopMeet.Api/Endpoints/UserEndpoints.cs`
- [X] T021 [US1] Implement profile load/state commands in `src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs`
- [X] T022 [US1] Implement profile summary UI and action entry points in `src/LoopMeet.App/Features/Profile/Views/ProfilePage.xaml` and `src/LoopMeet.App/Features/Profile/Views/ProfilePage.xaml.cs`

**Checkpoint**: US1 is independently functional and testable.

---

## Phase 4: User Story 2 - Edit Display Name and Avatar (Priority: P2)

**Goal**: Allow display-name/avatar edits and enforce avatar override precedence over social-avatar data.

**Independent Test**: Edit display name/avatar in Profile tab, save successfully, reload profile, and verify social sign-in does not overwrite avatar override.

### Tests for User Story 2

- [X] T023 [P] [US2] Add app tests for display-name/avatar save state transitions and feedback in `tests/LoopMeet.App.Tests/Features/Profile/ProfileViewModelTests.cs`
- [X] T024 [P] [US2] Add API tests for `PATCH /users/profile` display-name and avatar override behavior in `tests/LoopMeet.Api.Tests/Endpoints/UserEndpointsTests.cs`

### Implementation for User Story 2

- [X] T025 [US2] Implement `PATCH /users/profile` endpoint validation/flow in `src/LoopMeet.Api/Endpoints/UserEndpoints.cs`
- [X] T026 [US2] Implement profile update behavior with avatar precedence resolution in `src/LoopMeet.Api/Services/Auth/UserProvisioningService.cs` and `src/LoopMeet.Api/Services/Auth/ProfileAvatarResolver.cs`
- [X] T027 [US2] Add app-side profile update API usage in `src/LoopMeet.App/Services/UsersApi.cs`
- [X] T028 [US2] Implement display-name/avatar save commands in `src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs`
- [X] T029 [US2] Add editable display-name/avatar controls with save feedback in `src/LoopMeet.App/Features/Profile/Views/ProfilePage.xaml`
- [X] T030 [US2] Wire avatar preview/binding updates in `src/LoopMeet.App/Features/Profile/Views/ProfilePage.xaml.cs` and `src/LoopMeet.App/Features/Profile/Models/ProfileModels.cs`

**Checkpoint**: US2 is independently functional and testable.

---

## Phase 5: User Story 3 - Change Password in a Modal (Priority: P3)

**Goal**: Deliver dedicated modal password-change flow with validation and resilient success/error behavior.

**Independent Test**: Open password flow from Profile page, verify modal (no inline password fields), submit invalid then valid payloads, and confirm expected feedback and return-to-profile behavior.

### Tests for User Story 3

- [X] T031 [P] [US3] Add app tests for password modal open/close and validation feedback in `tests/LoopMeet.App.Tests/Features/Profile/ProfileViewModelTests.cs` and `tests/LoopMeet.App.Tests/Features/Profile/ChangePasswordViewModelTests.cs`
- [X] T032 [P] [US3] Add API tests for `POST /users/password` success and validation failures in `tests/LoopMeet.Api.Tests/Endpoints/UserEndpointsTests.cs`

### Implementation for User Story 3

- [X] T033 [US3] Implement password-change service method using policy validation in `src/LoopMeet.Api/Services/Auth/UserProvisioningService.cs` and `src/LoopMeet.Api/Services/Auth/PasswordPolicyValidator.cs`
- [X] T034 [US3] Add `POST /users/password` endpoint and error mapping in `src/LoopMeet.Api/Endpoints/UserEndpoints.cs`
- [X] T035 [US3] Add app password-change API contract/client usage in `src/LoopMeet.App/Services/UsersApi.cs` and `src/LoopMeet.App/Features/Profile/Models/ProfileModels.cs`
- [X] T036 [US3] Implement modal command/validation logic in `src/LoopMeet.App/Features/Profile/ViewModels/ChangePasswordViewModel.cs`
- [X] T037 [US3] Build password modal UI in `src/LoopMeet.App/Features/Profile/Views/ChangePasswordPage.xaml` and `src/LoopMeet.App/Features/Profile/Views/ChangePasswordPage.xaml.cs`
- [X] T038 [US3] Wire profile action to launch/handle modal results in `src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs` and `src/LoopMeet.App/Features/Profile/Views/ProfilePage.xaml.cs`
- [X] T039 [US3] Register password modal dependencies in `src/LoopMeet.App/MauiProgram.cs` and `src/LoopMeet.App/AppShell.xaml.cs`

**Checkpoint**: US3 is independently functional and testable.

---

## Phase 6: User Story 4 - Inherit Social Avatar on Account Creation (Priority: P4)

**Goal**: Copy social avatar on profile bootstrap only when no avatar override is established.

**Independent Test**: Complete social-login bootstrap with avatar and verify initial profile avatar copies; repeat with existing override and verify override remains unchanged.

### Tests for User Story 4

- [X] T040 [P] [US4] Add app tests for social-avatar bootstrap payload behavior in `tests/LoopMeet.App.Tests/Features/Auth/LoginViewModelTests.cs`
- [X] T041 [P] [US4] Add API tests for social-avatar copy conditional on override absence in `tests/LoopMeet.Api.Tests/Endpoints/UserEndpointsTests.cs`

### Implementation for User Story 4

- [X] T042 [US4] Extend OAuth result/avatar metadata extraction in `src/LoopMeet.App/Features/Auth/Models/AuthModels.cs` and `src/LoopMeet.App/Features/Auth/AuthService.cs`
- [X] T043 [US4] Include social-avatar data in profile bootstrap upsert calls in `src/LoopMeet.App/Features/Auth/ViewModels/LoginViewModel.cs` and `src/LoopMeet.App/Features/Auth/ViewModels/CreateAccountViewModel.cs`
- [X] T044 [US4] Apply backend social-avatar copy-only-without-override rule in `src/LoopMeet.Api/Services/Auth/UserProvisioningService.cs` and `src/LoopMeet.Api/Services/Auth/ProfileAvatarResolver.cs`
- [X] T045 [US4] Ensure profile response projection remains override-first after OAuth bootstrap in `src/LoopMeet.Api/Contracts/UserProfileResponse.cs` and `src/LoopMeet.Api/Endpoints/UserEndpoints.cs`

**Checkpoint**: US4 is independently functional and testable.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final reliability, observability, and validation sweep across stories.

- [X] T046 [P] Add/normalize structured logging for profile load/save/password/bootstrap flows in `src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs` and `src/LoopMeet.Api/Services/Auth/UserProvisioningService.cs`
- [X] T047 [P] Update execution notes and acceptance evidence in `specs/003-profile-settings-tab/quickstart.md`
- [X] T048 Run regression suites and record outcomes in `specs/003-profile-settings-tab/quickstart.md` for `tests/LoopMeet.App.Tests` and `tests/LoopMeet.Api.Tests`

---

## Dependencies & Execution Order

### Phase Dependencies

- Phase 1 (Setup): no dependencies
- Phase 2 (Foundational): depends on Phase 1; blocks all user stories
- Phase 3 (US1): depends on Phase 2
- Phase 4 (US2): depends on Phase 2 and uses US1 profile surface
- Phase 5 (US3): depends on Phase 2 and uses US1 profile surface
- Phase 6 (US4): depends on Phase 2 and integrates with US2 avatar precedence rules
- Phase 7 (Polish): depends on all selected user stories

### User Story Dependency Graph

- `US1 -> {US2, US3}`
- `US2 -> US4`
- `US3` has no required dependency on `US2`

Execution order by priority: `US1 (P1) -> US2 (P2) -> US3 (P3) -> US4 (P4)`

---

## Parallel Execution Examples

### User Story 1

```bash
# In parallel after Phase 2:
T013 (app tests)
T014 (API tests)
```

### User Story 2

```bash
# In parallel once US2 starts:
T023 (app tests)
T024 (API tests)
```

### User Story 3

```bash
# In parallel once US3 starts:
T031 (app tests)
T032 (API tests)
```

### User Story 4

```bash
# In parallel once US4 starts:
T040 (app tests)
T041 (API tests)
```

---

## Implementation Strategy

### MVP First (US1 Only)

1. Complete Phase 1 and Phase 2
2. Complete Phase 3 (US1)
3. Validate US1 independently before expanding scope

### Incremental Delivery

1. Deliver US1 (Profile tab + read summary)
2. Deliver US2 (display-name/avatar edits)
3. Deliver US3 (password modal)
4. Deliver US4 (social-avatar bootstrap rule)
5. Finish with Phase 7 cross-cutting checks

### Independent Validation Rule

Each user story phase includes required automated tests and an explicit independent test criterion; do not mark a story complete until its tests pass and its checkpoint criteria are verified.
