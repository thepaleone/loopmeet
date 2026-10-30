# Research: Past Meetups on Group Detail

**Feature**: 012-past-meetups | **Date**: 2026-10-30

Each decision below records what was chosen, why, and what was rejected. Code references are to `main` at `db5f35b`.

---

## D1 — Defining "past" as a calendar-date comparison

**Decision**: A meetup is past when the calendar date of *now* in the meetup's effective timezone is later than the calendar date of `scheduled_at` in that same timezone:

```text
isPast(scheduledAt, timezone, now) =
    localDate(now, tz) > localDate(scheduledAt, tz)
where tz = timezone if it is a valid IANA id, else "America/Los_Angeles"
```

This is the spec's "end of the scheduled day (midnight)" rule (FR-007), written so it never has to construct a local midnight.

**Rationale**:

- Computing "next local midnight" and converting it back to UTC breaks in zones whose DST transition happens at midnight (e.g. `America/Santiago`, `Asia/Beirut`), where that local time doesn't exist. Comparing dates has no such gap.
- It is exactly the comparison `reminders-scheduler` already uses (`localScheduled.isoDate !== localNow.isoDate`, `supabase/functions/reminders-scheduler/index.ts:208`). The rule is therefore consistent with the one existing piece of day-aware logic.
- Both languages that need it (C# and TypeScript) can do it with standard-library primitives: `TimeZoneInfo.ConvertTime` + `DateOnly` in .NET, and `Intl.DateTimeFormat` in Deno.

**Alternatives considered**:

- *Compute a `day_ends_at` instant and compare `now >= day_ends_at`*: equivalent where midnight exists, fragile where it doesn't, and more code.
- *Use the viewer's device timezone*: rejected by the spec (all members must see the same lists).

---

## D2 — Timezone fallback and invalid ids

**Decision**: Effective timezone = `meetups.timezone` when it is a non-empty, resolvable IANA id; otherwise `America/Los_Angeles`. An unresolvable id (e.g. a typo stored by an old client) is treated like an empty one rather than raising an error.

**Rationale**: `America/Los_Angeles` is the final fallback `reminders-scheduler` uses (`DEFAULT_TIMEZONE`). The scheduler's middle fallback, the recipient's profile timezone, can't be used: classification has to be the same for every viewer (spec Assumptions). On Linux, .NET resolves IANA ids from the OS zone database at `/usr/share/zoneinfo` (not from ICU), so the host must have it. **Correction (2026-10-30 staging outage):** the Api runs on `aspnet:10.0-alpine`, which ships without zone data; the runtime stage must `apk add --no-cache tzdata`, and `Program.cs` resolves the default zone at startup so a missing database fails the deploy instead of every meetup request. `Intl` throws `RangeError` for unknown zones, which is caught and mapped to the fallback.

**Alternatives considered**: Fall back to UTC. This is simpler, but it would disagree with the reminder scheduler for every meetup that has no timezone.

---

## D3 — One rule, two implementations, one shared fixture

**Decision**: Implement the rule twice, because it runs in two runtimes that can't share code:

| Runtime | Location | Consumers |
| --- | --- | --- |
| .NET | `src/LoopMeet.Core/Meetups/MeetupLifecycle.cs` (static, pure) | Infrastructure repository filters, Api update validation, Api read model (`IsPast`), in-memory test fake |
| Deno | `supabase/functions/_shared/meetup-lifecycle.ts` (pure) | `notifications-dispatch` push suppression |

Both are tested against **one** JSON case file, `supabase/functions/_shared/meetup-lifecycle.cases.json`. It holds each input triple `(scheduledAt, timezone, now)` and the expected `isPast`. The xUnit test reads it with the repo's existing relative-path pattern (`Path.Combine(AppContext.BaseDirectory, "../../../../../…")`), and the Deno test imports it directly.

**Rationale**: FR-008 requires a meetup never to sit in both lists or neither, *and* the push decision must agree with the lists. Two implementations are unavoidable. A shared fixture is the cheapest way to make them provably agree, and it is the only new abstraction (Principle IV: ≥2 real consumers; here, 2 runtimes and 5 call sites).

**Cases the fixture must cover** (minimum): just before / at / just after local midnight in a positive-offset zone, a negative-offset zone, and a half-hour zone (`Asia/Kolkata`); a meetup earlier the same local day (upcoming); one 1 minute after local midnight (past); a DST-transition day (`America/Los_Angeles` in March and November); a midnight-DST zone (`America/Santiago`); null timezone; an invalid timezone string; and a future meetup.

**Alternatives considered**:

- *Postgres function as the single source of truth*, called from both the Api and the edge function. Rejected: it's a schema change the spec asks to avoid, and it would add a round trip to every webhook.
- *Source-inspection test only for the TS side*: too weak for date arithmetic.

---

## D4 — Filtering lists without a schema change: bounded UTC window + in-memory classification

**Decision**: PostgREST can't apply a per-row timezone in a filter. The repository therefore narrows candidates with a UTC bound that is always safe, and then applies `MeetupLifecycle.IsPast` in memory:

- A future meetup (`scheduled_at > now`) is never past.
- A meetup with `scheduled_at <= now − 26h` is always past. A local calendar day lasts at most 25 h (on a DST fall-back day), so its date has necessarily ended. The extra hour is margin.
- Only rows in the **band** `(now − 26h, now]` need per-row classification.

| Query | Database filter | In-memory step |
| --- | --- | --- |
| Upcoming by group / by user | `scheduled_at > now − 26h`, ascending | drop rows where `IsPast` |
| Past by group (paged) | `scheduled_at <= now` + keyset cursor, descending | drop rows where `!IsPast` |

**Rationale**: Same strategy, and essentially the same window, as `reminders-scheduler.fetchTodayCandidateMeetups` (±25 h), so it's an established pattern here. The band contains at most one day of a group's meetups, so the extra rows are negligible. The existing indexes `idx_meetups_group_scheduled (group_id, scheduled_at)` and `idx_meetups_scheduled_at` serve both directions; **no index, function, or column is added.**

**Alternatives considered**:

- *Stored generated column `day_ends_at`* (Postgres `AT TIME ZONE` is immutable): exact filtering in SQL, but it's a migration plus a backfill, and the cases needing it are rare.
- *SQL view or RPC*: schema change, and the Postgres rule would then also have to stay in step with the C# and TypeScript ones.

---

## D5 — Keyset paging for the past list

**Decision**: Use cursor-based (keyset) paging, ordered by `scheduled_at DESC, id DESC`. Page size defaults to 20, with a maximum of 50. The cursor is an opaque, URL-safe string encoding `(scheduled_at, id)` of the last row the server **examined**, not the last row it returned. The next page is fetched with `scheduled_at < c.ts OR (scheduled_at = c.ts AND id < c.id)`, expressed as a PostgREST `or=(…)` filter through `Supabase.Postgrest` `QueryFilter` with `Operator.Or` / `Operator.And`.

When in-memory classification (D4) drops band rows, the repository keeps reading raw batches until it has `limit` past rows or the table runs out. `nextCursor` is `null` exactly when the last raw batch came back short.

**Rationale**:

- **No duplicates or skips when data changes between pages (FR-014).** With offset paging, deleting a meetup the user already saw shifts every later offset by one, so a meetup would be silently skipped. A keyset cursor isn't affected by inserts or deletes above it.
- The `id` tie-breaker makes the order total. Two meetups in one group at the identical instant are rare but legal.

**Fallback (in case the Postgrest C# client can't express the nested `and` inside `or`)**: query `scheduled_at <= c.ts`, then discard in memory any row with `scheduled_at == c.ts && id >= c.id`. Same results, at most a handful of extra rows. The implementer picks whichever works. The contract doesn't change either way.

**Alternatives considered**:

- *Offset/limit*: simpler, but violates FR-014 as described above.
- *Load all history at once*: rejected by the user during spec shaping.

---

## D6 — Where push suppression happens

**Decision**: In the `notifications-dispatch` edge function. A new exported function in `webhook-router.ts`:

```ts
resolveSuppression(payload: WebhookPayload, now: Date): "past_meetup" | null
```

It returns `"past_meetup"` when `table === "meetups"` and `type === "UPDATE"`, and both `old_record` and `record` are past per D1 at the moment of processing. `index.ts` calls it **before** `resolveNotificationType`. On suppression it emits one structured log line, `{ level: "info", message: "notification_suppressed", reason, table, type, meetupId, groupId }` (FR-028), and returns `{ skipped: true, reason: "past_meetup" }`. This is the same response shape as the existing `unsupported_event` / `no_recipients` skips.

**Rationale**:

- Pushes are produced by a Supabase **database webhook** on `meetups`, not by the Api. Suppressing in the Api is impossible: the Api has no say in whether the webhook fires. Suppressing in SQL would mean a trigger rewrite (schema change).
- Comparing old against new covers every row in the spec's push table with one predicate:

  - past→past edit: suppressed;
  - past→future: `record` is upcoming, so it falls through to `meetup.updated` (FR-024);
  - upcoming→anything: `old_record` is upcoming, so behavior is unchanged (FR-026).

- `resolveNotificationType` keeps its signature, so `WebhookNotificationMappingTests`, `NotificationMappingParityTests` and the payload contract tests are unaffected.
- "Judged at the moment the change is processed" (spec Edge Cases) falls out naturally from using the function's own `now`.

**Alternatives considered**: Add a "suppress" flag column the Api sets before updating. Rejected: schema change, and it's a stateful handshake between the Api and the webhook that can drift.

---

## D7 — Deletes and cancels of past meetups (FR-022, FR-025)

**Findings**:

- **Delete** already sends no push. `resolveNotificationType` returns `null` for every `DELETE` (`webhook-router.ts`), so `index.ts` answers `unsupported_event`. FR-025 for delete holds today. A regression test pins it.
- **Cancel doesn't exist in the data model.** `meetups` has no `status` column (`supabase/migrations/20260330120000_add_meetups.sql`), and neither `Meetup` nor any Api contract carries one. The router's `meetup.canceled` branch can never match in practice. The app only offers delete.

**Decision**: No cancel work in this feature. FR-022/FR-025's "cancel" clause is met by construction. Should a status column ever be added, D6's suppression check already runs before the status branch, so canceling a past meetup would be suppressed automatically. A Deno test with a `status: "canceled"` past→past payload pins that ordering.

---

## D8 — Same-day reminders never fire for a finished day (FR-027)

**Finding**: Already true. `reminders-scheduler` only queues a reminder when `localScheduled.isoDate === localNow.isoDate`, and its candidate window is ±25 h. A meetup moved past→future→past is re-evaluated on every 5-minute run against its *current* `scheduled_at`, and per-day event ids (`meetup:today_reminder:{variant}:{id}:{localDate}`) stop repeats.

**Nuance (accepted)**: When a meetup has no timezone, the scheduler uses the *recipient's* profile timezone, while the lists use `America/Los_Angeles`. Reminders are per-recipient ("your today"), so this is correct for them and doesn't affect list or push consistency.

**Decision**: No logic change. Add a source-inspection regression test asserting that the same-local-day guard is still present. The scheduler is a single `Deno.serve` closure, and pulling the guard out just to unit-test it would be a refactor outside this feature's scope.

---

## D9 — Edit validation: "only when the time changes"

**Decision**: The Api's `UpdateAsync` becomes:

```text
now     = timeProvider.GetUtcNow()
wasPast = MeetupLifecycle.IsPast(existing.ScheduledAt, existing.Timezone, now)
if request.ScheduledAt is set AND differs from existing.ScheduledAt:
    if !wasPast AND request.ScheduledAt <= now  → InvalidSchedule   (FR-020)
    existing.ScheduledAt = request.ScheduledAt                       (FR-018, FR-019)
```

On the client, `EditMeetupViewModel` mirrors it (FR-021):

- It remembers the loaded `ScheduledAt` and `IsPast`.
- It treats the time as changed only when the picked local date + time differs from the loaded value at **minute precision**, because the pickers have no seconds.
- It applies the "must be in the future" check only when the time changed and the meetup is not past.
- It sends `ScheduledAt` (and `Timezone`) **only when the time changed**, and otherwise sends `null`. `null` already means "leave unchanged" in `UpdateMeetupRequest`.

**Rationale**:

- Comparing against the stored value on the server means an older client, which always resends `ScheduledAt`, also benefits. An unchanged value no longer trips the future-time check, so the "started earlier today" case (User Story 4, scenario 3) works even before the app update ships.
- Sending `Timezone` only with a time change stops a location-only edit from quietly rewriting the meetup's timezone to the editor's device zone. Such a rewrite could move a meetup between lists, or change the push decision, when the time itself didn't change.
- `TimeProvider` is part of the .NET base library (no new package). Injecting it makes the cutoff tests deterministic (Principle II). The tests use a hand-rolled `FixedTimeProvider : TimeProvider`, per the repo's no-mocking-framework convention.

**Alternatives considered**: A separate "edit past meetup" endpoint. Rejected: duplicates the update path for a rule that is a single condition.

---

## D10 — Meetup detail and edit load a single meetup by id

**Decision**: Add `GET /groups/{groupId}/meetups/{meetupId}`. It returns `MeetupListItemResponse`, with the same projection as the lists, now including `IsPast`. It returns 404 when the row is missing or belongs to another group. `MeetupDetailViewModel` and `EditMeetupViewModel` switch from "fetch the group's upcoming list and find the id" to this call. Results are not cached.

**Rationale**:

- The current approach can't find past meetups (FR-015), and it would need to page through history to do so.
- RLS already limits the row to group members, because the Api impersonates the caller's token.
- Not caching avoids a fourth cache key to invalidate. A single-row read by primary key is cheap, and detail must show fresh values after an edit (011 FR-012).

**Alternatives considered**: Pass the `MeetupSummary` through navigation parameters. Rejected for the same reason as in 011: detail must load from `(groupId, meetupId)` alone (deep links, Home entry point).

---

## D11 — Exposing `IsPast` to the client

**Decision**: Add `bool IsPast` to `MeetupListItemResponse` and the client's `MeetupSummary`, computed in `MeetupQueryService.ProjectAsync` from the same `now` used for the query.

**Rationale**: `LoopMeet.App` doesn't reference `LoopMeet.Core` and can't reach `MeetupLifecycle`. Making the server the single classifier avoids a third copy of the rule in the client. The client needs the value to decide whether the future-time check applies (D9). The flag can be up to 30 s stale on cached list responses. The single-meetup endpoint used by edit isn't cached, so the value that matters for validation is always fresh.

---

## D12 — Group Detail UI mechanics

**Decisions**:

| Concern | Decision | Why |
| --- | --- | --- |
| Toggle | Two `Button`s side by side in a rounded `Border`, styled as a segmented control. A `MeetupListMode` property (`Upcoming`/`Past`) drives the selected look through `DataTrigger`s, and `SelectMeetupListCommand` takes the mode as a parameter. | MAUI has no built-in segmented control, and no new package is wanted. The existing `RadioButton` style in `Styles.xaml` doesn't look like tabs. Style and color keys must be checked against `Colors.xaml`/`Styles.xaml` before use (a missing key fails silently). |
| Default to Upcoming on open (FR-002) | Reset the mode in `ApplyQueryAttributes` (which runs on navigation **to** the page), not in `OnAppearing` (which also runs when coming back from detail or edit). | Returning from editing a past meetup should land back on the past list, refreshed. Only a fresh open resets. |
| Two lists | Two `ObservableCollection<MeetupSummary>` properties, `Meetups` (upcoming, existing name kept so 011's source-inspection tests and bindings stay valid) and `PastMeetups`, each with its own `BindableLayout` and visibility. The card `DataTemplate` moves to `ContentPage.Resources` and both lists reference it. | No duplicated card markup (Principle I). `BindableLayout` respects the "no `CollectionView` inside `ScrollView`" iOS crash rule. |
| Switching doesn't reload the page (FR-005) | Selecting Past loads the past list's first page only if it hasn't been loaded during this visit. Group info and members are untouched. | |
| Pull-to-refresh (FR-006) | Wrap the existing `ScrollView` in a `RefreshView` bound to `RefreshMeetupsCommand`, which reloads only the selected list (the past list restarts from page 1). | The page has no pull-to-refresh today, so this adds one. |
| Load more (FR-011) | The `ScrollView.Scrolled` handler in code-behind runs `LoadMorePastMeetupsCommand` when the past list is selected and the scroll position is within ~300 px of the bottom. The command is guarded against re-entry and does nothing once `nextCursor` is `null`. If loading a page fails, the loaded items stay and a "Couldn't load more — tap to retry" row is shown. | `BindableLayout` has no `RemainingItemsThreshold`. A scroll-position trigger is the smallest workable option. |
| Collection updates | All collection changes run on the main thread. | Recorded memory: updates from background threads are silently dropped by bindings. |
| Delete from the past list | `DeleteMeetupAsync` removes the item from whichever collection contains it. | |

---

## D13 — Running Deno tests in CI

**Decision**: Add `deno test` tests next to the code they cover:

- `supabase/functions/_shared/meetup-lifecycle.test.ts`
- `supabase/functions/notifications-dispatch/webhook-router.test.ts`

Add a `denoland/setup-deno` step and a `deno test supabase/functions` step to `.github/workflows/tests.yml`. Deno 2.x is already the local runtime for these functions.

**Rationale**: Constitution Principle II says tests must run in CI and merges are blocked on failure. This is the first piece of branching TypeScript logic with real edge cases (timezones), and source-inspection can't verify it. The step adds about 10 s to CI.

**Alternatives considered**: Source-inspection tests from xUnit only. That's the existing pattern, but it was adequate only because the TS routing so far was trivial string mapping.
