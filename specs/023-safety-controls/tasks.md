# Tasks: Safety Controls (Report, Leave, Block)

**Input**: design documents in `/specs/023-safety-controls/`: [plan.md](plan.md), [spec.md](spec.md), [research.md](research.md), [data-model.md](data-model.md), [contracts/](contracts/), [quickstart.md](quickstart.md).

**Tests**: required (Constitution Principle II). In every phase, the tests are written first and must fail before the code that satisfies them is written.

**Delivery**: one branch and one pull request per story, each from `023-safety-controls` and each into `main`, in the order US1, US2, US3.

- **Self-contained:** each phase holds its own migration, contracts code and tests, so its pull request stands alone. There is no shared foundational phase.
- **Before branching:** before creating each story branch, update `023-safety-controls` from `main`, so the branch has the previous story.
- **Commits:** commit, push and open pull requests only after the maintainer confirms.

**This file is a living log**: tick `[X]` in place as each task completes, on the story branch that does it.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no dependency on an unfinished task).
- **[Story]**: US1, US2 or US3.

---

## Phase 1: Feature branch

**Purpose**: put the spec documents on `023-safety-controls`, so every story branch starts from them.

- [X] T001 Commit `specs/023-safety-controls/` (spec, plan, research, data model, contracts, quickstart, checklist, design, tasks) on `023-safety-controls` and push it, once the maintainer confirms. Leave the untracked `plan/child-safety/` alone.

**Checkpoint**: `023-safety-controls` on the remote holds the documents and no code. There's no foundational phase: anything shared lives in the first story that needs it.

---

## Phase 2: User Story 1 - Report a concern (Priority: P1) 🎯 MVP

**Goal**: a user files a report from Profile or from a member tile. It's recorded through the Api, and emailed to `abuse@loopmeet.io` through SendGrid within 5 minutes, with a mail-app fallback.

**Independent Test**: quickstart rows 1–10 on staging. Reports from Profile and from a member arrive at the safety inbox. With the network off, the report goes through the mail app.

- [X] T002 [US1] Create branch `023-safety-controls-report` from `023-safety-controls`.

### Tests for User Story 1 (write first, confirm they fail)

- [X] T003 [P] [US1] Write `supabase/tests/database/abuse_reports.test.sql` (pgTAP). Cover:
  - **RLS:** `abuse_reports` has RLS on, and `authenticated` can't select, insert, update or delete.
  - **`submit_abuse_report` success:** a member reports another member and the group itself, and the snapshot names and trimmed details are stored.
  - **`submit_abuse_report` refusals:** `not_group_member`; `invalid_reported_user` for self and for a non-member; `invalid_report` for an unknown reason, blank details and 1,001 characters; `report_limit_reached` on the 11th report within 24 hours (but not for a report 25 hours old); `account_inactive` for a user pending deletion.
  - **`claim_report_emails`:** a second claim inside the lease returns nothing, and a claim after `email_sent_at` returns nothing.
  - **`mark_report_email`:** marks sent, and records an error code.
  - **`stuck_report_emails`:** returns a 25-hour-old unsent report once.
  - **`delete_expired_abuse_reports`:** deletes a 13-month-old report and keeps one with a future `legal_hold_until`.
  - **Trigger and execute rights:** the insert trigger exists, and the service-only functions refuse `authenticated`.
- [X] T004 [P] [US1] Write `tests/LoopMeet.Core.Tests/Validators/AbuseReportValidatorTests.cs`. Cover: each defined `ReportReason` passes, and an undefined value fails; details trimmed to 0 and to 1,001 characters fail, 1 and 1,000 pass; `ReportedUserId = Guid.Empty` fails, while null and a real id pass.
- [X] T005 [P] [US1] Write `supabase/functions/report-email/report-mailer.test.ts` (Deno, with `jsr:@std/assert@1` imported directly), with the tests listed in `contracts/report-email.md`:
  - every body field, and a subject without the group name or the details;
  - "The group itself", and `reply_to` dropped when the reporter's email is missing;
  - `202` marks the row sent, while non-2xx and a network error mark the failure and the later rows still go;
  - a `report_id` body against `{}`, and `400` for any other body;
  - no email, name or details in any log call.
