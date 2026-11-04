# Tasks: Account Deletion with a 30-Day Grace Period

**Input**: Design documents from `/specs/016-account-deletion/`
**Prerequisites**: plan.md (loaded), spec.md (4 user stories, FR-001–FR-021), research.md (D1–D15), data-model.md (tables, functions, policy changes), contracts/api-account-deletion.md, contracts/edge-functions.md, quickstart.md (13-row device matrix)

**Tests**: Included. Constitution Principle II makes tests a required deliverable. Each test sits at the layer that owns the behaviour:

- pgTAP for RLS, the request/restore functions and the purge SQL (new layer, D10)
- Deno tests for edge-function logic
- Api endpoint tests with `FixedTimeProvider` and in-memory fakes
- App unit tests for MAUI-free code linked into `LoopMeet.App.Tests`, and source-inspection tests for XAML and view-model wiring
- The quickstart device matrix for native and end-to-end behaviour

**Organization**: Tasks are grouped by user story. Phase 2 adds the deletion table, the "is this account active?" helpers, and the request and restore functions that every story uses.

**Refinements to the plan made here** (both keep the design and only change packaging):

1. The schema ships as **three ordered migrations** (core, visibility, purge) rather than one, so each story can be delivered and reverted on its own.
2. The app's confirmation rule and the 403 handler are **MAUI-free files linked into the test project** (the existing pattern for `ApiAuthHandler`), so they get real unit tests. The handler reports through a small `AccountDeletionSignal` class rather than an interface.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1–US4 per spec.md. Setup, Foundational and Polish phases have no story label.

## Path Conventions

- Database: `supabase/migrations/`, `supabase/tests/database/`
- Edge functions: `supabase/functions/`
- Backend: `src/LoopMeet.Core/`, `src/LoopMeet.Infrastructure/`, `src/LoopMeet.Api/`
- Client: `src/LoopMeet.App/`
- Tests: `tests/LoopMeet.Api.Tests/`, `tests/LoopMeet.App.Tests/`, plus `*.test.ts` next to the Deno modules

---

## Phase 1: Setup

**Purpose**: A known-good baseline, and a working local database test loop before any SQL is written.

- [X] T001 From the repo root, run `dotnet build LoopMeet.slnx -c Debug -p:SkipMaciOSTargets=true` and `dotnet test LoopMeet.slnx -c Debug -p:SkipMaciOSTargets=true`. Record the warning count and the passing and failing test counts as the baseline in this file's Notes section.
- [X] T002 [P] Run `deno lint supabase/functions/` and `deno test supabase/functions/` from the repo root and record the results in Notes.
- [X] T003 [P] Start the local stack with podman: `export DOCKER_HOST=…` as in quickstart.md, then `supabase start` and `supabase db reset`. Confirm every existing migration applies, and record the CLI version and any start-up flags needed (for example `-x studio,imgproxy,…`) in Notes.
- [X] T004 Add a `database-tests` job to `.github/workflows/tests.yml`:
  - `supabase/setup-cli@v1` pinned to the CLI version recorded in T003
  - `supabase start` excluding the services the tests don't need
  - `supabase test db`
  - the job fails on any pgTAP failure

  - then `supabase db lint --level error`, which fails the job on any error

  It runs alongside the existing `test` job. Commit it together with T006 so the job never runs with an empty test suite.

---

## Phase 2: Foundational: deletion record, active-account helpers, request and restore

**Purpose**: The state that every story reads. **No user-story work can begin until this phase is complete.**

### Database

- [X] T005 Create `supabase/migrations/20261103120000_account_deletion_core.sql`, following data-model.md:
  - **Table:** `account_deletions`, with an `on delete cascade` FK to `auth.users`, a check constraint on `status` (`pending` or `purging`), and RLS enabled with only `account_deletions_select_self`.
  - **Helpers:** `private.account_is_active`, `private.group_is_active` and `private.email_is_active` (`security definer`, `stable`, `set search_path = public`), after `create schema if not exists private` and `grant usage on schema private to authenticated, service_role`.
  - **Functions:** `request_account_deletion()`, which inserts with `now()` and `now() + interval '30 days'` on conflict do nothing, returns the row, and raises if `auth.uid()` is null. `restore_account_deletion() → boolean`, which deletes when pending and `purge_after > now()` and returns `true`, otherwise raises `deletion_in_progress` (errcode `P0001`, message `deletion_in_progress`), and returns `false` when there is no row.
  - **Grants:** revoke from `public`/`anon`; grant execute to `authenticated` and `service_role` as listed.
- [X] T006 [P] Create `supabase/tests/database/account_deletion_core.test.sql` (pgTAP, `begin; … rollback;`). Impersonate users with `set local role authenticated` and `set local request.jwt.claims`. Assert:
  - `request_account_deletion()` sets `purge_after - requested_at = interval '30 days'` exactly, and a second call returns the same row unchanged (FR-003, FR-005).
  - `restore_account_deletion()` removes a pending row and returns `true`, returns `false` with no row, and raises `deletion_in_progress` after `purge_after` and when `status = 'purging'` (FR-012).
  - A user can read only their own row. Direct `insert`, `update` and `delete` on `account_deletions` as `authenticated` fail.
  - `account_is_active`, `group_is_active` and `email_is_active` return false for a pending user, their owned group and their auth email (case-insensitive), and true otherwise.
  - `anon` can't execute either function.
  - The three helpers are in schema `private` (`select pronamespace::regnamespace from pg_proc where proname = 'account_is_active'` returns `private`), which PostgREST doesn't expose.

### Core, Infrastructure, test double

