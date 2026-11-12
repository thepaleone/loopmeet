# Implementation Plan: Account Deletion with a 30-Day Grace Period

**Branch**: `016-account-deletion` | **Date**: 2026-11-03 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/016-account-deletion/spec.md`

## Summary

Profile → Delete Account opens a confirmation screen. It lists the groups the user owns with member counts, and the user must type `DELETE`. The request is recorded with a permanent-deletion time 30 days later and the user is signed out. From that moment the user, their owned groups and everything in them are hidden from everyone. If the user signs in during those 30 days, they get only a "scheduled for deletion" screen with Restore account and Sign out. After 30 days an hourly job deletes everything, including the user's OneSignal record and their Apple sign-in authorization.

Code review turned up five facts that shape the design:

1. **Every Api read already goes through Postgres RLS as the caller, but the publishable key lets any user call PostgREST directly.** "Hidden now" is therefore enforced in RLS with three helper functions ANDed into every policy. The data is never moved, so a restore is exactly lossless ([research D1](./research.md#d1--hidden-now-is-enforced-by-rls-not-by-moving-data)).
2. **The Api has never used its service-role key. All privileged work runs in edge functions.** Permanent deletion is a new scheduled `account-purge` edge function. Request and restore are caller-run `security definer` SQL functions, so FR-019 holds with no new privileged path in the Api ([D2](./research.md#d2--request-and-restore-are-caller-run-database-functions), [D4](./research.md#d4--permanent-deletion-runs-in-a-scheduled-edge-function)).
3. **Edge functions bypass RLS.** Recipient resolution and reminders switch to an `active_memberships` view, so pending users and their groups drop out of every push path ([D8](./research.md#d8--notifications-skip-pending-users-and-their-groups)).
4. **Sign in with Apple currently discards the authorization code, and Supabase doesn't revoke Apple tokens.** The code is now exchanged at sign-in by a small `apple-authorization` edge function, and the refresh token is stored so the purge can revoke it ([D7](./research.md#d7--sign-in-with-apple-revocation)).
5. **Old avatars are never cleaned up** (each upload is a new `{userId}/{guid}` object), so the purge removes the whole prefix ([D14](./research.md#d14--avatar-bucket-name-is-shared-configuration)).

The app learns that an account is pending from one signal: every Api call returns `403 account_pending_deletion` while pending. A new `AccountStatusHandler` routes that to the pending screen. This covers sign-in, cold start, background refresh and other devices without adding anything to the startup path ([D3](./research.md#d3--a-pending-session-is-redirected-by-one-api-signal-handled-once-in-the-app)).

## Technical Context

**Language/Version**: C# 13 / .NET 10 (MAUI client + minimal-API backend). TypeScript on Deno 2.x (Supabase Edge Functions). SQL (PostgreSQL 17, Supabase migrations).
**Primary Dependencies**: Microsoft.Maui.Controls 10.0.70, CommunityToolkit.Mvvm 8.4.2, Refit.HttpClientFactory 10.2.0, Supabase 1.1.1 (Postgrest `Rpc`, `Functions`), `System.TimeProvider`. `@supabase/supabase-js` in edge functions, WebCrypto for the Apple ES256 client secret, `jsr:@std/assert@1` for tests. **New test tooling**: pgTAP through the Supabase CLI (`supabase test db`). No new NuGet or npm packages.
**Storage**: New tables `account_deletions` and `apple_authorizations`, a view `active_memberships`, 8 SQL functions, and recreated RLS policies on `groups`, `memberships`, `invitations`, `meetups` and `user_profiles` ([data-model.md](./data-model.md)). Avatar objects live in Supabase Storage under `{bucket}/{userId}/`.
**Testing**:

- **Database (new layer):** pgTAP tests run in CI against `supabase start`. They cover each policy for pending, active and outsider callers; request and restore semantics; and the completeness and idempotency of `purge_account_data`.
- **Deno:** `purge-runner.ts` orchestration with fakes (order, resume, idempotency, stuck logging), `apple-auth.ts` (JWT and request shapes), `account-status.ts`, and the changed recipient resolution.
- **Api:** `WebApplicationFactory<Program>` with `InMemoryAccountDeletionRepository` and `FixedTimeProvider`, covering the endpoints, the gate middleware, the preview and the log fields.
- **App:** view-model unit tests for the confirmation rule and the pending-screen actions, plus source-inspection tests for XAML, routes, handler order and the Apple registrar call.
- **Manual:** the [quickstart.md](./quickstart.md) device matrix, with iOS rows deferred until 013 can be tested.

**Target Platform**: Android (shipping), iOS and MacCatalyst (once 013 is testable). Staging Supabase and Api on DigitalOcean App Platform.
**Project Type**: Mobile app + minimal-API backend + Supabase (Postgres, Edge Functions, Storage) — the existing structure.
**Performance Goals**:

- The confirmation screen loads in under 1 s on mobile, so the whole flow fits SC-001's 1 minute.
- The gate adds no database round trip on cached requests (30 s per-user TTL).
- The RLS helpers are primary-key lookups, so list queries stay within their current latency.
- A purge run handles 10 users per hour, far above the expected volume.

**Constraints**:

- Normal requests keep the caller-impersonating client. Privileged access exists only in `account-purge` and `apple-authorization` (FR-019).
- No PII in logs (FR-020).
- The 006 rule stays: meetup RLS allows any active member to mutate, and the UI restricts mutation to owners. The new conditions are added, not substituted.
- Collection mutations on the main thread. No `CollectionView` in a `ScrollView`. Style keys are verified before use.
- The startup path must not gain a blocking call (010).

**Scale/Scope**:

- **Migration:** 1 migration and 1 cron job.
- **Edge functions:** 2 new, 2 changed, 2 shared modules.
- **Api:** 4 endpoints, 1 middleware, 1 repository and 1 Core model + interface.
- **App:** 2 pages + view models, 1 handler, 1 gate service and 1 Apple registrar.
- **CI:** 1 new job.
- **Tests:** about 60 across four layers.

## Constitution Check

*GATE: evaluated against Meetloop Constitution v0.1.0, before Phase 0 and re-checked after Phase 1.*

| Gate | Principle | Status | Notes |
| ------ | ----------- | -------- | ------- |
| G1 | I. Code Quality | PASS | One rule per concern: the "active" check is three SQL helpers reused by every policy and by the view. Purge orchestration is a pure function over ports. There are no TODOs. The provider retention values are filled in as a task, not left as markers. |
| G2 | II. Tests Required | PASS | Every FR maps to a test layer (see Test strategy). The security-critical SQL gets real database tests rather than string inspection (D10). Native-only behaviour (Apple revocation, push on devices) is in the quickstart matrix. |
| G3 | III. UX First | PASS | 4 prioritized stories with acceptance scenarios. The confirmation lists the real consequences (owned groups and member counts). Error copy says what happened and what to do ("Type DELETE to confirm.", "Your account can no longer be restored."). The pending screen offers exactly two actions. |
| G4 | IV. Simplicity | PASS, with exceptions recorded | There are no data copies: hiding is a predicate and restore deletes one row. The state set is `pending` and `purging`. The new test layer and the app → edge-function call are justified in Complexity Tracking. |
| G5 | V. Modularity | PASS | The new modules are: Core `Accounts/` (model, policy, interface), Infrastructure `AccountDeletionRepository`, the Api `AccountDeletion` endpoints and middleware, the App `Features/Account/` pages and gate, and the edge `account-purge`, `apple-authorization` and two `_shared` modules. Dependencies point inward only. |
| G6 | VI. Contract-First | PASS | [contracts/api-account-deletion.md](./contracts/api-account-deletion.md) and [contracts/edge-functions.md](./contracts/edge-functions.md) define the endpoints, the 403 gate body and the edge payloads before code. They're shared with 017. |
| G7 | VII. Observability | PASS | A named structured event exists for every request, restore, refusal, purge step, completion and stuck purge, keyed by `user_id` (D12). Provider failures log status codes, not bodies. |
| G8 | Privacy & retention constraints | PASS | `data-retention.md` (FR-021) is a deliverable. Apple refresh tokens sit in a table with no client policies and are deleted after revocation. |
| G9 | Quality gates | PASS | New `database-tests` CI job. `deno lint` and `deno test` cover the new functions. Existing .NET analyzers apply. The UX review is the confirmation and pending screen copy in the quickstart. |

**Post-design re-check (after Phase 1)**: all gates still pass. The design added `active_memberships` (one definition reused by two functions, which meets G4's two-use-case bar) and the D15 cache note, which aligns SC-003 with the existing 30 s cache rather than adding cross-user invalidation.

## Test strategy

| Requirement | Test layer | What is asserted |
| --- | --- | --- |
| FR-001, FR-002 | App VM, Api | The preview lists owned groups with counts; `CanDelete` is true only for exact `DELETE`; the server returns 400 otherwise |
| FR-003, FR-005 | pgTAP, Api | The row has 30 days to the microsecond; a repeat request keeps its dates; 202 body |
| FR-004 | Contract doc, Api | Endpoints take no client-specific input; the gate allowlist |
| FR-006, FR-007 | pgTAP | For each table: a pending owner, a pending member and their invitations are hidden from an active member; updates and deletes on hidden rows affect 0 rows |
| FR-008, FR-013 | pgTAP (view), Deno | `active_memberships` excludes pending; recipient resolver and scheduler use it; nothing is queued |
| FR-009, FR-010 | Api, App source + VM | 403 gate body and cache invalidation; handler order; the gate navigates once; the pending page has only two commands |
| FR-011, FR-012 | pgTAP, Api | Only `restore_account_deletion` removes the row; after the deadline it raises; Api returns 409 |
| FR-014, FR-018 | pgTAP, Deno | `purge_account_data` leaves 0 rows in every listed table; the auth cascade removes `account_deletions` |
| FR-015, FR-016 | Deno, quickstart | OneSignal and Apple request shapes; 404 counts as done; failures stop and resume |
| FR-017 | Deno, pgTAP | Resume after each step's failure; a second run is a no-op |
| FR-019 | pgTAP, source inspection | `authenticated` can't execute the purge functions; the Api never reads `ServiceOrSecretKey` |
| FR-020 | Api, Deno | Log scopes contain `UserId` and no `Email`; edge log lines contain no `@` |
| FR-021 | Doc review | `data-retention.md` covers every store in FR-014 and the provider-held copies |

## Project Structure

### Documentation (this feature)

```text
specs/016-account-deletion/
├── plan.md              # This file
├── research.md          # Phase 0
├── data-model.md        # Phase 1
├── quickstart.md        # Phase 1 (deployment + device matrix)
├── contracts/
│   ├── api-account-deletion.md
│   └── edge-functions.md
├── data-retention.md    # FR-021, written during implementation (source for 017's privacy policy)
├── checklists/requirements.md
└── tasks.md             # Phase 2 (/speckit.tasks)
```

### Source Code (repository root)

```text
supabase/
├── migrations/<timestamp>_account_deletion.sql        # tables, helpers, RPCs, purge fn, view, policies, cron
├── tests/database/                                     # NEW pgTAP suite
│   ├── account_deletion_request_restore.test.sql
│   ├── account_deletion_visibility.test.sql
│   └── account_deletion_purge.test.sql
└── functions/
    ├── account-purge/{index.ts, purge-runner.ts, purge-runner.test.ts, deno.json}      # NEW
    ├── apple-authorization/{index.ts, deno.json}                                        # NEW
    ├── _shared/{apple-auth.ts, apple-auth.test.ts, account-status.ts, account-status.test.ts}  # NEW
    ├── notifications-dispatch/recipient-resolver.ts    # active_memberships + invitation check
    └── reminders-scheduler/index.ts                    # active_memberships

