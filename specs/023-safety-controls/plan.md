# Implementation Plan: Safety Controls (Report, Leave, Block)

**Branch**: `023-safety-controls` | **Date**: 2026-11-16 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/023-safety-controls/spec.md`. Design input: [design/](design/).

## Summary

Add the controls App Store guideline 1.2 and Google Play's in-app reporting rule require:

- **Report a concern (US1):** a form on Profile and on Group Detail member tiles. The report is recorded through the Api and emailed to `abuse@loopmeet.io` through SendGrid, with a mail-app fallback.
- **Leave a group (US2).**
- **Remove and block (US3):** the owner removes or blocks a member, a member can Leave and block, and an invitee can Decline and block. Each side lifts only its own blocks.

**How it's built:**

- **Database:** every write is a `security definer` SQL function that the Api calls as the caller (research D1). Leaving deletes the membership row, so notifications stop with no change to `active_memberships` (D2).
- **Email:** an insert trigger and a 5-minute sweep call one new Edge Function, `report-email` (D4).
- **Delivery:** each story ships as its own pull request, from its own branch off `023-safety-controls`.

## Technical Context

**Language/Version**:

- C# 13 / .NET 10 (MAUI App, minimal-API Api, Core, Infrastructure);
- TypeScript on Deno 2.x (Edge Function);
- SQL (PostgreSQL 17, Supabase migrations).

**Primary Dependencies**: Microsoft.Maui.Controls 10.0.x, CommunityToolkit.Mvvm 8.4.x, Refit.HttpClientFactory 10.2.x, Supabase 8.9.0 (Postgrest `Rpc`), FluentValidation 12.x, `@supabase/supabase-js` 2 in the Edge Function, the SendGrid v3 REST API through `fetch`, and `pg_net` and `pg_cron` (already in use). No new NuGet or npm packages.

**Storage**:

- **New tables:** `abuse_reports` (US1) and `group_blocks` (US3).
- **New functions:** twelve public SQL functions plus one `private` matcher.
- **Recreated objects:** the `invitations_insert_owner` policy, and the `create_membership_from_invitation` and `purge_account_data` functions.

All of it is listed in [data-model.md](data-model.md).

**Testing**:

- **Core:** xUnit `AbuseReportValidator` tests.
- **Api:** xUnit with `WebApplicationFactory<Program>` and the in-memory repositories under `tests/LoopMeet.Api.Tests/Infrastructure/`, covering every Result status, plus the `LoggingPrivacyTests` extension.
- **App:** xUnit unit tests for the MAUI-free `Features/Safety/*` rules (D11), and source-inspection tests for the XAML and ViewModels.
- **Edge Function:** Deno tests.
- **Database:** pgTAP through `supabase test db`.
- **Devices:** the [quickstart.md](quickstart.md) matrix.

**Target Platform**: iOS 15+ (iPhone only, 021) and Android API 24+. Supabase-hosted Postgres and Edge Functions. The Api on DigitalOcean App Platform.

**Project Type**: Mobile app plus Api plus Supabase (the existing layout).

**Performance Goals**:

- A report reaches the safety inbox within 5 minutes (SC-002).
- Leave, remove and block each run as a single function round trip.
- The rate-limit count is indexed.

**Constraints**:

- The Api keeps its caller-impersonating Supabase client: no service role (`PrivilegedAccessTests`).
- Every new or recreated policy keeps the 016 active-account conditions, `to authenticated`.
- Notification recipients come only from `active_memberships`.
- No email address or report text in any log.
- The SendGrid key lives only in Supabase secrets.

**Scale/Scope**:

- Low volume: reports are rare, and limited to 10 per user per day.
- Three stories and three pull requests.
- In the App: about 3 new pages and changes to 3 existing pages.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
| --- | --- | --- |
| I. Code Quality | Pass | Each rule lives in one place: the SQL function enforces it, the Api maps its code to a status, and the App maps the status to a message (`SafetyErrorText`). No commented-out code. The Edge Function separates pure logic (`report-mailer.ts`) from I/O, as `account-purge` does. |
| II. Tests Required | Pass | Every refusal has a pgTAP test and an Api test: self-report, non-member, rate limit, owner leaving, non-owner remove or block, blocked invite, blocked accept. The validator, the App's rules (fallback content, limits, error text), the Edge Function's rendering, sending and logging, and the XAML bindings are all tested. The [quickstart](quickstart.md) covers device-only behaviour (mail composer, action sheets, pushes). All of it runs in CI under the existing scopes. |
| III. User Experience | Pass | Three independently deliverable stories with acceptance scenarios. Every refusal message is actionable (contracts/api.md). Destructive actions confirm first. A failed report keeps the text and offers the mail-app route. Tappable member tiles get screen-reader descriptions (FR-025). |
| IV. Simplicity | Pass, one justified exception | No new service layer beyond the paired Command and Query services the convention requires. No SDK for SendGrid: one `fetch` call. No soft-delete history (D2). Leave and block need no validator, because they take only route ids. The exception: the trigger-plus-sweep delivery, recorded in Complexity Tracking. |
| V. Modularity | Pass | **New modules:** `Safety` in Core, Infrastructure, Api and App (reports and blocks); the existing Groups and Invitations modules gain leave, remove and decline-and-block. **Edge Function:** `report-email`, one function with one responsibility. **Dependencies:** none circular. The App reaches the Api only through Refit, and the Api reaches the database only through repositories. |
| VI. Contract-First | Pass | [contracts/api.md](contracts/api.md) (endpoints, statuses, codes, messages, Refit), [contracts/report-email.md](contracts/report-email.md) (the function's input, email and logs) and the function table in data-model.md are written before code. The two changed endpoints (invite and accept) add a status without changing existing ones: an older App shows the server message. |
| VII. Observability | Pass | Structured events on report, refusal, leave, remove, block, lift and decline-and-block, keyed by ids (D14). The Edge Function logs sent, failed and stuck. A report still undelivered after 24 hours raises a one-time alert. A new `LoggingPrivacyTests` test keeps emails and report text out of log templates. |
| Additional: safety controls | Pass | This feature *is* the safety control set: report, leave and block. |
| Additional: retention documented | Pass | Reports: 1 year, or longer under a legal hold, kept through account deletion. Blocks: deleted on lift, with the group, or with either account. The new processor, SendGrid, is in `data-retention.md`, the privacy policy, `data-safety.md` and `app-privacy.md`, changed together in the US1 pull request (reports and SendGrid), and again in the US3 pull request (blocks) (D15). |
| Privacy defaults | Pass | The reported person learns nothing. An owner can't see member-set blocks. The reporter's email goes only to the safety inbox, and isn't stored in the table (D3). |

**Post-design re-check:** still Pass. Phase 1 added no project or package. The one exception is unchanged.

## UX review

This is the constitution's quality gate for user-facing changes, reviewed against the spec's acceptance scenarios and research D12 and D13.

- **Entry points:** reports are reachable in two taps, from Profile → Report a concern, or from a member tile → Report, with the group and person already chosen.
  - Leave sits below the members in Group Detail, out of the way of everyday actions.
  - Owner-only actions (Remove, Block, Blocked people) never show to members.
- **Confirmations:** every action that changes membership confirms first, and names the group or person. Submitting a report doesn't ask again: the form is already the deliberate step, and a second prompt would discourage reporting.
- **Messages:** every refusal has an actionable message, from contracts/api.md, mapped in one place (`SafetyErrorText`).
  - The rate-limit message names the email alternative.
  - The block messages are neutral: they never reveal who set a block.
  - A racing leave or removal shows no error (spec Edge Cases).
- **Empty, offline and failure states:**
  - **No groups:** an explanation, plus the copyable address.
  - **Network failure:** the text is kept, and the mail app is offered.
  - **No mail app:** the address is shown with Copy.
  - **Empty lists:** the Blocked people and Blocked groups lists say "No one is blocked" and "You haven't blocked any groups".
- **Accessibility:** tappable member tiles and icon-only actions carry `SemanticProperties.Description`. Pickers and the details editor have labels. The remaining-characters counter is announced as text.
- **Cost:** Decline on Pending Invitations gains one tap, an action sheet choosing Decline or Decline and block. That's accepted, because it's the only entry point for Decline and block, and declining is infrequent.

## Project Structure

### Documentation (this feature)

```text
specs/023-safety-controls/
├── design/                 # input: README, Spec Kit prompts
├── spec.md
├── plan.md
├── research.md             # D1–D15
├── data-model.md           # tables, functions, RLS, Core models
├── contracts/
│   ├── api.md              # endpoints and Refit
│   └── report-email.md     # Edge Function input, email, logs
├── quickstart.md           # staging device matrix and App Review recording script
├── checklists/requirements.md
└── tasks.md                # /speckit.tasks
```

### Source Code (repository root)

```text
supabase/
├── migrations/
│   ├── <ts>_abuse_reports.sql            # US1: table, submit/claim/mark/stuck/expire functions, trigger, sweep and retention cron
│   ├── <ts>_leave_group.sql              # US2: leave_group
│   └── <ts>_group_blocks.sql             # US3: table, RLS, block functions, recreated invitations_insert_owner,
│                                         #      create_membership_from_invitation, purge_account_data
├── functions/report-email/               # US1: index.ts, report-mailer.ts, report-mailer.test.ts, deno.json
├── tests/database/
│   ├── abuse_reports.test.sql            # US1
│   ├── leave_group.test.sql              # US2
│   └── group_blocks.test.sql             # US3
└── config.toml                           # US1: [functions.report-email] verify_jwt = false

src/LoopMeet.Core/
├── Models/AbuseReport.cs, ReportReason.cs, GroupBlock.cs, MembershipExitResult.cs
├── Interfaces/IAbuseReportRepository.cs, IGroupBlockRepository.cs   (+ IMembershipRepository.LeaveAsync)
└── Validators/AbuseReportValidator.cs

src/LoopMeet.Infrastructure/Repositories/
├── AbuseReportRepository.cs              # Rpc submit_abuse_report, error-code mapping
├── GroupBlockRepository.cs               # US3
└── MembershipRepository.cs               # + LeaveAsync (US2), RemoveAsync/LeaveAndBlockAsync (US3)

src/LoopMeet.Api/
├── Endpoints/ReportsEndpoints.cs         # US1
├── Endpoints/BlocksEndpoints.cs          # US3: /blocks, /groups/{id}/blocks
├── Endpoints/GroupsEndpoints.cs          # + leave (US2), leave-and-block, members DELETE (US3)
├── Endpoints/InvitationsEndpoints.cs     # + decline-and-block, invitee_blocked, invitation_unavailable (US3)
├── Contracts/SafetyContracts.cs
├── Services/Safety/ReportCommandService.cs, BlockCommandService.cs, BlockQueryService.cs
├── Services/Groups/GroupMembershipCommandService.cs   # leave (US2), remove/block (US3)
└── Services/Invitations/InvitationCommandService.cs   # + Blocked status (US3)

src/LoopMeet.App/
├── Features/Safety/
│   ├── ReportReasons.cs, ReportDraft.cs, ReportFallbackMail.cs, SafetyErrorText.cs   # MAUI-free (D11)
│   ├── ViewModels/ReportConcernViewModel.cs, BlockedGroupsViewModel.cs
│   └── Views/ReportConcernPage.xaml, BlockedGroupsPage.xaml
├── Features/Groups/ViewModels/GroupDetailViewModel.cs, GroupBlocksViewModel.cs
├── Features/Groups/Views/GroupDetailPage.xaml, GroupBlocksPage.xaml
├── Features/Invitations/…                # Decline → Decline and block (US3)
├── Features/Profile/Views/ProfilePage.xaml   # Report a concern (US1), Blocked groups (US3) rows
├── Services/SafetyApi.cs, GroupsApi.cs, InvitationsApi.cs
├── AppShell.xaml.cs                      # routes: report-concern, blocked-groups, group-blocks
└── MauiProgram.cs                        # registrations

tests/
├── LoopMeet.Core.Tests/Validators/AbuseReportValidatorTests.cs
├── LoopMeet.Api.Tests/Endpoints/ReportsEndpointsTests.cs, GroupMembershipEndpointsTests.cs, BlocksEndpointsTests.cs
├── LoopMeet.Api.Tests/Endpoints/InvitationsWriteEndpointsTests.cs   # + blocked cases
├── LoopMeet.Api.Tests/Infrastructure/InMemoryAbuseReportRepository.cs, InMemoryGroupBlockRepository.cs (+ membership exits)
├── LoopMeet.Api.Tests/Contract/LoggingPrivacyTests.cs              # + no email or details in new log templates
├── LoopMeet.App.Tests/Features/Safety/*Tests.cs                    # unit (D11) and source inspection
└── LoopMeet.App.Tests/LoopMeet.App.Tests.csproj                    # + links to the four MAUI-free files

docs to change together, with US1 (reports, SendGrid) and US3 (blocks): specs/016-account-deletion/data-retention.md, web/public/privacy/index.html,
web/tests/privacy-policy.test.js, web/data-safety.md, web/app-privacy.md
CLAUDE.md: Recent Changes line and gotchas, edited by hand at the end (update-agent-context.sh damages it)
```

**Structure Decision**: the existing four-layer layout (CLAUDE.md "Real architecture"), with a `Safety` module in each layer for reports and blocks. Leave and remove extend Groups, and decline-and-block extends Invitations, where those flows already live.

**CI scope**: no `ci-scope.sh` change is needed.

| Path | Suites it selects |
| --- | --- |
| `supabase/functions/*`, which covers the new `report-email` | `edge_functions api_tests` (an existing case in `ci-scope.test.sh`) |
| `supabase/migrations/*`, `supabase/tests/*` | `database` |
| `src/LoopMeet.App/*` | `app_tests build_app` |
| `src/LoopMeet.Api/*`, `src/LoopMeet.Core/*`, `src/LoopMeet.Infrastructure/*` | their suites |
| `web/*` | `web` |

`specs/*` selects nothing. `bash .github/scripts/ci-scope.test.sh` is run once at the end to confirm.

**Delivery** (spec Assumptions):

| Story | Branch | Pull request holds |
| --- | --- | --- |
| US1 | `023-safety-controls-report` | US1 plus the privacy documents for reports and SendGrid |
| US2 | `023-safety-controls-leave` | branched after US1 merges |
| US3 | `023-safety-controls-block` | branched after US2 merges |

All three branch from `023-safety-controls`. The spec documents land on `023-safety-controls` first.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
| --- | --- | --- |
| Two paths deliver one report email: an insert trigger, and a 5-minute sweep, both calling `report-email` with a claim lease | Clarification (b) and FR-005: send at once, retry until delivered, alert after 24 hours, never lose a report | **Trigger only** loses the email during a SendGrid or network failure. **Sweep only** adds up to 5 minutes of delay to every report, and runs a job every minute to cut that. **Sending from the Api** puts a third-party key and a retry loop in the Api, and still needs a sweep for crashes. |