- [X] T007 [P] Add the Core types:
  - `src/LoopMeet.Core/Models/AccountDeletion.cs`: `AccountDeletion` record and `AccountDeletionStatus { Pending, Purging }`
  - `src/LoopMeet.Core/Accounts/AccountDeletionPolicy.cs`: `GracePeriod = TimeSpan.FromDays(30)`, with a doc comment that SQL is authoritative and both values are pinned by tests
  - `src/LoopMeet.Core/Interfaces/IAccountDeletionRepository.cs`: `GetAsync(Guid userId, …)`, `RequestAsync(…)`, `RestoreAsync(…)` and `AccountRestoreResult { Restored, NotPending, DeletionInProgress }`
- [X] T008 Create `src/LoopMeet.Infrastructure/Supabase/Models/AccountDeletionRecord.cs` (a Postgrest model for `account_deletions`) and `src/LoopMeet.Infrastructure/Repositories/AccountDeletionRepository.cs`.
  - `GetAsync` selects the caller's row.
  - `RequestAsync` calls `Rpc("request_account_deletion")`.
  - `RestoreAsync` calls `Rpc("restore_account_deletion")`, which returns `boolean`: `true` maps to `Restored` and `false` to `NotPending`. A `PostgrestException` whose message contains `deletion_in_progress` maps to `DeletionInProgress`.
  - Mapping uses a manual `Map()`, following the existing repositories.
- [X] T009 [P] Add `Dictionary<Guid, AccountDeletion> AccountDeletions` to `tests/LoopMeet.Api.Tests/Infrastructure/InMemoryStore.cs`. Create `tests/LoopMeet.Api.Tests/Infrastructure/InMemoryAccountDeletionRepository.cs`, which mirrors the SQL semantics using the injected `TimeProvider`: 30 days, idempotent request, restore refused after the deadline or while purging. Register it in `TestWebApplicationFactory.cs` (`RemoveAll` + `AddScoped`).
- [X] T010 Register `IAccountDeletionRepository` → `AccountDeletionRepository` in `src/LoopMeet.Api/Program.cs`, next to the other repositories.

**Checkpoint**: `supabase test db` and `dotnet test` pass. Nothing is user-visible yet.

---

## Phase 3: User Story 1: Request account deletion from the app (Priority: P1) 🎯 MVP

**Goal**: Profile → Delete Account shows the consequences, requires `DELETE`, records the request and signs the user out.

**Independent Test**: As a user who owns a group with members, request deletion. Check that the screen listed that group with the right member count, the button stayed disabled until `DELETE` was typed, the date is 30 days out, and the user is signed out (quickstart rows 1–3).

### Tests for User Story 1

- [X] T011 [P] [US1] Create `tests/LoopMeet.Api.Tests/Endpoints/AccountDeletionEndpointsTests.cs` (request half):
  - **Preview:** `GET /users/account-deletion/preview` lists owned groups by name with member counts including the owner, returns `[]` when the caller owns none, uses `deletesAt = clock + 30 days` before a request, and returns the stored date after one.
  - **Request:** `POST` with `"DELETE"` returns 202 and the pending body. Repeating it returns the original dates. `"delete"`, `"DELETE "`, `""` and a missing body return 400 `invalid_confirmation`. A purging row returns 409 `deletion_in_progress`.
  - **Status:** `GET /users/account-deletion` returns `none` and then `pending`.
- [X] T012 [P] [US1] Create `tests/LoopMeet.Api.Tests/Infrastructure/CapturingLogSink.cs` (a hand-rolled Serilog `ILogEventSink` fake) and register it as a singleton `ILogEventSink` in `TestWebApplicationFactory.cs`. `Program.cs` uses `UseSerilog` without `writeToProviders`, so an `ILoggerProvider` would see nothing; `.ReadFrom.Services(services)` picks up the sink. Add a test to `AccountDeletionEndpointsTests.cs` that asserts on the rendered messages and properties: the request writes `AccountDeletionRequested` with a `UserId` property, and no captured message or property value contains the caller's email (FR-020).
- [X] T013 [P] [US1] Create `tests/LoopMeet.App.Tests/Features/Account/AccountDeletionConfirmationTests.cs`: `IsConfirmed` is true only for `"DELETE"` and false for `null`, `""`, `"delete"`, `"Delete"`, `" DELETE"`, `"DELETE "` and `"DELETED"` (FR-002, SC-002). Link `src/LoopMeet.App/Features/Account/AccountDeletionConfirmation.cs` in `tests/LoopMeet.App.Tests/LoopMeet.App.Tests.csproj`.
- [X] T014 [P] [US1] Create `tests/LoopMeet.App.Tests/Features/Account/DeleteAccountSurfaceTests.cs`. These are source-inspection tests:
  - `DeleteAccountPage.xaml` binds an `Entry` to `Confirmation`, the delete button to `DeleteCommand` and `CanDelete`, and the owned groups through `BindableLayout.ItemsSource="{Binding OwnedGroups}"` (no `CollectionView`).
  - `DeleteAccountViewModel.cs` uses `AccountDeletionConfirmation.IsConfirmed`, `RequestAccountDeletionAsync` and `SignOutAsync(SignOutReason.UserInitiated)`.
  - `AppShell.xaml.cs` registers `"delete-account"`.
  - `ProfileViewModel.cs` navigates to `delete-account` and no longer contains "isn't available yet".

### Implementation for User Story 1: backend