src/LoopMeet.Core/
├── Accounts/{AccountDeletionPolicy.cs}
├── Models/AccountDeletion.cs
└── Interfaces/IAccountDeletionRepository.cs
src/LoopMeet.Infrastructure/Repositories/AccountDeletionRepository.cs   # Rpc + own-row select
src/LoopMeet.Api/
├── Endpoints/AccountDeletionEndpoints.cs
├── Contracts/AccountDeletionContracts.cs
├── Services/Accounts/{AccountDeletionService.cs, AccountDeletionGateMiddleware.cs}
└── Program.cs                                          # registrations + UseMiddleware after UseAuthorization
src/LoopMeet.App/
├── Features/Account/
│   ├── ViewModels/{DeleteAccountViewModel.cs, PendingDeletionViewModel.cs}
│   ├── Views/{DeleteAccountPage.xaml(.cs), PendingDeletionPage.xaml(.cs)}
│   └── AccountDeletionGate.cs
├── Services/{AccountStatusHandler.cs, UsersApi.cs, ApiClient.cs}   # new handler in the Refit pipeline
├── Features/Auth/{AuthService.cs, AppleAuthorizationRegistrar.cs}   # code capture after Apple sign-in
├── Features/Profile/ViewModels/ProfileViewModel.cs     # DeleteAccountAsync navigates to delete-account
└── AppShell.xaml(.cs)                                  # //pending-deletion ShellContent, delete-account route