- [X] T006 [P] [US1] Add `tests/LoopMeet.Api.Tests/Infrastructure/InMemoryAbuseReportRepository.cs`. It reproduces the SQL rules from `data-model.md`: membership, self, the limit of 10 in 24 hours using `FixedTimeProvider`, and validation. Register it in `TestWebApplicationFactory.cs` in place of the real repository.
- [X] T007 [US1] Write `tests/LoopMeet.Api.Tests/Endpoints/ReportsEndpointsTests.cs` for `POST /reports`, checking each status and code in `contracts/api.md`:
  - `201` with `Location` and a body;
  - `400 invalid_report`;
  - `403 not_group_member`;
  - `422 invalid_reported_user` for self and for a non-member;
  - `429 report_limit_reached` on the 11th report;
  - `401` without a token.

  With `CapturingLogSink`, assert that `ReportSubmitted` and `ReportRefused` carry ids and the reason code only, never the details or an email.
- [X] T008 [P] [US1] Add a test to `tests/LoopMeet.Api.Tests/Contract/LoggingPrivacyTests.cs`. It reads every `.cs` file under `src/LoopMeet.Api/Services/Safety/`, plus `src/LoopMeet.Api/Endpoints/ReportsEndpoints.cs` and `supabase/functions/report-email/*.ts` (not tests). No logging template or log payload may contain `Email`, `Details`, `details` or `reply_to`.
- [X] T009 [P] [US1] Write the App unit tests in `tests/LoopMeet.App.Tests/Features/Safety/`, and link the four sources (`ReportReasons.cs`, `ReportDraft.cs`, `ReportFallbackMail.cs`, `SafetyErrorText.cs`) in `tests/LoopMeet.App.Tests/LoopMeet.App.Tests.csproj`:
  - **`ReportReasonsTests.cs`:** five reasons, with the labels from the spec and the Api codes from the contract.
  - **`ReportDraftTests.cs`:** `CanSubmit` needs a group, a reason and non-blank details of at most 1,000 characters. `Remaining` counts down. The details are trimmed.
  - **`ReportFallbackMailTests.cs`:** the address is `abuse@loopmeet.io`. The subject names the reason. The body has the group name, the person's display name or "The group itself", the reason, the details and the app version. No GUIDs.
  - **`SafetyErrorTextTests.cs`:** each US1 code maps to its contract message. An unknown code or a network failure gives the fallback prompt.
- [X] T010 [P] [US1] Write `tests/LoopMeet.App.Tests/Features/Safety/ReportConcernSurfaceTests.cs` (source inspection). It checks:
  - **Profile:** `ProfilePage.xaml` has a "Report a concern" settings row bound to `ReportConcernCommand`, using the 022 row styles.
  - **Report page:** `ReportConcernPage.xaml` has a no-groups empty state with the copyable `abuse@loopmeet.io` address, and binds the group and person pickers, the reason picker, an `Editor` with `MaxLength="1000"`, the remaining-characters label and `SubmitReportCommand`.
  - **ViewModel:** `ReportConcernViewModel.cs` is a `sealed partial` class, uses `Email.Default.IsComposeSupported`, `Email.Default.ComposeAsync`, `Clipboard.Default.SetTextAsync`, `ReportFallbackMail` and `SafetyErrorText`, and never logs the details.
  - **Group Detail:** the member tile in `GroupDetailPage.xaml` has a `TapGestureRecognizer` bound to `MemberActionsCommand`, and a `SemanticProperties.Description`.
  - **Routing:** `AppShell.xaml.cs` registers `report-concern`.
- [X] T011 [P] [US1] Extend `web/tests/privacy-policy.test.js`:
  - the processors list names SendGrid and what it does;
  - the policy has a "Reports" section with its anchor;
  - the retention text states reports are kept 1 year, or longer when the law requires, matching `specs/016-account-deletion/data-retention.md`.
- [X] T012 [US1] Run the new tests and confirm they fail for the right reason (missing objects, routes and files, not compile errors elsewhere):
  - `dotnet test`;
  - `deno test supabase/functions/report-email web/tests`;
  - `supabase test db`, with `DOCKER_HOST` set for podman.