- [X] T015 [P] [US1] Create `src/LoopMeet.Api/Contracts/AccountDeletionContracts.cs` with `AccountDeletionRequest`, `AccountDeletionStatusResponse`, `AccountDeletionPreviewResponse`, `OwnedGroupSummary` and `AccountPendingDeletionResponse` (`Code`, `Message`, `DeletesAt`), as in contracts/api-account-deletion.md.
- [X] T016 [US1] Create `src/LoopMeet.Api/Services/Accounts/AccountDeletionService.cs` with a result enum, `AccountDeletionCommandResult(AccountDeletionCommandStatus, AccountDeletion?)`:
  - `GetStatusAsync`, returning the *effective* status: `Purging` when the stored status is `Pending` and `DeletesAt <= TimeProvider.GetUtcNow()` (`AccountDeletionService.EffectiveStatus`, also used by the preview and the gate)
  - `GetPreviewAsync`, which uses `IGroupRepository.ListOwnedAsync` and `IMembershipRepository.ListMembersByGroupsAsync`, ordered by name, with `AccountDeletionPolicy.GracePeriod` and `TimeProvider`
  - `RequestAsync(confirmation)`, which uses ordinal `"DELETE"`, removes cache key `account-deletion:{userId}` via `ICacheService`, and logs `AccountDeletionRequested` or `AccountDeletionAlreadyPending` with `{UserId}` and `{DeletesAt}`
- [X] T017 [US1] Create `src/LoopMeet.Api/Endpoints/AccountDeletionEndpoints.cs` with `GET /users/account-deletion`, `GET /users/account-deletion/preview` and `POST /users/account-deletion` (`RequireAuthorization`, `ErrorResponse` for 400 and 409). Register `AccountDeletionService` and call `app.MapAccountDeletionEndpoints()` in `src/LoopMeet.Api/Program.cs`.

### Implementation for User Story 1: client

- [X] T018 [P] [US1] Create `src/LoopMeet.App/Features/Account/AccountDeletionConfirmation.cs`, MAUI-free: `public static bool IsConfirmed(string? text) => string.Equals(text, "DELETE", StringComparison.Ordinal);`
- [X] T019 [P] [US1] Create `src/LoopMeet.App/Features/Account/Models/AccountDeletionModels.cs` with the status, preview, owned-group and request DTOs. Add `GetAccountDeletionAsync`, `GetAccountDeletionPreviewAsync`, `RequestAccountDeletionAsync` and `RestoreAccountAsync` to `IUsersApi` and to the `UsersApi` wrapper in `src/LoopMeet.App/Services/UsersApi.cs`.
- [X] T020 [US1] Create `src/LoopMeet.App/Features/Account/ViewModels/DeleteAccountViewModel.cs` (`sealed partial class : ObservableObject`):
  - It loads the preview on appearing into `OwnedGroups`, an `ObservableCollection` updated on the main thread.
  - `Confirmation` drives `CanDelete` via `AccountDeletionConfirmation.IsConfirmed`.
  - `DeleteAsync` posts the request. On success it shows "Your account will be permanently deleted on {local date}". On failure it shows "Couldn't schedule the deletion. Check your connection and try again." and stays signed in (US1 scenario 5).
  - `AcknowledgeAsync` calls `SessionCoordinator.SignOutAsync(SignOutReason.UserInitiated)`.
- [X] T021 [US1] Create `src/LoopMeet.App/Features/Account/Views/DeleteAccountPage.xaml(.cs)`:
  - The explanation, then the owned-group list ("{name} · {n} member(s)") inside a `ScrollView` with `BindableLayout`, then the `DELETE` entry and a destructive button.
  - After success, the date and an OK button.
  - Before using any style or colour key, confirm it exists in `Resources/Styles/Colors.xaml` and `Styles.xaml`.
- [X] T022 [US1] Register the `delete-account` route in `src/LoopMeet.App/AppShell.xaml.cs` and the page and view model in `src/LoopMeet.App/MauiProgram.cs`. Change `DeleteAccountAsync` in `src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs` to `Shell.Current.GoToAsync("delete-account")`.

**Checkpoint**: US1 is complete. Quickstart rows 1–3 pass on Android staging.

---

## Phase 4: User Story 2: Everyone else stops seeing the user straight away (Priority: P1)

**Goal**: From the request onward, the pending user and everything they own are invisible to everyone, no pushes involve them, and their own sessions are confined to the deletion endpoints.

**Independent Test**: With A pending, B and C no longer see A's group, its meetups, A's membership or A's invitations, and no push or reminder involves A (quickstart rows 4, 5, 12).

### Tests for User Story 2

- [X] T023 [P] [US2] Create `supabase/tests/database/account_deletion_visibility.test.sql`.
  - **Fixture:**
    - users A, B and C, with profiles
    - group G owned by A, with members A and B, a meetup, and a pending invitation A → B
    - group H owned by C, with members C and A, plus a meetup created by A in H
    - an invitation from C to A's email
  - **With A pending, as B and as C:** G, G's meetups, G's memberships, A's membership in H, A's meetup in H, the A → B invitation, the invitation to A's email and A's profile are all invisible. `update` and `delete` on G's meetup affect 0 rows (FR-006, FR-007).
  - **As A:** inserts into `groups`, `meetups`, `invitations`, `memberships` and `user_profiles` fail (FR-009).
  - **Restore:** after `restore_account_deletion()` as A, every item is visible again with identical column values (FR-012).
  - **Control:** an unrelated active user's data stays visible throughout.
- [X] T024 [P] [US2] Add assertions to `account_deletion_visibility.test.sql` for `active_memberships`: it excludes A's memberships and every membership of G while A is pending, includes them again after restore, and can't be selected by `authenticated` (FR-008).
- [X] T025 [P] [US2] Create `supabase/functions/_shared/account-status.test.ts` (`isAccountActive` against a fake client) and `supabase/functions/notifications-dispatch/recipient-resolver.test.ts`:
  - Meetup recipients come from `active_memberships`.
  - An invitation to a pending user returns `[]`, and one to an active user returns that user.
  - Use a fake client passed in as a parameter, and import `jsr:@std/assert@1` directly.
