# Implementation Plan: Past Meetups on Group Detail

**Branch**: `012-past-meetups` | **Date**: 2026-10-30 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/012-past-meetups/spec.md`

## Summary

Group Detail gets an Upcoming / Past toggle. The past list is keyset-paged, newest first. Meetup detail and edit load a single meetup by id, so past meetups open. The group owner can edit or delete past meetups without group members getting a push. A meetup now counts as "upcoming" until the end of its scheduled day in its own timezone, and that one rule decides list membership, edit validation and push suppression.

Code review turned up five facts that shape the design:

1. **Pushes come from a database webhook, not the Api.** The only place to suppress them is the `notifications-dispatch` edge function, by comparing `old_record` with `record` ([research D6](./research.md#d6--where-push-suppression-happens)).
2. **"Past" needs a per-row timezone, and PostgREST can't apply one.** The repository narrows rows with a UTC band that is always safe, then classifies in memory. This mirrors `reminders-scheduler`'s ±25 h window. **No schema change, no new index** ([D4](./research.md#d4--filtering-lists-without-a-schema-change-bounded-utc-window--in-memory-classification)).
3. **The rule has to exist in C# and TypeScript.** Both implementations are tested against one shared JSON case file, so they can't drift apart ([D3](./research.md#d3--one-rule-two-implementations-one-shared-fixture)).
4. **"Cancel" doesn't exist**: `meetups` has no `status` column. **Deletes already never push.** FR-022/025 hold by construction and get regression tests ([D7](./research.md#d7--deletes-and-cancels-of-past-meetups-fr-022-fr-025)).
5. **Same-day reminders already can't fire for a finished day.** The scheduler's same-local-date guard does that, so a guard test is enough ([D8](./research.md#d8--same-day-reminders-never-fire-for-a-finished-day-fr-027)).

The edit fix compares the requested `ScheduledAt` with the stored value: the future-time check applies only when an *upcoming* meetup's time actually changes. Older app builds therefore also stop failing on "started earlier today" edits ([D9](./research.md#d9--edit-validation-only-when-the-time-changes)).

## Technical Context

**Language/Version**: C# 13 / .NET 10 (MAUI client + minimal-API backend). TypeScript on Deno 2.x (Supabase Edge Functions). No database schema changes.
**Primary Dependencies**: Microsoft.Maui.Controls 10.0.70, CommunityToolkit.Mvvm 8.4.2, Refit.HttpClientFactory 10.2.0, Supabase 1.1.1 (Postgrest client), `System.TimeProvider` (BCL). Deno std `@std/assert` for edge-function tests. No new NuGet packages.
**Storage**: Existing Supabase `meetups` table, columns, indexes and RLS unchanged. "Past" is derived at read time.
**Testing**: xUnit:

- Core: unit tests for `MeetupLifecycle` driven by the shared case file.
- Api: `WebApplicationFactory<Program>` + in-memory fakes, with a hand-rolled `FixedTimeProvider`, for the list cutoff, paging, single-meetup reads and edit validation.
- App: source-inspection tests for XAML and view-model wiring.

**New**: `deno test` for `meetup-lifecycle.ts` (same case file) and `resolveSuppression`, added to CI. Plus the [quickstart.md](./quickstart.md) device matrix for real push delivery and scroll/refresh behavior.
**Target Platform**: iOS, Android (primary); MacCatalyst, Windows (same behavior).
**Project Type**: Mobile app + minimal-API backend + Supabase Edge Functions (existing structure).
**Performance Goals**: First page of past meetups appears as fast as the upcoming list today, under 2 s on mobile (SC-002). The upcoming queries read at most one extra day of rows (the 26 h band). Past paging is O(limit) per request via the keyset on an existing index.
**Constraints**:

- No `CollectionView` inside the page's `ScrollView` (iOS release crash), so `BindableLayout` is used.
- `ObservableCollection` changes on the main thread.
- Style and color keys are verified to exist before use.
- The meetups RLS stays as-is (006 known gotcha).
- The Api keeps impersonating the caller's token.

**Scale/Scope**:

- Endpoints: 2 new, 3 with behavior changes.
- 1 new Core rule + 2 new value types, 1 repository extended.
- 1 edge function changed + 1 shared TS module.
- 3 view models and 1 page changed, 1 CI step.
- About 35 tests.

## Constitution Check

*GATE: evaluated against Meetloop Constitution v0.1.0 — before Phase 0 and re-checked after Phase 1.*

| Gate | Principle | Status | Notes |
| ------ | ----------- | -------- | ------- |
| G1 | I. Code Quality | PASS | The card `DataTemplate` moves to page resources and is shared by both lists rather than duplicated. The existing `DateTimeOffset.UtcNow` calls in the meetup command and query paths are replaced with an injected `TimeProvider`. There are no TODOs. The dormant `meetup.canceled` router branch is left untouched; it's out of scope and documented in D7. |
| G2 | II. Tests Required | PASS | Every FR is mapped to a test layer (see "Test strategy" below). The cutoff, paging and push decisions are unit- or integration-tested on both sides of the boundary (FR-029). TS logic gets real Deno tests running in CI rather than source inspection only (D13). Push delivery and gestures are covered by the quickstart matrix, the documented approach for native-only behavior. |
| G3 | III. UX First | PASS | 4 prioritized stories with acceptance scenarios. Each list has its own empty state, and a failed page load has a retry path. The existing "Scheduled time must be in the future." message is kept. A location-only edit no longer fails confusingly on a meetup that has started. |
| G4 | IV. Simplicity | PASS | One new abstraction, `MeetupLifecycle`, with 5 C# call sites plus 1 TS twin. No schema change, no new package, no new caching mechanism (the existing 30 s pattern is reused for one new key). Keyset paging is chosen over offset only because offset breaks FR-014 (D5). |
| G5 | V. Modularity | PASS | The rule lives in Core (no dependencies), and Infrastructure and Api consume it. Push policy stays inside the edge function that owns dispatch. The App gets `IsPast` from the server rather than a third copy of the rule (D11). No circular dependencies. |
| G6 | VI. Contract-First | PASS | [contracts/meetup-history-contracts.md](./contracts/meetup-history-contracts.md) fixes the endpoints, the cursor semantics, the edit validation table, the webhook decision table and the interface signatures. All Api changes are additive, so older clients keep working. |
| G7 | VII. Observability | PASS | Structured logs on the new endpoints (count, hasMore). The edge function emits `notification_suppressed` with its reason and ids (FR-028). Edit-validation rejections keep their existing warning logs. |
| — | Privacy / retention | PASS | No new data stored. Past meetups were already retained and RLS-visible to the same members. Suppression logs carry ids only (see [data-model.md](./data-model.md#privacy-and-retention)). |

**Post-Phase-1 re-check (2026-10-30)**: All gates still PASS. The one item needing justification, a new CI step for Deno tests, is recorded under Complexity Tracking.

## Test strategy (FR → test)

| FR | Test | Layer |
| --- | --- | --- |
| FR-007, FR-008 | `MeetupLifecycleTests` over `meetup-lifecycle.cases.json`, plus `meetup-lifecycle.test.ts` over the same file | Core xUnit + Deno |
| FR-008, FR-009 | Endpoint tests with `FixedTimeProvider` set 1 min before and 1 min after local midnight: the meetup is in exactly one of `/groups/{id}/meetups`, `/groups/{id}/meetups/past`, and in `/meetups/upcoming` only before | Api integration |
| FR-010, FR-011, FR-014 | Past list: order; `limit`; follow `nextCursor` to the end with no duplicates; a tied `scheduled_at` pair; delete between pages doesn't skip; short page with non-null cursor when band rows are skipped; `invalid_cursor` / `invalid_limit` | Api integration |
| FR-015 | `GET /groups/{g}/meetups/{id}`: past → 200 with `isPast=true`; other group → 404; missing → 404 | Api integration |
| FR-017–FR-021 | `PATCH`: past→past ok; past→future ok; upcoming with unchanged time when started today ok; upcoming changed to past → 400; upcoming changed to future ok. Plus source-inspection that `EditMeetupViewModel` gates the future check on "time changed and not past" and sends `ScheduledAt`/`Timezone` only when changed | Api integration + App source-inspection |
| FR-022 | `DELETE` of a past meetup → 204 and gone from the past list | Api integration |
| FR-023–FR-026 | `webhook-router.test.ts`: `resolveSuppression` for past→past, past→future, upcoming→past, upcoming→upcoming, past→past with `status: "canceled"`, `DELETE`, `INSERT`, other tables; `resolveNotificationType` still returns `null` for `DELETE` | Deno |
| FR-027 | Source-inspection that `reminders-scheduler/index.ts` keeps the same-local-date guard | Api xUnit (existing pattern) |
| FR-028 | Source-inspection that `index.ts` logs `notification_suppressed` with `reason` before resolving the type | Api xUnit |
| FR-001–FR-006, FR-012, FR-013 | Source-inspection of `GroupDetailPage.xaml(.cs)` and `GroupDetailViewModel.cs`: toggle commands and labels, both empty states, `RefreshView` command, shared card template, mode reset in `ApplyQueryAttributes` (not `OnAppearing`), scroll-near-end load-more, `PastMeetups` binding | App source-inspection |
| Cache invalidation | Endpoint test: create/update/delete then immediately re-read the past list's first page and see the change | Api integration |

## Project Structure

### Documentation (this feature)

```text
specs/012-past-meetups/
├── plan.md              # This file
├── research.md          # Phase 0 — decisions D1..D13
├── data-model.md        # Phase 1 — lifecycle rule, page/cursor types, cache keys
├── quickstart.md        # Phase 1 — device + push validation matrix
├── checklists/
│   └── requirements.md  # Spec quality checklist (complete)
├── contracts/
│   └── meetup-history-contracts.md   # Phase 1
└── tasks.md             # Phase 2 output (/speckit.tasks — NOT created here)
```

### Source Code (repository root)

```text
src/LoopMeet.Core/
├── Meetups/MeetupLifecycle.cs                 # NEW: IsPast rule, fallback tz, 26 h band
├── Meetups/MeetupPage.cs                      # NEW: MeetupPageCursor, MeetupPage
└── Interfaces/IMeetupRepository.cs            # MODIFIED: + now params; + ListPastByGroupAsync