### Implementation for User Story 1

- [X] T013 [US1] Write migration `supabase/migrations/<ts>_abuse_reports.sql` as `data-model.md` describes:
  - the table, checks, indexes and RLS, with no user policies;
  - the functions `submit_abuse_report` (with an advisory lock and the 10-in-24-hours count), `claim_report_emails`, `mark_report_email`, `stuck_report_emails` and `delete_expired_abuse_reports`, with execute granted to `authenticated` and `service_role` as listed;
  - the `after insert` trigger calling `net.http_post` to `/functions/v1/report-email` with `{report_id}`;
  - the `report-email-sweep` cron job (`*/5 * * * *`), skipped with a notice when the Vault secrets are missing, as in `20261103120200_account_deletion_purge.sql`, and the unguarded `abuse-reports-retention` job (`17 3 * * *`).

  Apply it locally with `supabase migration up`, never `db reset`. T003 passes.
- [X] T014 [US1] Create `supabase/functions/report-email/`:
  - `deno.json`;
  - `report-mailer.ts`: pure claim, render, send, mark and stuck logic, through injected ports;
  - `index.ts`: the service-role client, SendGrid `fetch` and env secrets `SENDGRID_API_KEY`, `REPORT_EMAIL_FROM` and `REPORT_EMAIL_TO`, per `contracts/report-email.md`.

  Add `[functions.report-email] verify_jwt = false` to `supabase/config.toml`. T005 passes, and `deno lint` is clean.
- [X] T015 [P] [US1] Add to Core:
  - `src/LoopMeet.Core/Models/AbuseReport.cs` and `ReportReason.cs`, with the snake_case code map;
  - `ReportSubmission.cs`, the result;
  - `src/LoopMeet.Core/Interfaces/IAbuseReportRepository.cs`;
  - `src/LoopMeet.Core/Validators/AbuseReportValidator.cs`.

  T004 passes.
- [X] T016 [US1] Add `src/LoopMeet.Infrastructure/Repositories/AbuseReportRepository.cs`:
  - it calls `submit_abuse_report` with `Client.Rpc`, reading the returned row with explicit snake_case names, as `AccountDeletionRepository` does;
  - it maps the `PostgrestException` codes to `ReportSubmission`.

  Register it in `src/LoopMeet.Api/Program.cs`.
- [X] T017 [US1] Add to the Api:
  - `src/LoopMeet.Api/Contracts/SafetyContracts.cs` (`ReportRequest`, `ReportResponse`);
  - `src/LoopMeet.Api/Services/Safety/ReportCommandService.cs`, which validates, calls the repository and logs `ReportSubmitted` or `ReportRefused` by id;
  - `src/LoopMeet.Api/Endpoints/ReportsEndpoints.cs`, mapping each result to the contract's status, code and message.

  Call `app.MapReportsEndpoints()` in `Program.cs`. T007 and T008 pass.
- [X] T018 [P] [US1] Add the MAUI-free rules in `src/LoopMeet.App/Features/Safety/`: `ReportReasons.cs`, `ReportDraft.cs`, `ReportFallbackMail.cs` and `SafetyErrorText.cs`, with no `Microsoft.Maui.*` usings. T009 passes.
- [X] T019 [US1] Add `src/LoopMeet.App/Services/SafetyApi.cs`, with `ISafetyApi.SubmitReportAsync` and the `SafetyApi` wrapper. Register the Refit client and wrapper in `src/LoopMeet.App/MauiProgram.cs` beside the `GroupsApi` registrations.
- [X] T020 [US1] Build the report form:
  - **Page:** `src/LoopMeet.App/Features/Safety/ViewModels/ReportConcernViewModel.cs` and `src/LoopMeet.App/Features/Safety/Views/ReportConcernPage.xaml(.cs)`, with the group picker from `GroupsApi.GetGroupsAsync` and the members from `GetGroupAsync`, excluding self, plus "The group itself (name or meetups)".
  - **Preselection:** query parameters `groupId` and `userId` preselect the group and person.
  - **No groups:** the empty state from the spec's Edge Cases, explaining that reports cover groups you belong to, with the address and a Copy button.
  - **On submit:**
    - **Success:** a confirmation, then back.
    - **4xx:** the `SafetyErrorText` message, keeping the text.
    - **Network or 5xx:** the fallback, `Email.Default.ComposeAsync` with `ReportFallbackMail` when `IsComposeSupported`, otherwise an alert with the address and a Copy button using `Clipboard`.
  - **Wiring:** register the route `report-concern` in `AppShell.xaml.cs`, and the ViewModel and page in `MauiProgram.cs`.