- [X] T026 [P] [US2] Add a Deno source test, `supabase/functions/reminders-scheduler/recipients-source.test.ts`, asserting that `index.ts` reads `.from("active_memberships")` and not `.from("memberships")`. The scheduler's handler isn't injectable, and this is the repo's accepted source-inspection pattern.
- [X] T027 [P] [US2] Create `tests/LoopMeet.Api.Tests/Endpoints/AccountDeletionGateTests.cs`:
  - A pending caller gets 403 `account_pending_deletion` with `deletesAt` on `/groups` and `/users/profile`.
  - The same caller gets 200 on all three `/users/account-deletion*` routes.
  - An active caller and unauthenticated routes are unaffected.
  - Right after `POST /users/account-deletion`, the next `/groups` call is already 403, because the cache key was invalidated.

### Implementation for User Story 2

- [X] T028 [US2] Create `supabase/migrations/20261103120100_account_deletion_visibility.sql`:
  - Drop and recreate every policy listed in data-model.md "Policy changes", with the existing predicate intact AND the added condition. Keep the existing `service_role` branch on memberships insert.
  - Create the `active_memberships` view: revoke from `public`, `anon` and `authenticated`; grant select to `service_role`.
- [X] T029 [P] [US2] Create `supabase/functions/_shared/account-status.ts` exporting `isAccountActive(client, userId)`.
- [X] T030 [US2] Change `supabase/functions/notifications-dispatch/recipient-resolver.ts`:
  - `resolveRecipients(client, input)` takes the client as a parameter.
  - Meetups read `active_memberships`.
  - Invitations return `[]` unless `isAccountActive(invited_user_id)`.

  Update the call in `supabase/functions/notifications-dispatch/index.ts` to pass its service-role client.
- [X] T031 [US2] In `supabase/functions/reminders-scheduler/index.ts`, change `fetchRecipients` to read `active_memberships`.
- [X] T032 [US2] Create `src/LoopMeet.Api/Services/Accounts/AccountDeletionGateMiddleware.cs`.
  - For authenticated requests whose path doesn't start with `/users/account-deletion`, it reads the caller's status through `ICacheService.GetOrSetAsync("account-deletion:{userId}", 30 s, …)`.
  - When the status is pending or purging, it writes 403 `AccountPendingDeletionResponse` and logs `AccountDeletionGateBlocked` at debug level.

  Add `app.UseMiddleware<AccountDeletionGateMiddleware>()` after `app.UseAuthorization()` in `src/LoopMeet.Api/Program.cs`.
- [X] T033 [US2] Review every repository update, delete and accept path (`MeetupRepository`, `InvitationRepository`, `MembershipRepository`, `GroupRepository`) to confirm that a zero-row Postgrest result maps to not-found or a `*CommandStatus.NotFound` result rather than an exception (FR-007). Fix any path that throws, and record the findings in Notes.

**Checkpoint**: US2 is complete. Quickstart rows 4, 5 and 12 pass.

---

## Phase 5: User Story 4: Permanent deletion happens automatically after 30 days (Priority: P1)

**Goal**: An hourly job removes everything tied to a due user, including OneSignal and Apple, in resumable steps.

**Independent Test**: Force a pending user's date into the past and run the purge. No record remains in any store, the push provider no longer knows the user, and a second run does nothing (quickstart rows 9–11; row 13 on iOS later).

### Tests for User Story 4

- [X] T034 [P] [US4] Create `supabase/tests/database/account_deletion_purge.test.sql`, using the T023 fixture plus devices, delivery attempts, open events and an `apple_authorizations` row for A:
  - `purge_account_data(A)` leaves 0 rows for A in every table listed in data-model.md, and leaves H, C's data and B's other data intact.
  - A second call succeeds and changes nothing.
  - Deleting A from `auth.users` cascades away `account_deletions` and `apple_authorizations`.
  - `claim_due_account_purges` claims only due rows with a free lease, and sets `purging`, `claimed_at` and `attempts + 1`.
  - `record_account_purge_step` appends steps in order and records `last_error` while releasing the lease.
  - `authenticated` can't execute the claim, record or purge functions (FR-014, FR-017, FR-018, FR-019).
- [X] T035 [P] [US4] Create `supabase/functions/account-purge/purge-runner.test.ts` with fake `PurgePorts`. Cover:
  - **Order:** steps run in order on a fresh row.
  - **Resume:** a failure injected at each step resumes at that step on the next run.
  - **Done conditions:** OneSignal `404` counts as done. A storage listing that returns two pages is emptied.
  - **Apple:** with no Apple rows but an Apple identity, the run logs `apple_token_unavailable` and completes.
  - **Logging:** `account_purge_stuck` is logged when `attempts >= 24`. No logged line contains `@`.
  - **Summary and isolation:** the summary counts are correct, and one user's failure doesn't stop the others.
- [X] T036 [P] [US4] Create `supabase/functions/_shared/apple-auth.test.ts`:
  - `createClientSecret` produces an ES256 JWT with `kid`, `iss`, `aud=https://appleid.apple.com`, `sub` and a 300 s expiry, whose signature verifies against a P-256 key generated in the test.
  - The exchange and revoke requests send the documented form fields to the documented URLs (fake `fetch`).
  - Errors carry the HTTP status, not the body.
- [X] T037 [P] [US4] Create `tests/LoopMeet.App.Tests/Features/Auth/AppleAuthorizationSurfaceTests.cs`, source-inspection: `AuthService.SignInWithAppleAsync` passes `credential.AuthorizationCode` to `AppleAuthorizationRegistrar` after `SignInWithIdToken` succeeds, without awaiting it on the sign-in path, and the registrar catches and logs every exception.
- [X] T038 [P] [US4] Create `tests/LoopMeet.Api.Tests/Contract/PrivilegedAccessTests.cs`, source-inspection: no file under `src/LoopMeet.Api/` other than `Services/Configuration/` references `ServiceOrSecretKey` (FR-019).

### Implementation for User Story 4