src/LoopMeet.Infrastructure/Repositories/
└── MeetupRepository.cs                        # MODIFIED: band-widened upcoming queries +
                                               #   in-memory classify; keyset past query

src/LoopMeet.Api/
├── Contracts/MeetupContracts.cs               # MODIFIED: + IsPast; + PastMeetupsResponse
├── Endpoints/MeetupsEndpoints.cs              # MODIFIED: + GET past, + GET by id
├── Services/Meetups/MeetupQueryService.cs     # MODIFIED: TimeProvider; past page (+cache key);
│                                              #   single read; IsPast in ProjectAsync
├── Services/Meetups/MeetupCommandService.cs   # MODIFIED: TimeProvider; changed-time validation;
│                                              #   invalidate meetups:past:{groupId}
├── Services/Meetups/MeetupCursorCodec.cs      # NEW: opaque cursor encode/decode
└── Program.cs                                 # MODIFIED: register TimeProvider.System

supabase/functions/
├── _shared/meetup-lifecycle.ts                # NEW: isMeetupPast
├── _shared/meetup-lifecycle.cases.json        # NEW: shared C#/TS fixture
├── _shared/meetup-lifecycle.test.ts           # NEW
├── notifications-dispatch/webhook-router.ts   # MODIFIED: + resolveSuppression
├── notifications-dispatch/webhook-router.test.ts  # NEW
├── notifications-dispatch/index.ts            # MODIFIED: suppression check + structured log
└── notifications-dispatch/deno.json           # MODIFIED: + @std/assert import