- [X] T021 [US1] Add the "Report a concern" row to the Account card in `src/LoopMeet.App/Features/Profile/Views/ProfilePage.xaml`, using the 022 `SettingsRow` styles, and add `ReportConcernCommand` to `src/LoopMeet.App/Features/Profile/ViewModels/ProfileViewModel.cs`. The command navigates to `report-concern`.
- [X] T022 [US1] Make member tiles tappable:
  - **Page:** in `src/LoopMeet.App/Features/Groups/Views/GroupDetailPage.xaml`, add a `TapGestureRecognizer` bound to `MemberActionsCommand`, with `SemanticProperties.Description` "Actions for {name}".
  - **ViewModel:** in `GroupDetailViewModel.cs`, `MemberActionsCommandAsync` shows an action sheet that offers Report for members other than the viewer, and navigates to `report-concern` with `groupId` and `userId`. The viewer's own tile does nothing.

  T010 passes.
- [X] T023 [US1] Update the privacy documents together, for reports and SendGrid only (research D15): blocks are added in T052. T011 passes, and the web suite is green.
  - **`specs/016-account-deletion/data-retention.md`:** a "Reports" section:
    - reports are kept 1 year, longer under a legal hold, and kept through account deletion, with snapshot names;
    - SendGrid is added to the providers table.
  - **`web/public/privacy/index.html`:** section 2 "Reports", the SendGrid processor row, and the retention text.
  - **`web/data-safety.md`:** the "Other user-generated content" purposes add "Fraud prevention, security, and compliance".
  - **`web/app-privacy.md`:** the "Other User Content" policy-section mapping adds the new section.
- [X] T024 [US1] Update `CLAUDE.md` by hand (not `update-agent-context.sh`):
  - a Recent Changes line for 023 US1;
  - a gotcha: reports have no foreign keys, deliberately, so they outlive deletion. Email delivery is the trigger plus sweep in the migration, with a lease, not a dashboard webhook. The SendGrid key and addresses are Supabase secrets.

### Verification for User Story 1

- [X] T025 [US1] Run every suite and record the counts here:
  - `dotnet test` for the Api, App, Core and Infrastructure;
  - `deno test` for `supabase/functions` and `web/tests`, plus `deno lint`;
  - `supabase test db`;
  - `bash .github/scripts/ci-scope.test.sh`;
  - the iOS simulator and Android Debug builds, with no new warnings.

  **Result (2026-11-16):** Api 161, App 351, Core 48, Infrastructure 1, edge functions 116, web 61, pgTAP 133 (4 files), all passing. `deno lint` is clean, and `ci-scope.test.sh` is ok. Both builds succeed with only the known warnings: `Settings.plist` on iOS, and on Android the `MainActivity` CS8602 and the Java SDK path.
- [ ] T026 [US1] Maintainer, on staging: complete the one-time setup in `quickstart.md`:
  - set the secrets;
  - `supabase functions deploy report-email`;
  - `supabase db push`;
  - deploy the Api from a `staging/**` tag.

  Then run quickstart rows 1–10 on iPhone and Android, and record the results.
- [X] T027 [US1] Open a pull request for this story (`023-safety-controls-report` into `main`), once the maintainer confirms. The body names the privacy documents changed and the secrets that production needs.

**Checkpoint**: US1 is merged. Reports work end to end, and the store-required reporting is in place.

---

## Phase 3: User Story 2 - Leave a group (Priority: P2)

**Goal**: any member except the owner leaves a group after a confirmation. The group, its meetups and its notifications stop for them at once.

**Independent Test**: quickstart rows 11–16.