- [X] T039 [US4] Create `supabase/migrations/20261103120200_account_deletion_purge.sql`:
  - **Table:** `apple_authorizations`, with RLS enabled and no policies.
  - **Functions:** `claim_due_account_purges(p_limit)` (`for update skip locked`, 15-minute lease), `record_account_purge_step(...)` and `purge_account_data(p_user_id)`, deleting in the order in data-model.md. Execute is granted to `service_role` only.
  - **Cron:** `cron.schedule('account-purge-hourly', '7 * * * *', …)`, using the same vault secrets as `20260515110000_schedule_reminders_every_5_minutes.sql`. Guard it so re-running the migration doesn't duplicate the job.
- [X] T040 [P] [US4] Create `supabase/functions/_shared/apple-auth.ts` with `createClientSecret`, `exchangeAuthorizationCode` and `revokeRefreshToken` (WebCrypto `importKey('pkcs8', …)` from the `.p8` PEM, ES256 via `sign({name:'ECDSA', hash:'SHA-256'})`, raw signature encoding), taking an injectable `fetch`.
- [X] T041 [US4] Create `supabase/functions/account-purge/purge-runner.ts`: `runPurge(ports, now)` and the `PurgePorts` interface from contracts/edge-functions.md, with the step order, recording, stop-on-failure per user and the logging events.
- [X] T042 [US4] Create `supabase/functions/account-purge/index.ts` and `deno.json`, wiring the real ports:
  - Supabase `rpc` calls for claim, record and purge
  - storage `list` and `remove` on `AVATAR_BUCKET_NAME` under `{user_id}/`
  - `auth.admin.getUserById` for the Apple identity check and `auth.admin.deleteUser`, treating "not found" as done
  - the OneSignal `DELETE` by external id
  - the `apple-auth` revoke, plus deleting each revoked row
- [X] T043 [P] [US4] Create `supabase/functions/apple-authorization/handler.test.ts` (fake ports, `jsr:@std/assert@1`):
  - a missing or invalid bearer token returns 401
  - a body with neither `authorizationCode` nor `refreshToken`+`clientId` returns 400
  - a code is exchanged, then upserted with `APPLE_CLIENT_ID`
  - a refresh token is upserted as is with its `clientId`
  - an Apple failure returns 502 `apple_exchange_failed`
  - no logged line contains a token or `@`
- [X] T044 [US4] Create `supabase/functions/apple-authorization/handler.ts`, `handleAppleAuthorization(request, ports)`, where `ports` are `getUser`, `exchangeCode` and `upsertAuthorization`. `index.ts` and `deno.json` only wire the real ports. The handler must:
  - Verify the bearer token with `getUser`.
  - Accept `{authorizationCode}`, which is exchanged, or `{refreshToken, clientId}`, used as is.
  - Upsert `apple_authorizations` on `(user_id, client_id)`.
  - Return `204`, `400`, `401` or `502`, and log as in the contract.
- [X] T045 [US4] Create `src/LoopMeet.App/Features/Auth/AppleAuthorizationRegistrar.cs`. `RegisterAsync(string authorizationCode)` calls `Supabase.Client.Functions.Invoke("apple-authorization", …)` with the current access token, and catches and logs failures. In `src/LoopMeet.App/Features/Auth/AuthService.cs`, inside `#if IOS || MACCATALYST`, after a successful `SignInWithIdToken`, start `_ = _appleAuthorizationRegistrar.RegisterAsync(code)` when `credential.AuthorizationCode` is present. Register the registrar in `src/LoopMeet.App/MauiProgram.cs`.

**Checkpoint**: US4 is complete. Quickstart rows 9–11 pass on staging (row 13 waits for iOS).

---

## Phase 6: User Story 3: Change one's mind within 30 days (Priority: P2)

**Goal**: A pending user who signs in sees only the scheduled-deletion screen, and only Restore account cancels the deletion.

**Independent Test**: Request deletion, sign in and choose Sign out (still pending), then sign in and choose Restore. Everything is back and visible to the other members (quickstart rows 6–8).

### Tests for User Story 3

- [X] T046 [P] [US3] Add restore tests to `tests/LoopMeet.Api.Tests/Endpoints/AccountDeletionEndpointsTests.cs`:
  - `DELETE /users/account-deletion` returns 200 `none`, and again when nothing is pending.
  - It returns 409 `deletion_in_progress` once the clock passes `deletesAt`, and while the status is purging.
  - After a restore, the next `/groups` call succeeds immediately (gate cache invalidated).
  - The restore writes `AccountDeletionRestored` or `AccountDeletionRestoreRefused` with `UserId`.
  - With the clock past `deletesAt` but the stored status still `pending`, `GET /users/account-deletion` returns `purging`, and the gate's 403 still carries `deletesAt`.
- [X] T047 [P] [US3] Create `tests/LoopMeet.App.Tests/Services/AccountStatusHandlerTests.cs`:
  - A 403 whose body code is `account_pending_deletion` raises `AccountDeletionSignal.PendingDeletion` with the parsed `deletesAt`.
  - Other 403s, 401 and 200 raise nothing.
  - The response returned is the original, unchanged.

  Link `src/LoopMeet.App/Services/AccountStatusHandler.cs` and `src/LoopMeet.App/Features/Account/AccountDeletionSignal.cs` in the test csproj.
- [X] T048 [P] [US3] Create `tests/LoopMeet.App.Tests/Features/Account/PendingDeletionSurfaceTests.cs`, source-inspection:
  - `ApiClient.cs` adds `AccountStatusHandler` after `ApiAuthHandler`.
  - `ApiAuthHandler.cs` still contains no `GoToAsync`.
  - `AppShell.xaml` declares a `pending-deletion` `ShellContent`.
  - `PendingDeletionViewModel.cs` exposes exactly `RestoreCommand` and `SignOutCommand` and uses `RestoreAccountAsync`.
  - `PendingDeletionPage.xaml` binds the Restore button's `IsVisible` to `IsRestoreAvailable`.
  - `AccountDeletionGate.cs` navigates with `GoToAsync("//pending-deletion` inside `MainThread`, and guards against re-entry until `Reset()`.