src/LoopMeet.App/
├── Services/MeetupsApi.cs                     # MODIFIED: + GetPastMeetupsAsync, GetMeetupAsync
├── Features/Meetups/Models/MeetupModels.cs    # MODIFIED: + IsPast; + PastMeetupsResponse
├── Features/Meetups/ViewModels/
│   ├── MeetupDetailViewModel.cs               # MODIFIED: load by id; 404 → not-found
│   └── EditMeetupViewModel.cs                 # MODIFIED: load by id; changed-time rule;
│                                              #   send ScheduledAt/Timezone only if changed
├── Features/Groups/ViewModels/GroupDetailViewModel.cs  # MODIFIED: list mode, PastMeetups,
│                                              #   paging, refresh, delete from either list
└── Features/Groups/Views/
    ├── GroupDetailPage.xaml                   # MODIFIED: toggle, shared card template,
    │                                          #   two lists + empty states, RefreshView
    └── GroupDetailPage.xaml.cs                # MODIFIED: reset mode on navigation;
                                               #   scroll-near-end → load more

tests/
├── LoopMeet.Core.Tests/Meetups/MeetupLifecycleTests.cs          # NEW (reads shared fixture)
├── LoopMeet.Api.Tests/
│   ├── Infrastructure/InMemoryMeetupRepository.cs               # MODIFIED: uses MeetupLifecycle; paging
│   ├── Infrastructure/FixedTimeProvider.cs                      # NEW: hand-rolled TimeProvider
│   ├── Infrastructure/TestWebApplicationFactory.cs              # MODIFIED: swap in FixedTimeProvider
│   ├── Endpoints/MeetupHistoryEndpointsTests.cs                 # NEW: lists, paging, by-id, PATCH rules
│   └── Integration/PastMeetupNotificationGuardTests.cs          # NEW: source-inspection (FR-027, FR-028)
└── LoopMeet.App.Tests/Features/
    ├── Groups/GroupDetailPastMeetupsSurfaceTests.cs             # NEW: source-inspection
    └── Meetups/EditMeetupViewModelTests.cs                      # MODIFIED: changed-time rule assertions