- [ ] T028 [US2] After US1 merges, update `023-safety-controls` from `main`, then create branch `023-safety-controls-leave` from it.

### Tests for User Story 2 (write first, confirm they fail)

- [ ] T029 [P] [US2] Write `supabase/tests/database/leave_group.test.sql` (pgTAP). Cover:
  - `leave_group` deletes only the caller's membership;
  - the leaver drops out of `active_memberships`;
  - meetups the leaver created stay visible to the remaining members;
  - `owner_cannot_leave`, `not_group_member` and `account_inactive`;
  - a second call raises `not_group_member`.
- [ ] T030 [P] [US2] Add `LeaveAsync` to `tests/LoopMeet.Api.Tests/Infrastructure/InMemoryMembershipRepository.cs`, with the same rules as T029. Write `tests/LoopMeet.Api.Tests/Endpoints/GroupMembershipEndpointsTests.cs` for `POST /groups/{id}/leave`. Cover:
  - `204`, `404 not_group_member`, `409 owner_cannot_leave` and `401`;
  - after leaving, `GET /groups` for the leaver no longer lists the group, and `GET /groups/{id}` for the owner no longer lists the leaver: the cache was invalidated;
  - the `GroupLeft` log has ids only.
- [ ] T031 [P] [US2] Write `tests/LoopMeet.App.Tests/Features/Groups/GroupLeaveSurfaceTests.cs` (source inspection). It checks:
  - **Page:** `GroupDetailPage.xaml` has a "Leave group" button bound to `LeaveGroupCommand`, visible only for non-owners (`IsVisible` bound to `CanLeave`).
  - **ViewModel:** `GroupDetailViewModel.cs` confirms with `DisplayAlert` naming the group, calls `LeaveGroupAsync`, and navigates to the Groups tab. A `not_group_member` response also navigates to Groups with no error (spec Edge Cases, racing actions).
  - **Api interface:** `GroupsApi.cs` declares `[Post("/groups/{groupId}/leave")]`.

  Extend `SafetyErrorTextTests.cs` with `owner_cannot_leave` and `not_group_member`.
- [ ] T032 [US2] Run the new tests and confirm they fail.

### Implementation for User Story 2

- [ ] T033 [US2] Write migration `supabase/migrations/<ts>_leave_group.sql` with `leave_group(p_group_id)` (`security definer`, the account-active check, the errors in `data-model.md`), granted to `authenticated`. Apply it with `supabase migration up`. T029 passes.
- [ ] T034 [P] [US2] Add `src/LoopMeet.Core/Models/MembershipExitResult.cs`, and `LeaveAsync(Guid groupId)` on `src/LoopMeet.Core/Interfaces/IMembershipRepository.cs`.
- [ ] T035 [US2] Implement `LeaveAsync` in `src/LoopMeet.Infrastructure/Repositories/MembershipRepository.cs`: an Rpc call, mapping the error codes.
- [ ] T036 [US2] Add the leave endpoint to the Api. T030 passes.
  - **Service:** `src/LoopMeet.Api/Services/Groups/GroupMembershipCommandService.cs`, `LeaveAsync`. It lists the members first, so it can invalidate `group-detail:{id}`, `groups:{userId}` for the leaver and every remaining member, the leaver's `HomeCacheKey` and `meetups:{id}`. It logs `GroupLeft`.
  - **Endpoint:** `POST /groups/{groupId}/leave` in `src/LoopMeet.Api/Endpoints/GroupsEndpoints.cs`, with the contract's codes and messages.
  - **Registration:** the service in `Program.cs`.
- [ ] T037 [US2] Add Leave group to the App. T031 passes.
  - **Api interface:** `LeaveGroupAsync` on `IGroupsApi` and `GroupsApi` in `src/LoopMeet.App/Services/GroupsApi.cs`.
  - **ViewModel:** `CanLeave` and `LeaveGroupCommand` in `GroupDetailViewModel.cs`, with a confirmation that says a new invitation is needed to rejoin, then navigation to `//groups`. A `not_group_member` response is treated as already left.
  - **Page:** the "Leave group" button below the members in `GroupDetailPage.xaml`.