### Implementation for User Story 3

- [X] T049 [US3] Add `RestoreAsync` to `src/LoopMeet.Api/Services/Accounts/AccountDeletionService.cs` (invalidate the cache key; log the outcome) and `DELETE /users/account-deletion` to `src/LoopMeet.Api/Endpoints/AccountDeletionEndpoints.cs` (200 or 409 `ErrorResponse`).
- [X] T050 [P] [US3] Create these MAUI-free files:
  - `src/LoopMeet.App/Features/Account/AccountDeletionSignal.cs`: a singleton with `event Action<DateTimeOffset>? PendingDeletion` and `Raise`.
  - `src/LoopMeet.App/Services/AccountStatusHandler.cs`: a `DelegatingHandler` that, on a 403, buffers and parses the body's `code` and `deletesAt` and raises the signal.

  Add `.AddHttpMessageHandler<AccountStatusHandler>()` after `ApiAuthHandler` in `src/LoopMeet.App/Services/ApiClient.cs`, and register both classes in `MauiProgram.cs`.
- [X] T051 [US3] Create `src/LoopMeet.App/Features/Account/AccountDeletionGate.cs`. It subscribes to the signal. On the first signal of an episode it calls `MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync($"//pending-deletion?deletesAt=…"))`, and ignores repeat signals until `Reset()`. Resolve it at app start in `MauiProgram.cs` so it subscribes before the first Api call.
- [X] T052 [US3] Create `src/LoopMeet.App/Features/Account/ViewModels/PendingDeletionViewModel.cs` and `Views/PendingDeletionPage.xaml(.cs)`:
  - The page shows "Your account is scheduled for deletion on {local date}" and only two buttons.
  - `RestoreAsync` calls `RestoreAccountAsync`, then `AccountDeletionGate.Reset()`, then navigates to `SignedInTabs.HomeShellPath`. A 409 shows "Your account can no longer be restored." and then signs out.
  - `SignOutAsync` calls `Reset()` and then `SessionCoordinator.SignOutAsync(SignOutReason.UserInitiated)`.
  - When the status is `purging`, the page shows "Your account is being deleted." with only Sign out (`IsRestoreAvailable = false`).

  Add the `pending-deletion` `ShellContent` to `src/LoopMeet.App/AppShell.xaml` alongside `login`, hidden from the tab bar, and register the page and view model in `MauiProgram.cs`.
- [X] T053 [US3] In `src/LoopMeet.App/Features/Auth/ViewModels/LoginViewModel.cs`, the email, Google and Apple sign-in paths catch an `ApiException` whose body code is `account_pending_deletion` (helper `AccountStatusHandler.IsPendingDeletion(ApiException)`) before their generic `catch`. They return without an error message or navigation, because the gate has already navigated. Add the assertion to `PendingDeletionSurfaceTests.cs` (T048).

**Checkpoint**: All stories are complete. Quickstart rows 1–12 pass on Android staging.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T054 Write `specs/016-account-deletion/data-retention.md` (FR-021):
  - The grace period, and what is hidden during it.
  - Exactly what is deleted, and when: within 1 hour of the date, at most 24 hours.
  - That replaced avatars stay in storage until account deletion.
  - That nothing identifying is kept after completion.
  - The provider-held copies, with actual values read from the dashboards: the Supabase plan's backup and PITR window, Supabase log retention, DigitalOcean App Platform log retention, and OneSignal's asynchronous deletion.

  This document is the source text for the 017 privacy policy.
- [X] T055 [P] Add Known-gotchas entries to `CLAUDE.md`:
  - The 30-day value lives in SQL (`request_account_deletion`) and in `AccountDeletionPolicy`, and tests pin both.
  - Edge functions must resolve recipients from `active_memberships`, never `memberships`.
  - New RLS policies must AND `private.account_is_active` / `private.group_is_active`.
  - `supabase test db` needs `DOCKER_HOST` pointed at podman locally.
- [X] T056 [P] Run `deno lint supabase/functions/`, `deno test supabase/functions/`, `supabase test db` and the full `dotnet test`. Compare against the T001–T003 baselines in Notes. No new warnings.
- [X] T057 Deploy to staging per the quickstart.md checklist (migrations, secrets including `AVATAR_BUCKET_NAME`, functions, cron check, an Api `staging/**` tag, an app build) and fill in the bucket-name table.
- [ ] T058 Run quickstart.md rows 1–12 on Android staging and record the results. Row 13 (iOS Apple revocation) stays open until 013 can be tested on a device. Record the Google Play closed-testing review outcome for the delete-account path (SC-007).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: none. T004 depends on T003's recorded CLI version.
- **Foundational (Phase 2)**: depends on Setup and blocks every story.
- **US1 (Phase 3)**: depends on Phase 2 only.
- **US2 (Phase 4)**: depends on Phase 2. Its gate tests (T027) use US1's `POST` endpoint, so do T017 first, or seed the fake repository directly.
- **US4 (Phase 5)**: depends on Phase 2. The T034 fixture reuses T023's shape. Write T023 first, or copy the fixture.
- **US3 (Phase 6)**: depends on Phase 2. The pending screen is only reachable once US2's gate (T032) exists.
- **Polish (Phase 7)**: after the stories it covers. T054 needs US4's final purge behaviour.

### Story-Level Parallelism

After Phase 2, US1 (Api, App), US2 (SQL, edge, Api middleware) and US4 (SQL, edge, App registrar) touch mostly separate files and can proceed in parallel. US3 follows US2.

### Within-Phase Parallelism

- Tests marked [P] in a phase can be written together before that phase's implementation.
- The three migrations are sequential files but independent tasks (T005, T028, T039). Each must apply cleanly on top of the previous ones.

### Parallel example: start of US2