tests/
├── LoopMeet.Api.Tests/{Endpoints/AccountDeletionEndpointsTests.cs, Infrastructure/InMemoryAccountDeletionRepository.cs}
└── LoopMeet.App.Tests/Features/Account/{DeleteAccountViewModelTests.cs, PendingDeletionViewModelTests.cs, AccountDeletionSurfaceTests.cs}

.github/workflows/tests.yml                             # NEW database-tests job (supabase start + test db)
CLAUDE.md                                               # gotcha: 30-day rule in SQL + preview constant; active_memberships for edge recipients
```

**Structure Decision**: this uses the existing four-project layering and the Supabase folder. Account deletion gets its own `Accounts` module in Core and Api and a `Features/Account` folder in the app, rather than growing `Profile`, because the pending-deletion screen sits outside the signed-in tabs and is shared with no other feature.

## Complexity Tracking

| Exception | Why Needed | Simpler Alternative Rejected Because |
| --- | --- | --- |
| New pgTAP database test layer and CI job (`supabase start` takes about 2 minutes per run) | FR-006/007/019 and the purge live in SQL (RLS, `security definer`). They're the security boundary of this feature, and the in-memory Api fakes can't execute them. | Source-inspecting the migration only checks that strings are present. A policy with a wrong predicate would still pass. |
| App calls an edge function (`apple-authorization`) directly, outside the Api | The Apple authorization code expires 5 minutes after sign-in and must be exchanged with secrets that the purge also needs. Keeping exchange and revoke in one TypeScript module means one secret location. | Exchanging in the Api duplicates the ES256 client-secret logic in C# and puts Apple secrets on a second host. The call is part of Apple sign-in, which already talks to Supabase directly. |
| The 30-day value exists in SQL and in `AccountDeletionPolicy` | The confirmation screen must show the projected date before any row exists. | An Api round trip to SQL just to compute `now() + 30 days` adds a privileged-free but pointless call. Both values are pinned by tests and recorded in CLAUDE.md. |