- [ ] T038 [US2] Update the 023 Recent Changes line in `CLAUDE.md`. Add a gotcha: leaving deletes the membership row, so `active_memberships` needs no change. Don't add a soft-delete.

### Verification for User Story 2

- [ ] T039 [US2] Run every suite, as in T025, and record the counts here.
- [ ] T040 [US2] Maintainer, on staging: `supabase db push`, deploy the Api, then run quickstart rows 11–16 on iPhone and Android, and record the results.
- [ ] T041 [US2] Open a pull request for this story (`023-safety-controls-leave` into `main`), once the maintainer confirms.

**Checkpoint**: US2 is merged. Members can leave groups, and their notifications stop.

---

## Phase 4: User Story 3 - Remove and block (Priority: P2)

**Goal**: the owner removes and blocks members and manages the Blocked list. Members can Leave and block, and invitees can Decline and block. Blocked people can't be invited back. Each side lifts only its own blocks.

**Independent Test**: quickstart rows 17–30.

- [ ] T042 [US3] After US2 merges, update `023-safety-controls` from `main`, then create branch `023-safety-controls-block` from it.

### Tests for User Story 3 (write first, confirm they fail)

- [ ] T043 [P] [US3] Write `supabase/tests/database/group_blocks.test.sql` (pgTAP). Cover:
  - **RLS:** the owner sees only `owner` blocks, and the blocked person sees only `self` blocks. Neither sees the other side's, and both policies keep the 016 conditions.
  - **`remove_group_member`:**
    - the owner removes a member;
    - refusals: `not_group_owner` for a non-owner, `cannot_remove_owner`;
    - an already-gone member is not an error without `p_block`, and raises `not_group_member` with it;
    - with `p_block`, it inserts an `owner` block with the lower-cased email, cancels pending invitations by id and by email, and keeps an existing `self` row.
  - **`leave_group_and_block`:** inserts a `self` block.
  - **`lift_group_block`:** each side lifts its own block. A cross-side attempt raises `block_not_found` or `not_group_owner`.
  - **`decline_invitation_and_block`:** declines and blocks, and refuses someone else's invitation.
  - **`invitation_is_blocked`:** true by email and by the account's current email, and raises for a non-owner.
  - **`caller_is_blocked_from_group`:** returns true for a blocked caller.
  - **Recreated policy and functions:**
    - `invitations_insert_owner` refuses a blocked email and still refuses a pending-deletion owner;
    - `create_membership_from_invitation` raises `blocked_from_group`;
    - `purge_account_data` deletes the user's blocks by id and email, and leaves `abuse_reports` untouched;
    - deleting a group cascades its blocks.

  Also extend `supabase/functions/notifications-dispatch/webhook-router.test.ts` (FR-019): an `invitations` `UPDATE` to `cancelled` or `declined`, and a `memberships` `INSERT` or `DELETE`, each resolve to no notification.
- [ ] T044 [P] [US3] Add `tests/LoopMeet.Api.Tests/Infrastructure/InMemoryGroupBlockRepository.cs` and the membership remove and leave-and-block methods, with the T043 rules, and register them in `TestWebApplicationFactory.cs`. Then write the endpoint tests:
  - **`GroupMembershipEndpointsTests.cs`:** `DELETE /groups/{id}/members/{userId}` with and without `block` (`204`, `403 not_group_owner`, `409 cannot_remove_owner`, `204` for an already-removed member without `block`, `404 not_group_member` with `block`), and `POST /groups/{id}/leave-and-block`.
  - **`BlocksEndpointsTests.cs`:** `GET` and `DELETE /groups/{id}/blocks…` for the owner only, and `GET` and `DELETE /blocks…` for the caller's own blocks, with each status.
  - **`InvitationsWriteEndpointsTests.cs`:**
    - `409 invitee_blocked` for an owner-set and a self-set block, with the same message;
    - `409 invitation_unavailable` on accept, with the invitation then `cancelled`;
    - `POST /invitations/{id}/decline-and-block`.
  - **Logs:** `GroupMemberRemoved`, `GroupMemberBlocked`, `GroupBlockLifted` and `InvitationDeclinedAndBlocked` carry ids only.