```text
T023 visibility pgTAP     | T025 Deno account-status + resolver tests | T027 Api gate tests
then: T028 migration → T029/T030/T031 edge changes | T032 middleware
```

## Implementation Strategy

### MVP first

Phase 1 → Phase 2 → US1 → US2 → US4. This is the store-compliant core: users can request deletion, they disappear immediately, and their data is really deleted. US3 (restore) follows straight after. Don't ship a build to closed testing without US3, because a pending user signing in would see an empty app.

### Incremental delivery

Each story ends at a checkpoint with passing tests and quickstart rows. Each migration can be applied to staging independently, in order.

### Delivery (one PR per checkpoint)

1. **PR A:** Phase 1 + Phase 2 + US1 (T001–T022)
2. **PR B:** US2 (T023–T033)
3. **PR C:** US4 (T034–T045)
4. **PR D:** US3 + Polish (T046–T058)

Each PR is merged to `main` only after its checkpoint passes. PR A–C may be deployed to staging, but no closed-testing build ships before PR D.

### Risk notes

- **RLS rewrites (T028)** are the highest-risk change: a dropped predicate could expose data. T023 must include the "unrelated active user still sees their own data" control, and a diff review of each recreated policy against its original is required before merging.
- **Apple revocation** can't be verified end to end until iOS builds work (013). The Deno tests pin the protocol, and quickstart row 13 stays open.
- **The local stack under podman** may need extra configuration. T003 records whatever was needed.

## Notes

- Mark tasks `[X]` in place as they complete (living execution log).
- Record baselines (T001–T003), repository review findings (T033) and provider retention values (T054) here.

### Baselines (2026-11-03)

- **T001**: `dotnet build` 4 warnings, 0 errors. `dotnet test` 227 passed, 0 failed (Core 29, Infrastructure 1, App 126, Api 71).
- **T002**: Deno 2.9.8. `deno lint` clean (16 files). `deno test` 34 passed, 0 failed.
- **T003**: Supabase CLI 3.0.0, podman 6.0.2. `/var/run/docker.sock` already links to the podman machine socket, so `DOCKER_HOST` isn't needed on this machine. No start-up flags needed locally. CI starts with `-x realtime,storage-api,imgproxy,kong,mailpit,postgrest,postgres-meta,studio,edge-runtime,logflare,vector,supavisor`.
  - Applied with `supabase migration up` instead of `db reset`, to keep local data.
  - **Fixed two existing migrations so a fresh database can apply them.** The production project has both recorded as applied, so it never re-runs them.
    - `20260511103000` closed its `$cron$` string with `$$` and didn't parse.
    - `20260511103000` and `20260515110000` raised when the vault secrets were missing. They now skip scheduling with a notice, so local and CI databases get no cron job.
  - `supabase db lint` exits 0 by default even with errors; CI passes `--fail-on error`.
- **T008**: `Client.Rpc<T>` ignores `[Column]` names, so a composite row comes back all defaults. `RequestAsync` parses the RPC JSON with explicit snake_case names. Verified all four repository paths against the local stack.

### US1 notes (PR A)

- **T012**: `UseSerilog` without `preserveStaticLogger` routes every host's `ILogger` through the static `Log.Logger`, and the most recently built host replaces it. Parallel test classes therefore leak events into each other's sinks. Classes that assert on logs join `[Collection(CapturedLogsCollection.Name)]`, which disables parallelization. A mutation check confirmed the no-email assertion fails when an email is logged.
- **T019**: only `GetAccountDeletionPreviewAsync` and `RequestAccountDeletionAsync` were added. `GetAccountDeletionAsync` and `RestoreAccountAsync` move to T050/T052, where they're first used, so PR A has no unused client methods.
- **T020/T021**: once the deletion is scheduled, the page hides the back button and swallows hardware back, so OK (sign out) is the only way out. The delete button keeps the default Button style, so its Disabled visual state still applies. There's no destructive colour key in `Colors.xaml`.
- **Checkpoint**: quickstart rows 1–3 on Android staging need the core migration on staging, an Api `staging/**` tag and an app build.

### US2 notes (PR B)

- **T028**:
  - The recreated policies apply `to authenticated`, where before they applied to every role. anon has no usage on `private`, so with the helpers added an anon query would have failed instead of returning nothing. With no policy, anon still sees nothing; pgTAP asserts this.
  - The memberships insert policy loses its `current_setting('role') = 'service_role'` branch. service_role has `bypassrls`, so that branch never decided anything.
  - A pending user can't see their own groups any more, so their preview lists no owned groups. Nothing uses the preview after the request.
- **T023/T024**: the restore check compares every row each viewer can see (as `to_jsonb`) before the request and after restore, so any changed column value fails it.
- **T025/T029/T030**:
  - `isAccountActive` and `resolveRecipients` take the service-role client as a parameter and type it with `import type { SupabaseClient } from "npm:@supabase/supabase-js@2"`.
  - The full `npm:` specifier is needed because `deno test` from the repo root ignores the per-function import maps.
  - A hand-written structural client type doesn't work: comparing it with supabase-js's generics fails with TS2589. The fake lives in `_shared/fake-query-client.ts` and casts to `SupabaseClient`.
- **T026**: CI's `deno test` now runs with `--allow-read=supabase/functions` so the source test can read `index.ts`. A mutation check (reverting to `.from("memberships")`) fails it.
  - `deno check` on `reminders-scheduler/index.ts` already reports TS2352 on `fetchTodayCandidateMeetups` on `main`. It's unrelated to this feature and left alone.
