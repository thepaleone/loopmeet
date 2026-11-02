# Tasks: Past Meetups on Group Detail

**Input**: Design documents from `/specs/012-past-meetups/`
**Prerequisites**: plan.md (loaded), spec.md (4 user stories, P1–P3, FR-001–FR-029), research.md (decisions D1–D13), data-model.md (lifecycle rule, page/cursor, cache keys), contracts/meetup-history-contracts.md (§1–§8), quickstart.md (15-row device matrix)

**Tests**: Included. Constitution Principle II makes tests a required deliverable, and FR-029 names the cutoff regression tests explicitly. Each test sits at the layer that owns the behavior:

- Core xUnit for the lifecycle rule
- Deno tests for the edge-function rule and the suppression decision
- Api endpoint tests (with a fixed clock) for lists, paging and edit validation
- Source-inspection tests for XAML and view-model wiring
- The quickstart device matrix for real push delivery and gestures

**Organization**: Tasks are grouped by user story. Phase 2 provides the shared "is this meetup past?" rule in both runtimes plus the injectable clock; every story depends on it.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1–US4 per spec.md. Setup, Foundational and Polish phases have no story label.

## Path Conventions

- Backend: `src/LoopMeet.Core/`, `src/LoopMeet.Infrastructure/`, `src/LoopMeet.Api/`
- Edge functions: `supabase/functions/`
- Client: `src/LoopMeet.App/`
- Tests: `tests/LoopMeet.Core.Tests/`, `tests/LoopMeet.Api.Tests/`, `tests/LoopMeet.App.Tests/`, plus `*.test.ts` next to the Deno modules

---

## Phase 1: Setup

**Purpose**: A known-good baseline, so any later failure can be traced to this feature.

- [X] T001 From the repo root, run `dotnet build LoopMeet.slnx -c Debug -p:SkipMaciOSTargets=true` and `dotnet test LoopMeet.slnx -c Debug -p:SkipMaciOSTargets=true`. Record the build warning count and the passing/failing test counts as the baseline in this file's Notes section.
- [X] T002 [P] Run `deno --version` and `(cd supabase/functions/notifications-dispatch && deno check index.ts)` and `(cd supabase/functions/reminders-scheduler && deno check index.ts)`. Each function's `deno.json` import map only applies when run from inside that folder and record the Deno version and a clean type-check in this file's Notes section. If the type-check already fails on `main`, record the errors so they aren't blamed on this feature.

---

## Phase 2: Foundational: Lifecycle Rule, Clock, Read-Model Flag

**Purpose**: The single "past" rule (research D1/D2) in C# and TypeScript, proven equal by one shared fixture (D3). Also an injectable clock (D9), the `now`-taking repository signatures (contract §7), the `IsPast` read-model flag (D11), and Deno tests in CI (D13).

**⚠️ Blocks all user stories.**

**⚠️ Sequencing note**: T009 changes repository signatures that `MeetupQueryService` and the in-memory fake consume, so the tree doesn't compile between T009 and T011. Land T009 → T010 → T011 as one change. Phase 2 is never merged on its own: T010's temporary `NotSupportedException` stubs exist only until T018/T019 replace them (Constitution I).

### The rule, in both runtimes

- [X] T003 Create `supabase/functions/_shared/meetup-lifecycle.cases.json`: a JSON array of `{ "name", "scheduledAt", "timezone", "now", "expectedIsPast" }` objects (ISO-8601 instants with explicit offsets, `timezone` nullable). Cover every case listed in research D3:
  - 1 minute before, exactly at, and 1 minute after local midnight in `America/New_York` (negative offset), `Australia/Sydney` (positive) and `Asia/Kolkata` (half hour)
  - A meetup earlier the same local day (`false`)
  - The US spring-forward and fall-back days in `America/Los_Angeles`
  - A midnight-DST day in `America/Santiago`
  - `timezone: null` and `timezone: "Not/AZone"`, both classified as `America/Los_Angeles`
  - A meetup 30 days in the future (`false`)
  - A meetup 30 days in the past (`true`)
- [X] T004 [P] Create `src/LoopMeet.Core/Meetups/MeetupLifecycle.cs`: `public static class MeetupLifecycle` with `DefaultTimezone = "America/Los_Angeles"`, `ClassificationBand = TimeSpan.FromHours(26)`, and `IsPast(DateTimeOffset scheduledAt, string? timezone, DateTimeOffset now)`.
  - `IsPast` resolves the zone with `TimeZoneInfo.FindSystemTimeZoneById`. On a null or blank id, `TimeZoneNotFoundException` or `InvalidTimeZoneException`, it uses the default zone.
  - It returns `DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, tz).DateTime) > DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(scheduledAt, tz).DateTime)`.
  - Cache resolved `TimeZoneInfo`s in a `ConcurrentDictionary<string, TimeZoneInfo>`.
  - Add a one-line XML doc comment stating the rule and pointing at the shared case file.