.github/workflows/tests.yml                    # MODIFIED: setup-deno + `deno test supabase/functions`
```

**Structure Decision**: Existing layered structure, unchanged. One new Core folder, `Meetups/`, holds the lifecycle rule and paging types because they're pure domain logic shared by Infrastructure and Api. Edge-function sharing uses the existing `supabase/functions/_shared/` convention. Tests stay next to their runtime: C# tests under `tests/`, and Deno tests next to the module, as is idiomatic for Deno.

## Rollout notes

- **Deploy order**: Api first (all its changes are additive and backward compatible), then `notifications-dispatch` (`supabase functions deploy notifications-dispatch`), then the app. An old app on the new Api already gets the relaxed edit validation and the end-of-day upcoming lists. It just has no Past toggle.
- **Behavior change users will notice**: meetups now stay in Upcoming lists (including Home) for the rest of their day. This is intended (User Story 4) and worth a line in release notes.

## Complexity Tracking

| Addition | Why needed | Simpler alternative rejected because |
| --- | --- | --- |
| Deno test step in `tests.yml` | Constitution II requires deterministic tests that run in CI. Timezone date arithmetic and the suppression decision table can't be checked by source inspection. | Source-inspection only: it can't catch a wrong date comparison, the exact failure mode that would cause wrong or missing pushes. |
| Rule implemented twice (C# + TS) | The webhook runs in Deno and the lists in .NET; they can't share code. | A Postgres function as the single source: a schema change the spec asks to avoid, plus a round trip on every webhook. The shared case file keeps both implementations honest instead. |