- [ ] T045 [P] [US3] Write `tests/LoopMeet.App.Tests/Features/Safety/BlockSurfaceTests.cs` (source inspection). It checks:
  - **Member action sheet** (`GroupDetailViewModel.cs`): it adds "Remove from group" and "Block" only when `IsOwner` and the tile isn't the owner's, each after a confirmation.
  - **Leave choices:** the Leave group sheet offers "Leave group" and "Leave and block".
  - **Owner's Blocked list:** `GroupBlocksPage.xaml` lists names and dates with an Unblock action and a confirmation, and a "Blocked people" button is visible only for the owner.
  - **Profile:** `ProfilePage.xaml` has a "Blocked groups" row, and `BlockedGroupsPage.xaml` lists groups with Unblock.
  - **Pending invitations:** in `PendingInvitationsViewModel.cs`, Decline offers "Decline" and "Decline and block".
  - **Routes:** `group-blocks` and `blocked-groups` are registered.
  - **Accessibility:** icon-only actions have a `SemanticProperties.Description`.

  Extend `SafetyErrorTextTests.cs` with the US3 codes.
- [ ] T046 [US3] Run the new tests and confirm they fail.

### Implementation for User Story 3

- [ ] T047 [US3] Write migration `supabase/migrations/<ts>_group_blocks.sql`, as `data-model.md` describes:
  - **New objects:**
    - the table, keys, indexes and the two select policies (`to authenticated`, with the 016 conditions);
    - `private.group_block_matches`;
    - the functions `leave_group_and_block`, `remove_group_member`, `lift_group_block`, `decline_invitation_and_block`, `invitation_is_blocked` and `caller_is_blocked_from_group`.
  - **Recreated objects:**
    - the `invitations_insert_owner` policy, with the block condition added and the 016 conditions kept;
    - the `create_membership_from_invitation` function, with the block check;
    - the `purge_account_data` function, with the block delete.

  Apply it with `supabase migration up`. T043 passes, and the existing `account_deletion_*` pgTAP files still pass.
- [ ] T048 [P] [US3] Add to Core:
  - `src/LoopMeet.Core/Models/GroupBlock.cs`;
  - `src/LoopMeet.Core/Interfaces/IGroupBlockRepository.cs`: list owner blocks, list my blocks, lift, decline-and-block, `IsInvitationBlocked`, `IsCallerBlocked`;
  - `RemoveAsync(groupId, userId, block)` and `LeaveAndBlockAsync(groupId)` on `IMembershipRepository.cs`.
- [ ] T049 [US3] Implement the repositories, and register them in `Program.cs`:
  - `src/LoopMeet.Infrastructure/Repositories/GroupBlockRepository.cs`, using Rpc and the selects under RLS, with display and group names resolved from `user_profiles` and `groups`;
  - the `MembershipRepository.cs` additions.
- [ ] T050 [US3] Add the Api pieces. T044 passes.
  - **Membership service:** remove, block and leave-and-block in `GroupMembershipCommandService.cs`, with the cache invalidation from `contracts/api.md`.
  - **Block services:** `src/LoopMeet.Api/Services/Safety/BlockCommandService.cs`, and `BlockQueryService.cs` with 30-second caches `group-blocks:{id}` and `my-blocks:{userId}`.
  - **Endpoints:**
    - `src/LoopMeet.Api/Endpoints/BlocksEndpoints.cs`;
    - the new routes in `GroupsEndpoints.cs`;
    - in `InvitationCommandService.cs`, the `Blocked` status and the checks in `CreateAsync` and `AcceptAsync`, plus decline-and-block in `InvitationsEndpoints.cs`.
  - **Registration:** `app.MapBlocksEndpoints()` in `Program.cs`.