- [X] T005 [P] Create `tests/LoopMeet.Core.Tests/Meetups/MeetupLifecycleTests.cs`: a `[Theory]` fed by a `MemberData` source that reads `supabase/functions/_shared/meetup-lifecycle.cases.json` (via `Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../supabase/functions/_shared/meetup-lifecycle.cases.json"))`, the repo's existing pattern) and asserts `MeetupLifecycle.IsPast` equals `expectedIsPast` for every case, using `name` as the display name. Add one `[Fact]` asserting that the file has at least 15 cases, so an accidentally emptied fixture fails loudly.
- [X] T006 [P] Create `supabase/functions/_shared/meetup-lifecycle.ts` per contract §6:
  - Exports `DEFAULT_MEETUP_TIMEZONE` and `isMeetupPast(scheduledAt: string | Date, timezone: string | null | undefined, now: Date): boolean`.
  - Resolves the zone by constructing `Intl.DateTimeFormat("en-CA", { timeZone, year: "numeric", month: "2-digit", day: "2-digit" })` inside `try`, falling back to the default zone on a blank id or `RangeError`.
  - Returns whether the formatted local date (`YYYY-MM-DD`) of `now` is lexically greater than that of `scheduledAt`.
  - No imports beyond the standard library.
- [X] T007 [P] Create `supabase/functions/_shared/meetup-lifecycle.test.ts`: import the cases with `import cases from "./meetup-lifecycle.cases.json" with { type: "json" };`, register one `Deno.test` per case (named by `name`) asserting `isMeetupPast(...) === expectedIsPast` with `import { assertEquals } from "jsr:@std/assert@1";`, and add one test asserting at least 15 cases.
- [X] T008 Run `deno test supabase/functions/_shared/` from the repo root and confirm all cases pass. Running T005 and T007 green against the same file proves the two implementations agree (FR-008). Test files import `jsr:@std/assert@1` **directly**. Don't add an import map or a new `deno.json`: run from the repo root (as CI does), Deno ignores `deno.json` files in subfolders, so a bare `@std/assert` fails to type-check (verified 2026-10-30).

### Clock, repository signatures, read-model flag

- [X] T009 Create `src/LoopMeet.Core/Meetups/MeetupPage.cs` with `public readonly record struct MeetupPageCursor(DateTimeOffset ScheduledAt, Guid Id);` and `public sealed record MeetupPage(IReadOnlyList<Meetup> Items, MeetupPageCursor? NextCursor);`. In `src/LoopMeet.Core/Interfaces/IMeetupRepository.cs`, add a `DateTimeOffset now` parameter (after the id) to `ListUpcomingByGroupAsync` and `ListUpcomingByUserAsync`, and add `Task<MeetupPage> ListPastByGroupAsync(Guid groupId, DateTimeOffset now, MeetupPageCursor? before, int limit, CancellationToken cancellationToken = default);` (contract §7).
- [X] T010 In `src/LoopMeet.Infrastructure/Repositories/MeetupRepository.cs`, take the new `now` parameter in both upcoming methods in place of `DateTimeOffset.UtcNow`, keeping today's `scheduled_at > now` filter for now (US4 widens it). Add `ListPastByGroupAsync` as a stub that throws `NotSupportedException`; US1 (T018) replaces it, and the stub must not survive into a merged commit (Constitution I). In `tests/LoopMeet.Api.Tests/Infrastructure/InMemoryMeetupRepository.cs`, mirror the same signatures, with the fake's `ListPastByGroupAsync` likewise stubbed until T019.
- [X] T011 Inject `TimeProvider` into `src/LoopMeet.Api/Services/Meetups/MeetupQueryService.cs` and `src/LoopMeet.Api/Services/Meetups/MeetupCommandService.cs`, replacing every `DateTimeOffset.UtcNow` in both with `_timeProvider.GetUtcNow()`. Pass `now` into the repository calls. Register `builder.Services.AddSingleton(TimeProvider.System);` in `src/LoopMeet.Api/Program.cs` next to the meetup service registrations.
- [X] T012 [P] Create `tests/LoopMeet.Api.Tests/Infrastructure/FixedTimeProvider.cs`: `public sealed class FixedTimeProvider : TimeProvider` with a settable `DateTimeOffset UtcNow` (defaulting to the real current time at construction) and `GetUtcNow()` returning it. Hand-rolled, no mocking library. In `tests/LoopMeet.Api.Tests/Infrastructure/TestWebApplicationFactory.cs`, register a single `FixedTimeProvider` instance (exposed as a public `Clock` property on the factory) in place of `TimeProvider`, inside the existing `ConfigureServices` block.
- [X] T013 Add `public bool IsPast { get; init; }` to `MeetupListItemResponse` in `src/LoopMeet.Api/Contracts/MeetupContracts.cs`, with an XML doc line ("Derived at read time via MeetupLifecycle; may be up to 30 s stale on cached lists"). In `MeetupQueryService.ProjectAsync`, add a `DateTimeOffset now` parameter and set `IsPast = MeetupLifecycle.IsPast(meetup.ScheduledAt, meetup.Timezone, now)`. Both existing callers pass the `now` they already obtained.
- [X] T014 [P] Add `public bool IsPast { get; set; }` to `MeetupSummary` in `src/LoopMeet.App/Features/Meetups/Models/MeetupModels.cs`, matching the class's existing `{ get; set; }` accessor style.
- [X] T015 [P] In `.github/workflows/tests.yml`, after "Run tests", add a `denoland/setup-deno@v2` step (`deno-version: v2.x`), a "Lint edge functions" step running `deno lint supabase/functions/`, and a "Run edge-function tests" step running `deno test supabase/functions/` (research D13; Constitution quality gate: static analysis/linting).

**Checkpoint**: `dotnet test` and `deno test supabase/functions/` are green, and the counts match the T001 baseline plus the new lifecycle tests. No user-visible change yet.

---

## Phase 3: User Story 1: Browse a group's past meetups (Priority: P1) 🎯 MVP

**Goal**: Members can switch Group Detail to "Past Meetups", see the group's history newest first, load older meetups by scrolling, and open any past meetup.

**Independent Test**: Seed a group with 45+ past meetups and a few future ones. Open Group Detail and confirm the toggle defaults to Upcoming. Switch to Past: only past meetups appear, newest first, older ones load on scroll with no duplicates, and tapping one opens its detail (quickstart rows 1–7, 15).

### Tests for User Story 1

- [X] T016 [P] [US1] Create `tests/LoopMeet.Api.Tests/Endpoints/MeetupHistoryEndpointsTests.cs` with US1 coverage of contract §1/§2. Set `factory.Clock.UtcNow` explicitly in every test, and seed meetups, groups, memberships and users through `InMemoryStore` (copy the seeding helpers already used in `tests/LoopMeet.Api.Tests/Endpoints/MeetupsEndpointsTests.cs`). Cases:
  - `GET /groups/{g}/meetups/past` returns only past meetups, ordered `scheduledAt` desc, with `isPast=true` and organizer/group fields populated.
  - Default `limit` is 20.
  - Following `nextCursor` until it's `null` over 45 seeded meetups returns all 45 exactly once.
  - Two meetups with identical `scheduledAt` both appear exactly once across a page boundary.
  - Deleting an already-returned meetup between page 1 and page 2 doesn't skip any remaining meetup.
  - A meetup in the band that is still upcoming (started earlier today in its zone) is excluded, and a page shortened by that exclusion still carries a non-null `nextCursor` when older meetups exist.
  - `?cursor=garbage` → 400 `invalid_cursor`; `?limit=0` and `?limit=51` → 400 `invalid_limit`.
  - `GET /groups/{g}/meetups/{id}` for a past meetup → 200 with `isPast=true`; for a meetup in another group → 404; for an unknown id → 404.
  - Cache: read the past first page, `DELETE` one of its meetups, re-read → the deleted meetup is gone (proves `meetups:past:{groupId}` invalidation).
- [X] T017 [P] [US1] Create `tests/LoopMeet.App.Tests/Features/Groups/GroupDetailPastMeetupsSurfaceTests.cs` (source inspection; copy the `ReadSource` helper pattern from `tests/LoopMeet.App.Tests/Features/Meetups/MeetupInteractionSurfaceTests.cs`) asserting:
  - `GroupDetailPage.xaml` contains both labels "Upcoming Meetups" and "Past Meetups", `SelectMeetupListCommand`, `{Binding PastMeetups}`, a `RefreshView` bound to `RefreshMeetupsCommand`, both empty-state messages ("No upcoming meetups" and "No past meetups yet"), the load-more retry text, and a shared card template referenced via `StaticResource` from both lists.
  - `GroupDetailPage.xaml` does **not** contain `IsVisible="{Binding HasMeetups}"` on the section heading (the toggle is always visible, FR-003) and contains no `CollectionView` (BindableLayout rule).
  - `GroupDetailPage.xaml.cs` resets the list mode inside `ApplyQueryAttributes` and not inside `OnAppearing`, and wires `Scrolled` to `LoadMorePastMeetupsCommand`.
  - `GroupDetailViewModel.cs` calls `GetPastMeetupsAsync`, keeps `NextCursor` state, and uses `MainThread` for collection updates.
  - `MeetupDetailViewModel.cs` calls `GetMeetupAsync(_groupId, _meetupId)` and handles `HttpStatusCode.NotFound`.

### Implementation for User Story 1 — backend

- [X] T018 [US1] Implement `ListPastByGroupAsync` in `src/LoopMeet.Infrastructure/Repositories/MeetupRepository.cs` per research D4/D5:
  - Query `group_id = groupId AND scheduled_at <= now`, ordered `scheduled_at` desc then `id` desc, limited to `limit`.
  - When `before` is set, add the keyset `or=(scheduled_at.lt.{ts},and(scheduled_at.eq.{ts},id.lt.{id}))` through `QueryFilter` with `Operator.Or`/`Operator.And`. If the client can't express the nested `and`, use the documented fallback (`scheduled_at <= ts`, then drop rows with `scheduled_at == ts && id >= before.Id` in memory) and say so in a code comment.
  - Map rows, keep only those where `MeetupLifecycle.IsPast(...)`, and set the cursor to the last raw row **examined**.
  - Repeat raw batches until `limit` past rows are collected or a batch returns fewer than `limit` rows. `NextCursor` is `null` exactly when the last raw batch was short.
  - Remove the T010 stub.
- [X] T019 [US1] Implement `ListPastByGroupAsync` in `tests/LoopMeet.Api.Tests/Infrastructure/InMemoryMeetupRepository.cs` with the same semantics (keyset on `(ScheduledAt desc, Id desc)`, `MeetupLifecycle.IsPast` filter, cursor = last examined, null when exhausted), and remove the T010 stub there.
- [X] T020 [P] [US1] Create `src/LoopMeet.Api/Services/Meetups/MeetupCursorCodec.cs`: `internal static class MeetupCursorCodec` with `string Encode(MeetupPageCursor cursor)` (base64url of `"{ScheduledAt.UtcTicks}:{Id:N}"`) and `bool TryDecode(string? value, out MeetupPageCursor cursor)`, which returns `false` on any malformed input and never throws.
- [X] T021 [US1] Add `PastMeetupsResponse { IReadOnlyList<MeetupListItemResponse> Meetups; string? NextCursor; }` to `src/LoopMeet.Api/Contracts/MeetupContracts.cs`. In `src/LoopMeet.Api/Services/Meetups/MeetupQueryService.cs`, add:
  - `GetPastGroupMeetupsAsync(Guid groupId, MeetupPageCursor? before, int limit, CancellationToken)`. It projects via `ProjectAsync` and encodes `NextCursor`. It goes through the 30 s cache under `meetups:past:{groupId}` **only** when `before` is null and `limit` is the default 20. It logs `Loading past meetups for group {GroupId} cursor={HasCursor} limit={Limit}` and `Loaded past meetups for group {GroupId} count={Count} hasMore={HasMore}`.
  - `GetGroupMeetupAsync(Guid groupId, Guid meetupId, CancellationToken)`. It calls `GetByIdAsync`, returns `null` when missing or `GroupId != groupId`, and otherwise returns the single projected item. Not cached (research D10).
- [X] T022 [US1] In `src/LoopMeet.Api/Endpoints/MeetupsEndpoints.cs`, add `GET /groups/{groupId:guid}/meetups/past` with `string? cursor` and `int? limit` query parameters.
  - Validate `limit` in 1–50 (default 20) → `400 { code: "invalid_limit", message: "Limit must be between 1 and 50." }`.
  - Decode `cursor` with `MeetupCursorCodec.TryDecode` → `400 { code: "invalid_cursor", message: "The paging cursor is invalid. Refresh and try again." }`.
  - Add `GET /groups/{groupId:guid}/meetups/{meetupId:guid}` returning 200 or 404.
  - Map `/past` **before** the `{meetupId:guid}` route; the guid constraint already prevents a clash, but declaring it first makes the intent obvious.
  - Both routes `.RequireAuthorization()`, with entry/exit log lines matching the existing style in that file.
- [X] T023 [US1] In `src/LoopMeet.Api/Services/Meetups/MeetupCommandService.cs`, add `await _cacheService.RemoveAsync($"meetups:past:{groupId}");` next to every existing `RemoveAsync($"meetups:{groupId}")` call (create, update, delete), then run T016 to green.

### Implementation for User Story 1 — client

- [X] T024 [P] [US1] In `src/LoopMeet.App/Features/Meetups/Models/MeetupModels.cs`, add `public sealed class PastMeetupsResponse { public List<MeetupSummary> Meetups { get; set; } = new(); public string? NextCursor { get; set; } }`. In `src/LoopMeet.App/Services/MeetupsApi.cs`, add the two Refit methods from contract §8 (`GetPastMeetupsAsync(Guid groupId, [Query] string? cursor = null, [Query] int? limit = null)` and `GetMeetupAsync(Guid groupId, Guid meetupId)`) and matching pass-through methods on the `MeetupsApi` wrapper.
- [X] T025 [US1] In `src/LoopMeet.App/Features/Meetups/ViewModels/MeetupDetailViewModel.cs`, replace the "fetch the upcoming list and find the id" load with `await _meetupsApi.GetMeetupAsync(_groupId, _meetupId)`. Map `Refit.ApiException { StatusCode: HttpStatusCode.NotFound }` to the existing not-found branch (same state changes and log line), and update its comment, which currently says lists are upcoming-only. In `tests/LoopMeet.App.Tests/Features/Meetups/MeetupInteractionSurfaceTests.cs` (`MeetupDetailViewModel_LoadsFromIdsAndOffersNoDelete`), change the asserted string from `GetGroupMeetupsAsync(_groupId)` to `GetMeetupAsync(_groupId, _meetupId)`. All other assertions in that test stay.
- [X] T026 [US1] Extend `src/LoopMeet.App/Features/Groups/ViewModels/GroupDetailViewModel.cs`:
  - Create a top-level `public enum MeetupListMode { Upcoming, Past }` in `src/LoopMeet.App/Features/Groups/ViewModels/MeetupListMode.cs` (not nested in the view model, so XAML `x:Static` can reference it plainly), and add `[ObservableProperty] MeetupListMode _listMode`, with computed `IsUpcomingSelected` / `IsPastSelected` (raise change notifications from `OnListModeChanged`).
  - Add `ObservableCollection<MeetupSummary> PastMeetups`, plus `HasPastMeetups`, `IsLoadingMorePast`, `HasLoadMoreError`, a private `string? _pastNextCursor` and `bool _pastLoaded`.
  - `ResetListMode()` sets Upcoming and clears the past state (called on navigation).
  - `[RelayCommand] SelectMeetupListAsync(MeetupListMode mode)` sets the mode and loads the first past page only if `!_pastLoaded`. It never touches the group or member data (FR-005).
  - `[RelayCommand] RefreshMeetupsAsync()` reloads only the selected list; the past list restarts from page 1 (FR-006). It sets a bound `IsRefreshing` to false when done.
  - `[RelayCommand] LoadMorePastMeetupsAsync()` returns early when loading, when `_pastNextCursor` is null after a load, or when Upcoming is selected. On success it appends, skipping any id already present as a safety net. On failure it sets `HasLoadMoreError = true` and keeps the loaded items.
  - Wrap every collection mutation in `MainThread.InvokeOnMainThreadAsync` (memory: background updates are dropped).
  - `LoadAsync` (runs on every appearance) reloads the group, the members and the **selected** list, so returning from an edit refreshes the past list in place.
  - `DeleteMeetupAsync` removes the item from whichever collection contains it and updates the matching `Has…` flag.
  - Keep the existing `Meetups` name and `HasMeetups` for the upcoming list (011's source-inspection tests bind to them).
- [X] T027 [US1] Restructure `src/LoopMeet.App/Features/Groups/Views/GroupDetailPage.xaml`. **Before** writing any `StaticResource`/`DynamicResource`, confirm every key exists in `src/LoopMeet.App/Resources/Styles/Colors.xaml` or `Styles.xaml` (memory: missing keys fail silently).
  - Move the meetup card `DataTemplate` (the whole `SwipeView` … `</SwipeView>` block, unchanged so 011's assertions on it still pass) into `<ContentPage.Resources>` as `x:Key="MeetupCardTemplate"`.
  - Replace the "Upcoming Meetups" label with a segmented toggle: a rounded `Border` holding a two-column `Grid` of `Button`s "Upcoming Meetups" / "Past Meetups", each running `SelectMeetupListCommand` with an `x:Static` `MeetupListMode` parameter, and with `DataTrigger`s on `IsUpcomingSelected` / `IsPastSelected` for the selected look. Always visible.
  - Render two `VerticalStackLayout` lists via `BindableLayout.ItemsSource` (`Meetups` / `PastMeetups`), both using `BindableLayout.ItemTemplate="{StaticResource MeetupCardTemplate}"`, visible per mode and non-empty state.
  - Add one empty-state `Label` per list: "No upcoming meetups" / "No past meetups yet".
  - Below the past list, add an `ActivityIndicator` bound to `IsLoadingMorePast` and a tappable "Couldn't load more — tap to retry" label bound to `HasLoadMoreError` → `LoadMorePastMeetupsCommand`.
  - Wrap the existing `ScrollView` in a `RefreshView` (`Command="{Binding RefreshMeetupsCommand}"`, `IsRefreshing="{Binding IsRefreshing}"`), and give the `ScrollView` `x:Name="ContentScroll"` and `Scrolled="OnContentScrolled"`.
  - Leave the FABs outside the `RefreshView`, as now.
- [X] T028 [US1] In `src/LoopMeet.App/Features/Groups/Views/GroupDetailPage.xaml.cs`: in `ApplyQueryAttributes`, call `viewModel.ResetListMode()` after setting `GroupId` (FR-002: reset on open only), and leave `OnAppearing` calling `LoadCommand` without resetting. Add `OnContentScrolled(object? sender, ScrolledEventArgs e)`: when `viewModel.IsPastSelected` and `e.ScrollY >= ContentScroll.ContentSize.Height - ContentScroll.Height - 300` and `LoadMorePastMeetupsCommand.CanExecute(null)`, execute it. Then run T017 and the full App test project to green.

**Checkpoint**: US1 is fully usable. Quickstart rows 1–7 and 15 pass on a device. Detail opens past meetups (read-only for members).

---

## Phase 4: User Story 2: Correct a past meetup without alerting the group (Priority: P2)

**Goal**: The group owner can edit any field of a past meetup (including moving it to another past date) and delete it, and group members get no push for either.

**Independent Test**: As owner, edit a past meetup's location and save, then move it to another past date and save, then delete one. Every save succeeds, and the member device receives nothing (quickstart rows 8, 9, 11 + log check).

### Tests for User Story 2

- [X] T029 [P] [US2] Extend `tests/LoopMeet.Api.Tests/Endpoints/MeetupHistoryEndpointsTests.cs` with `PATCH` cases per contract §5, setting `factory.Clock` explicitly:
  - Past meetup, location-only change (`scheduledAt` omitted) → 200.
  - Past meetup, `scheduledAt` resent unchanged → 200.
  - Past meetup moved to an earlier past instant → 200, and it appears at its new position in `/past`.
  - Upcoming meetup, `scheduledAt` changed to a future instant → 200.
  - Upcoming meetup, `scheduledAt` changed to a past instant → 400 `invalid_meetup_schedule`.
  - `DELETE` of a past meetup → 204, and it's gone from `/past` (FR-022).
- [X] T030 [P] [US2] Create `supabase/functions/notifications-dispatch/webhook-router.test.ts` testing `resolveSuppression(payload, now)` with a fixed `now`. Import assertions with `import { assertEquals } from "jsr:@std/assert@1";` (see T008):
  - `meetups` UPDATE past→past → `"past_meetup"`.
  - past→past where `record.status` is `"canceled"` → `"past_meetup"`. Future-proofing only: meetups have no status column today, but suppression must keep running before the status branch (research D7).
  - upcoming→upcoming → `null`.
  - upcoming→past → `null` (old record was upcoming, FR-026).
  - `meetups` INSERT → `null`; `meetups` DELETE → `null`; `invitations` INSERT → `null`; UPDATE with `old_record` missing → `null`.
  - Also assert `resolveNotificationType` still returns `null` for a `meetups` DELETE (deletes never push, FR-025) and `"meetup.updated"` for a plain UPDATE.
- [X] T031 [P] [US2] Create `tests/LoopMeet.Api.Tests/Integration/PastMeetupNotificationGuardTests.cs` (source inspection, same path pattern as `WebhookNotificationMappingTests.cs`):
  - (a) `supabase/functions/notifications-dispatch/index.ts` calls `resolveSuppression(` **before** `resolveNotificationType(` (compare `IndexOf`), and contains `"notification_suppressed"` and `reason` (FR-028).
  - (b) `supabase/functions/reminders-scheduler/index.ts` still contains `localScheduled.isoDate !== localNow.isoDate` (FR-027, research D8).
  - (c) `webhook-router.ts` imports `isMeetupPast` from `../_shared/meetup-lifecycle.ts`.

### Implementation for User Story 2 — backend validation

- [X] T032 [US2] In `MeetupCommandService.UpdateAsync` (`src/LoopMeet.Api/Services/Meetups/MeetupCommandService.cs`), replace the `ScheduledAt` block with the research D9 rule:
  - `var now = _timeProvider.GetUtcNow(); var wasPast = MeetupLifecycle.IsPast(existing.ScheduledAt, existing.Timezone, now);`
  - Only when `request.ScheduledAt is { } requested && requested != existing.ScheduledAt`: reject with `InvalidSchedule` if `!wasPast && requested <= now` (keep the existing warning log, adding `wasPast` to it), otherwise assign.
  - Leave `CreateAsync`'s future-only rule untouched.
  - Run T029 to green.

### Implementation for User Story 2 — push suppression

- [X] T033 [US2] In `supabase/functions/notifications-dispatch/webhook-router.ts`, add `import { isMeetupPast } from "../_shared/meetup-lifecycle.ts";`, `export type SuppressionReason = "past_meetup";`, and `export const resolveSuppression = (payload: WebhookPayload, now: Date): SuppressionReason | null`. It returns `"past_meetup"` only when `table === "meetups"`, `type === "UPDATE"`, `old_record` is present, and `isMeetupPast` is true for both `old_record` and `record` (reading `scheduled_at` as a string and `timezone` as string-or-null). Otherwise it returns `null`. Leave `resolveNotificationType` unchanged. Run T030 to green.
- [X] T034 [US2] In `supabase/functions/notifications-dispatch/index.ts`, right after parsing `payload`:
  - Call `const suppression = resolveSuppression(payload, new Date());`.
  - When it's non-null, `console.log(JSON.stringify({ level: "info", message: "notification_suppressed", reason: suppression, table: payload.table, type: payload.type, meetupId: payload.record.id ?? null, groupId: payload.record.group_id ?? null }))` (same JSON log style as `reminders-scheduler`), then return `Response.json({ skipped: true, reason: suppression })` before `resolveNotificationType`.
  - Run `(cd supabase/functions/notifications-dispatch && deno check index.ts)`, then T031 to green.

### Implementation for User Story 2 — client edit

- [X] T035 [US2] In `src/LoopMeet.App/Features/Meetups/ViewModels/EditMeetupViewModel.cs`:
  - Load via `_meetupsApi.GetMeetupAsync(GroupId, MeetupId)` (keep the existing "Could not load meetup details." message on failure), and store `_loadedScheduledAt` (`DateTimeOffset`) and `_loadedIsPast` (`bool`).
  - In `SaveAsync`, compute `var timeChanged = TruncateToMinute(scheduledAt) != TruncateToMinute(_loadedScheduledAt);`, using a private static helper that zeroes seconds and sub-seconds.
  - Show the existing "Scheduled time must be in the future." error only when `timeChanged && !_loadedIsPast && scheduledAt <= DateTimeOffset.Now`.
  - Send `ScheduledAt = timeChanged ? scheduledAt : null` and `Timezone = timeChanged ? TimezoneHelper.GetCurrentDeviceTimezoneId() : null` (research D9).
- [X] T036 [US2] Extend `tests/LoopMeet.App.Tests/Features/Meetups/EditMeetupViewModelTests.cs` with a source-inspection `[Fact]` asserting `EditMeetupViewModel.cs` contains `GetMeetupAsync(GroupId, MeetupId)`, `timeChanged && !_loadedIsPast`, `ScheduledAt = timeChanged ? scheduledAt : null`, and `Timezone = timeChanged ?` (FR-021a), and no longer contains `GetGroupMeetupsAsync`.

**Checkpoint**: Quickstart rows 8, 9 and 11 pass, with the `notification_suppressed` log line visible for rows 8–9.

---

## Phase 5: User Story 3: Move a past meetup back into the future (Priority: P3)

**Goal**: Moving a past meetup's date into the future makes it upcoming again everywhere and sends the normal "Meetup Updated" push.

**Independent Test**: As owner, change a past meetup's date to tomorrow. It leaves Past, appears in Upcoming on Group Detail and the member's Home tab, and the member receives "LoopMeet - Meetup Updated" (quickstart row 10).

The behavior is produced by US2's rule (T032) and suppression predicate (T033). This phase pins it with tests so a later change can't silently break it.

- [X] T037 [P] [US3] Extend `tests/LoopMeet.Api.Tests/Endpoints/MeetupHistoryEndpointsTests.cs`: a past meetup `PATCH`ed to `now + 1 day` → 200. It's absent from `/groups/{g}/meetups/past`, present in `/groups/{g}/meetups` with `isPast=false`, and present in `/meetups/upcoming` for a member. Rely on the `PATCH` invalidating the group keys (`meetups:{groupId}`, `meetups:past:{groupId}`), and read `/meetups/upcoming` as a member who hasn't read it yet in this test, because `home-meetups:{userId}` only expires by TTL. **Don't** advance `factory.Clock` to expire caches: `CacheService` uses `IMemoryCache` on the real clock, not `TimeProvider`.
- [X] T038 [P] [US3] Extend `supabase/functions/notifications-dispatch/webhook-router.test.ts`: `meetups` UPDATE with a past `old_record` and a future `record` → `resolveSuppression` returns `null` **and** `resolveNotificationType` returns `"meetup.updated"` (FR-024).
- [X] T039 [P] [US3] Add a `[Fact]` to `tests/LoopMeet.App.Tests/Features/Meetups/EditMeetupViewModelTests.cs` asserting that the future-time error branch is guarded by `!_loadedIsPast`, so a past meetup's date can be moved anywhere, including the future, without a client-side block.

**Checkpoint**: Quickstart row 10 passes on both platforms.

---

## Phase 6: User Story 4: A meetup stays "upcoming" for the rest of its day (Priority: P3)

**Goal**: Meetups remain in the Upcoming lists (Group Detail and Home) until local midnight in their timezone, then move to Past. A started-today meetup can be edited without changing its time.

**Independent Test**: A meetup that started 1 hour ago (in its zone) appears under Upcoming on Group Detail and Home and not under Past. After its local midnight it appears only under Past (quickstart rows 12–14).

**⚠️ Release coupling**: Until this phase lands, a meetup that started earlier today is in **neither** list (the upcoming query still uses `scheduled_at > now`, and US1's past list correctly excludes it). Ship US4 in the same release as US1.

### Tests for User Story 4

- [X] T040 [P] [US4] Extend `tests/LoopMeet.Api.Tests/Endpoints/MeetupHistoryEndpointsTests.cs` with cutoff tests (FR-008, FR-009, FR-029), using a meetup in `America/New_York` scheduled at 19:00 local:
  - With `factory.Clock` at 23:59 local the same day, it's in `/groups/{g}/meetups` and `/meetups/upcoming`, and not in `/past`.
  - With the clock at 00:01 local the next day, it's in `/past` only.
  - Repeat with `timezone = null` at Los Angeles midnight.
  - Use a distinct group and member user for each clock setting, so the 30 s caches (real clock, not `TimeProvider`) can't return a result computed at the other clock setting.
  - `PATCH` of that meetup at 23:00 local with an unchanged `scheduledAt` and a new title → 200 (spec US4 scenario 3).
  - `PATCH` changing its time to 30 minutes before the clock → 400 `invalid_meetup_schedule` (US4 scenario 4).

### Implementation for User Story 4

- [X] T041 [US4] In `src/LoopMeet.Infrastructure/Repositories/MeetupRepository.cs`, change both `ListUpcomingByGroupAsync` and `ListUpcomingByUserAsync` to filter `scheduled_at > (now - MeetupLifecycle.ClassificationBand)` (formatted `"o"`, as today) and then drop rows where `MeetupLifecycle.IsPast(record.ScheduledAt, record.Timezone, now)` after mapping. Keep the ascending order.
- [X] T042 [US4] Apply the same band + `IsPast` filtering to both upcoming methods in `tests/LoopMeet.Api.Tests/Infrastructure/InMemoryMeetupRepository.cs`, then run T040 and the full Api test project to green. Update any existing assertion in `tests/LoopMeet.Api.Tests/Endpoints/MeetupsEndpointsTests.cs` that relied on a meetup vanishing the moment it starts (search for meetups seeded with `DateTimeOffset.UtcNow.AddMinutes(-` / `AddHours(-`). Seed such meetups on a previous local day instead, preserving each test's intent.

**Checkpoint**: Quickstart rows 12–14 pass. The Home tab and Group Detail agree at the cutoff.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T043 From the repo root, run all quality gates (Constitution: tests **and** static analysis/linting) and record the results in Notes against the T001 baseline:
  - `dotnet build LoopMeet.slnx -c Debug -p:SkipMaciOSTargets=true`: no warnings beyond the T001 baseline count
  - `dotnet test LoopMeet.slnx -c Debug -p:SkipMaciOSTargets=true`: green
  - `deno lint supabase/functions/`: clean
  - `deno fmt --check` on **new** files only (`_shared/meetup-lifecycle.ts`, `_shared/meetup-lifecycle.test.ts`, `notifications-dispatch/webhook-router.test.ts`). Existing files such as `webhook-router.ts` and `index.ts` already fail `fmt` on `main`, and reformatting them is out of scope.
  - `deno test supabase/functions/`: green
- [X] T044 [P] Search the changed files for leftover `NotSupportedException` stubs, `TODO`, commented-out code, and stray `DateTimeOffset.UtcNow` in the meetup query/command/repository paths (`grep -rn "UtcNow\|TODO\|NotSupportedException" src/LoopMeet.Api/Services/Meetups src/LoopMeet.Infrastructure/Repositories/MeetupRepository.cs`). Expect no matches. T011 moves every meetup service call, `CreateAsync` included, onto the injected `TimeProvider`.
- [X] T045 [P] Add a "Known gotchas" bullet to `CLAUDE.md`: the meetup past/upcoming rule lives in both `LoopMeet.Core/Meetups/MeetupLifecycle.cs` and `supabase/functions/_shared/meetup-lifecycle.ts`; change both together and extend `meetup-lifecycle.cases.json`, which both test suites read.
- [ ] T046 Deploy to staging: the Api (staging tag workflow), `supabase functions deploy notifications-dispatch`, and an Android build via `deploy/deploy-android.sh -c Debug`. Plus an iOS build. Then run the full [quickstart.md](./quickstart.md) matrix on one iOS and one Android device and record ✅/❌ per row and the row 16 timings in Notes. Any ❌ blocks the PR.
  - **Status (2026-11-02)**: Android ✅ on a physical device (staging Api + functions). iOS not yet run: waiting for a .NET workload that supports Xcode 27.2 (see 013-uiscene-lifecycle). Leave unchecked until the iOS run passes.
- [X] T047 Open the PR from `012-past-meetups` to `main`. The description lists the deploy order (Api → `notifications-dispatch` → app), the Home-tab behavior change for release notes, and the new CI Deno step. Opened as PR #15.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies.
- **Foundational (Phase 2)**: depends on Setup. **Blocks all stories.**
- **US1 (Phase 3)**: depends on Phase 2.
- **US2 (Phase 4)**: depends on Phase 2. Its client edit task (T035) needs `GetMeetupAsync` from T024 (US1). Backend and push tasks (T029–T034) don't depend on US1.
- **US3 (Phase 5)**: depends on T032 and T033 (US2). Test-only phase.
- **US4 (Phase 6)**: depends on Phase 2 only. **Must ship in the same release as US1** (see the coupling note in Phase 6).
- **Polish (Phase 7)**: depends on all stories.

### Story-Level Parallelism

After Phase 2, these tracks can run concurrently:

- Track A: US1 backend (T016, T018–T023)
- Track B: US1 client (T017, T024–T028)
- Track C: US2 push suppression (T030, T031, T033, T034), which is pure TypeScript with no .NET dependency
- Track D: US4 (T040–T042)

US2's backend validation (T029, T032) can run alongside, but both T029 and T032 touch files shared with Track A (`MeetupHistoryEndpointsTests.cs`, `MeetupCommandService.cs`), so sequence them after T016/T023.

### Within-Phase Parallelism

- **Phase 2**: T004/T005 (C#) ∥ T006/T007 (TS), both after T003. T012, T014 and T015 run in parallel once T009–T011 land.
- **US1**: T016 ∥ T017 ∥ T020 ∥ T024. Then T018 → T019 → T021 → T022 → T023 (backend), and T025 → T026 → T027 → T028 (client).
- **US2**: T029 ∥ T030 ∥ T031. Then T032; T033 → T034; T035 → T036.
- **US3**: T037 ∥ T038 ∥ T039.

### Parallel example: start of US1

```text
Task: T016 [US1] Api history endpoint tests        (tests/LoopMeet.Api.Tests/Endpoints/MeetupHistoryEndpointsTests.cs)
Task: T017 [US1] Group detail source-inspection    (tests/LoopMeet.App.Tests/Features/Groups/GroupDetailPastMeetupsSurfaceTests.cs)
Task: T020 [US1] Cursor codec                       (src/LoopMeet.Api/Services/Meetups/MeetupCursorCodec.cs)
Task: T024 [US1] Client API + models                (src/LoopMeet.App/Services/MeetupsApi.cs, Models/MeetupModels.cs)
```

---

## Implementation Strategy

### MVP first

Do Phase 1, Phase 2, US1, then **US4**. US4 is P3, but it's tiny (3 tasks), and shipping US1 without it leaves started-today meetups in neither list. Together they give members the history view and a consistent cutoff. Validate with quickstart rows 1–7 and 12–15.

### Incremental delivery

1. MVP (US1 + US4): members can browse history.
2. US2: owners can correct history silently. This is the main reason the feature exists for owners.
3. US3: tests only, so it can land in the same PR as US2.

### Risk notes

- **Postgrest nested `or`/`and` (T018)**: if `QueryFilter` can't express it, use the documented fallback. The contract and tests don't change.
- **Edge-function deploy is separate from the Api deploy**: until `notifications-dispatch` is redeployed, past-meetup edits made through the new Api still push. Deploy the function right after the Api (T046).
- **The 30 s caches** run on the real clock (`IMemoryCache`), not the injected `TimeProvider`. A test that reads the same key twice across a fake-clock change gets the stale first result. T037 and T040 use a separate group or user for each clock setting.

## Notes

- `[P]` tasks touch different files and don't depend on incomplete tasks.
- Keep the `[x]` checkmarks in this file up to date as work completes (living execution log).
- Commit after each task or logical group. T009–T011 must land together (see the Phase 2 sequencing note).

### Execution log

- **T001 baseline (2026-10-30)**: `dotnet build` 8 warnings, 0 errors. `dotnet test` 159 passing, 0 failing (App 110, Core 5, Infrastructure 1, Api 43).
- **T002 baseline (2026-10-30)**: Deno 2.9.7. `notifications-dispatch` type-checks cleanly. `reminders-scheduler` already fails on `main` with one TS2352 (`as MeetupRow[]` cast at `index.ts:114`). Pre-existing, and this feature doesn't change that file.
- **T043 gates (2026-10-30)**:
  - `dotnet build`: 8 warnings (= baseline), 0 errors
  - `dotnet test`: 217 passing, 0 failing (App 118, Core 29, Infrastructure 1, Api 69)
  - `deno lint supabase/functions/`: clean (16 files)
  - `deno fmt --check` on new files: clean
  - `deno test supabase/functions/`: 34 passing
- **T044 sweep**: no `DateTimeOffset.UtcNow`, `TODO` or `NotSupportedException` left in the meetup services, repository or Core rule; the only matches are the injected `TimeProvider.GetUtcNow()`.
- **Staging outage (2026-10-30)**: after the Api deploy, every meetup list returned nothing. `TimeZoneNotFoundException` for `America/Los_Angeles`: the Alpine runtime image has no `/usr/share/zoneinfo`. Fixed by installing `tzdata` in the runtime stage, plus a startup check in `Program.cs`. Regression tests in `tests/LoopMeet.Api.Tests/Integration/ApiImageTimeZoneDataTests.cs`; research D2 corrected.
- **Toggle follow-up (2026-10-30)**: replaced the two trigger-styled buttons with a wide sliding switch (track + animated thumb showing `SelectedListLabel`). Fixes both options looking selected after switching back, most likely from MAUI not reverting `DataTrigger` setters that hold `AppThemeBinding` values. Regression test: `Toggle_IsASlidingSwitchWithNoTriggerDrivenColors`.
- **Switch reset follow-up (2026-11-02)**: opening a past meetup and going back reset the switch to Upcoming. Shell re-delivers the `groupId` query when a page above Group Detail is popped, so `ApplyQueryAttributes` ran `ResetListMode()` again. It now resets only when `groupId` differs from the view model's (fresh opens get a new transient page and view model). Regression assertion added to `CodeBehind_ResetsModeOnNavigationNotOnAppearing`.