- **T032**: the middleware resolves `AccountDeletionService` from `RequestServices` only for authenticated requests. Injecting it into `InvokeAsync` would build the scoped Supabase client, which throws without a bearer token, on every unauthenticated request. The cached read is `AccountDeletionService.GetCachedAsync`, next to `CacheKey`.
- **T033**: repository update and delete calls ignore the returned rows, so a zero-row result never throws. The command services load the row first, so a hidden row becomes `NotFound`. This covers group rename, meetup update and delete, and invitation create, accept and decline.
  - **Fixed**: `MeetupCommandService.CreateAsync` didn't check the group. In a group hidden by RLS, the insert check rejected the row and the request failed with 500. It now returns 404 when `GetByIdAsync` finds no group. Regression test: `CreateInAGroupTheCallerCannotSeeReturnsNotFound`.
  - `create_membership_from_invitation` is security definer and bypasses RLS. A pending user could reach it only with an accepted invitation and no membership row, and the Api gate stops them first. It's unchanged.
- **Checkpoint**: quickstart rows 4, 5 and 12 need the visibility migration on staging, an Api `staging/**` tag, and a redeploy of `notifications-dispatch` and `reminders-scheduler`.

### US4 notes (PR C)

- **T039**:
  - `record_account_purge_step` rejects an unknown step with `22023`. Recording a step twice leaves one entry.
  - A successful step doesn't clear `last_error`, so the value from the failure before is still there for diagnosis.
  - The cron job is skipped on databases without the vault secrets (local, CI), as in the reminders migration.
  - `supabase/config.toml` sets `verify_jwt = false` for both new functions. `account-purge` is triggered with the publishable key, as `reminders-scheduler` is. `apple-authorization` checks the caller's token itself with `auth.getUser`.
- **T041**:
  - `runPurge(ports)` takes no `now`, a deviation from contracts/edge-functions.md. Whether a row is due is decided in SQL with `now()`, so the runner had no use for it.
  - Two log events beyond the contract: `account_purge_record_failed`, when recording a step throws (the lease expires and a later run resumes), and `account_purge_claim_failed`, when the claim fails (the run returns 500).
  - The storage step stops with `storage_incomplete` after 50 list/remove rounds.
  - Error codes are an HTTP status, a short code from the error object, or `exception`, never a message.
- **T042**: OneSignal counts any `2xx` or `404` as done. `AVATAR_BUCKET_NAME` unset fails the step with `storage_bucket_unset` instead of skipping it.
- **T044**:
  - The ports also carry `nativeClientId` and `log`.
  - A failed upsert returns `500 {"code":"store_failed"}`, which the contract doesn't list.
  - Errors that aren't from Apple, such as network failures, log `status: "exception"` and return 502.
- **T045**:
  - The registrar logs only the exception type, never the code or token.
  - The iOS build compiles with `-p:ValidateXcodeVersion=false`. Installed .NET for iOS 27.1 expects Xcode 27.1, and 27.2 is installed (the 013 tooling issue).
- **Extra test**: `webhook-router.test.ts` asserts that invitation `DELETE` events never notify. Purge deletes invitations directly and by cascade.
- **Checkpoint**:
  - apply `20261103120200_account_deletion_purge.sql` on staging
  - set the `APPLE_*` and `AVATAR_BUCKET_NAME` secrets; the purge works without the Apple ones as long as no `apple_authorizations` rows exist
  - deploy `account-purge` and `apple-authorization`
  - confirm the `account-purge-hourly` job
  - run quickstart rows 9–11

### US3 + Polish notes (PR D)

- **T046/T049**:
  - Restore always clears the gate cache entry, refused or not.
  - `RestoreAsync` returns the same `none` body whether a deletion was removed or nothing was pending.
  - `AccountDeletionRestoreRefused` is logged at warning level.
- **T050/T053**:
  - `AccountStatusHandler.IsPendingDeletion` takes the body string (`ApiException.Content`) rather than an `ApiException`. That keeps Refit out of the MAUI-free handler, and the test project needs no Refit package.
  - The handler buffers the 403 body so Refit can still read it.
- **T051**:
  - The gate passes `deletesAt` as a `GoToAsync` parameter dictionary (the CLAUDE.md navigation convention) instead of a query string.
  - A failed navigation resets the gate so the next 403 retries.
- **T052**:
  - The page reloads `GET /users/account-deletion` each time it appears. Past the deletion date the account reads as `purging`, and only Sign out is shown.
  - After a restore, the view model caches the profile and runs `HandleSuccessfulSignInAsync` before going home, because sign-in stopped at the 403 before doing either. Both are best effort.
  - Back is disabled on the page.
- **T054**: `data-retention.md` has the provider values from the dashboards: Supabase backups 7 days, Supabase logs 7 days, DigitalOcean App Platform logs not stored.
  - `ShowPII` was on in every environment, so Api token-validation logs could contain an email. It's now set only in Development, and `LoggingPrivacyTests` checks that.
- **T056**:
  - All suites pass: pgTAP 101, Deno 69 with lint clean, Api 103, App 165, Core 29, Infrastructure 1.
  - `account_deletion_purge.test.sql`'s "remains" checks now read only the fixture's ids. Rows already in a local database made the profile count fail.
  - Locally, `supabase test db` failed until it ran with an empty `DOCKER_CONFIG`: the gcloud credential helper's token had expired. This is environmental.
- **Builds**: Android, and the iOS simulator with `--artifacts-path` set to a scratch folder so the project's `obj` isn't touched.
- **Checkpoint**:
  - Rows 6–8 need an Api `staging/**` tag (for the restore endpoint) and an app build.
  - They also need a fresh test account, because A was permanently deleted in row 9.
- **T057/T058**:
  - Staging has every migration, both new edge functions, `AVATAR_BUCKET_NAME=avatars`, the `account-purge-hourly` cron job, the Api and app 0.1.3.
  - Quickstart rows 1–12 pass on Android staging (2026-11-04). Row 11 first exposed an existing email sign-up crash, fixed in #23.
  - T058 stays open for row 13 (iOS Apple revocation, which needs the Apple secrets and 013) and for the Google Play review outcome of the delete-account path (SC-007).