- [ ] T051 [US3] Add the App pieces. T045 passes.
  - **Api interfaces:** the Refit methods from `contracts/api.md` in `GroupsApi.cs`, `InvitationsApi.cs` and `SafetyApi.cs`.
  - **Group Detail:** Remove and Block in the member action sheet, Leave and block in the leave sheet, and the owner's "Blocked people" button.
  - **New pages:**
    - `src/LoopMeet.App/Features/Groups/ViewModels/GroupBlocksViewModel.cs` and `Views/GroupBlocksPage.xaml(.cs)`;
    - `src/LoopMeet.App/Features/Safety/ViewModels/BlockedGroupsViewModel.cs` and `Views/BlockedGroupsPage.xaml(.cs)`.
  - **Profile:** the "Blocked groups" row.
  - **Pending invitations:** the Decline action sheet in `PendingInvitationsViewModel.cs`.
  - **Wiring:** the routes in `AppShell.xaml.cs`, and the registrations in `MauiProgram.cs`.
- [ ] T052 [US3] Add blocks to the privacy documents together (research D15), test first:
  - **Test:** extend `web/tests/privacy-policy.test.js` to require a "Reports and blocks" section and the block retention rule, and see it fail.
  - **`data-retention.md`:** rename the section "Reports and blocks", and add that blocks are deleted on lift, with the group or with either account.
  - **Privacy policy:** rename section 2 to "Reports and blocks", and describe blocks and their retention.
  - **Store forms:** check `web/data-safety.md` and `web/app-privacy.md`, and change them only if an answer changes.
- [ ] T053 [US3] Update `CLAUDE.md`:
  - the 023 Recent Changes line;
  - a gotcha: each side lifts only its own block, so owners can't see `self` blocks. `invitation_is_blocked` answers only true or false. Any new invitation path must call `private.group_block_matches`.

### Verification for User Story 3

- [ ] T054 [US3] Run every suite, as in T025, and record the counts here.
- [ ] T055 [US3] Maintainer, on staging: `supabase db push`, deploy the Api, then run quickstart rows 17–30 on iPhone and Android, and record the results.
- [ ] T056 [US3] Open a pull request for this story (`023-safety-controls-block` into `main`), once the maintainer confirms.

**Checkpoint**: all three stories are merged. The guideline 1.2 controls are complete.

---

## Phase 5: Polish & store follow-up

- [ ] T057 Maintainer:
  - record the 🎥 quickstart rows on a physical iPhone;
  - update the App Review notes so the report and block lines describe the real flows;
  - submit a new build (lockstep build number) for review (SC-006).
- [ ] T058 Update the untracked `plan/child-safety/` (README, draft page and prompts) so they describe this report form and `abuse@loopmeet.io` as the safety contact, instead of the mail-only "Report a concern" (spec Assumptions). That later feature then adds only the standards page, its links, the legal-hold runbook and the retention text.
- [ ] T059 Update the store-release memory with 023's state, and record the device results in `quickstart.md` on a docs-only branch if any arrive after the last story merges.

---

## Dependencies & execution order

**Phases**:

1. **Phase 1:** before everything.
2. **US1 (Phase 2):** depends only on Phase 1.
3. **US2 (Phase 3):** starts after the US1 pull request merges. It reuses `SafetyErrorText` from US1, and otherwise touches only Groups.
4. **US3 (Phase 4):** starts after the US2 pull request merges. It reuses `leave_group` (US2) inside `leave_group_and_block`, and the report and error-text pieces (US1).
5. **Phase 5:** after US3.

**Within each story**:

1. Tests (all `[P]` with each other, except the Api endpoint test after its in-memory double).
2. Run the tests and confirm they fail.
3. Migration.
4. Core.
5. Infrastructure.
6. Api.
7. App: rules, then Refit, then ViewModel and page, then entry points.
8. Documents.
9. Verification.
10. Pull request.

**Parallel examples**:

- **US1:** T003, T004, T005, T006, T008, T009, T010 and T011 together. Then T015 and T018 together, beside the migration (T013) and the function (T014).
- **US2:** T029, T030 and T031 together. Then T034 beside T033.
- **US3:** T043, T044 and T045 together. Then T048 beside T047.

## Implementation strategy

- **MVP:** US1 alone. It answers Google Play's in-app reporting requirement and half of App Store guideline 1.2, so it can ship to the stores before US2 and US3.
- **Incremental:** each story is a separate pull request, deployed to staging, and checked against its quickstart rows before the next branch starts.
- **Stop points:** stop after each "open a pull request" task, so the maintainer can review, merge and deploy before the next story.
