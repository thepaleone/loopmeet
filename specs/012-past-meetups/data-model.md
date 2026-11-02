# Data Model: Past Meetups on Group Detail

**Feature**: 012-past-meetups | **Date**: 2026-10-30

**No database schema changes.** The `meetups` table, its indexes and its RLS policies are unchanged. Everything new below is derived at read time or exists only in memory or on the wire.

## Stored entity (unchanged)

### `meetups` (Supabase table) / `Meetup` (Core model)

| Field | Type | Role in this feature |
| --- | --- | --- |
| `id` | uuid | Keyset tie-breaker for past-list paging |
| `group_id` | uuid | List scope |
| `scheduled_at` | timestamptz | Input to classification, list order, keyset primary key |
| `timezone` | text, nullable | Input to classification (IANA id). Fallback per research D2 |
| `title`, `place_*`, `latitude`, `longitude`, `created_by_user_id`, `created_at`, `updated_at` | — | Displayed or edited as today |

Existing indexes `idx_meetups_group_scheduled (group_id, scheduled_at)` and `idx_meetups_scheduled_at (scheduled_at)` serve the ascending upcoming query, the descending past query and the keyset filter. None are added.

## Derived concepts

### Meetup lifecycle (derived, never stored)

```text
effectiveTimezone(m) = m.timezone if a valid IANA id, else "America/Los_Angeles"
isPast(m, now)       = localDate(now, tz) > localDate(m.scheduled_at, tz)
isUpcoming(m, now)   = !isPast(m, now)
```

```text
            local midnight after scheduled day passes
  UPCOMING ───────────────────────────────────────────▶ PAST
     ▲                                                   │
     └──────── owner edits scheduled_at into future ─────┘
               (FR-019; sends normal "meetup updated" push)
```

There is no other transition. A meetup can't be created in the past (create validation is unchanged), and editing a past meetup to another past time keeps it past.

**Implementations**: `LoopMeet.Core.Meetups.MeetupLifecycle` (C#) and `supabase/functions/_shared/meetup-lifecycle.ts` (TS). Both are verified against `supabase/functions/_shared/meetup-lifecycle.cases.json` (research D3).

### Candidate window (repository-internal)

| Constant | Value | Meaning |
| --- | --- | --- |
| `ClassificationBand` | 26 h | Rows with `scheduled_at` in `(now − 26h, now]` need per-row classification. Older rows are always past, future rows are always upcoming. |

### Past-meetup page (new, in memory and on the wire)

| Field | Type | Notes |
| --- | --- | --- |
| `Items` | list of `Meetup` | Only past meetups, ordered `scheduled_at DESC, id DESC`. At most `limit` |
| `NextCursor` | `MeetupPageCursor?` | `null` when no older meetups remain |

### `MeetupPageCursor` (new value type, Core)

| Field | Type | Notes |
| --- | --- | --- |
| `ScheduledAt` | DateTimeOffset | `scheduled_at` of the last row **examined** (not necessarily the last returned) |
| `Id` | Guid | `id` of that row |

Wire form: an opaque base64url string, `"{scheduledAt UTC ticks}:{id:N}"`. Encoding and decoding live in the Api layer. A string that can't be decoded becomes a `400 invalid_cursor`.

## Read model changes

### `MeetupListItemResponse` (Api) / `MeetupSummary` (App)

| Field | Change | Notes |
| --- | --- | --- |
| `IsPast` | **ADDED**, bool | Computed in `MeetupQueryService.ProjectAsync` with the same `now` as the query. Up to 30 s stale on cached list responses. Always fresh on the single-meetup endpoint |

All existing fields are unchanged.

### `PastMeetupsResponse` (new, Api + App mirror)

| Field | Type |
| --- | --- |
| `Meetups` | `IReadOnlyList<MeetupListItemResponse>` |
| `NextCursor` | `string?` |

## Validation rules

| Rule | Where | Source |
| --- | --- | --- |
| A past meetup may be saved with any `ScheduledAt` | Api `MeetupCommandService.UpdateAsync`, App `EditMeetupViewModel` | FR-018, FR-019 |
| An upcoming meetup's `ScheduledAt`, **if changed**, must be `> now` | Same two places | FR-020 |
| `ScheduledAt` equal to the stored value counts as "unchanged" (server: exact; client: minute precision) | Same two places | Research D9 |
| The client sends `ScheduledAt` and `Timezone` only when the time changed | `EditMeetupViewModel` | Research D9 |
| Past-list `limit`: 1–50, default 20 | Api endpoint | Research D5 |
| Title and location rules | Unchanged | — |

## Cache keys (Api, 30 s TTL)

| Key | Contents | Invalidated on |
| --- | --- | --- |
| `meetups:{groupId}` | Upcoming list (existing) | create / update / delete (existing) |
| `meetups:past:{groupId}` | **NEW**: first page of the past list only (requests carrying a cursor bypass the cache) | create / update / delete, in the same places `meetups:{groupId}` is invalidated |
| `home-meetups:{userId}` | Home upcoming list (existing) | Unchanged (expires by TTL) |

The single-meetup read (`GET /groups/{groupId}/meetups/{meetupId}`) isn't cached.

## Privacy and retention

No new user data is collected or stored. Past meetups were already retained indefinitely in `meetups`. This feature only makes them visible to the same group members who could already read them under RLS (`meetups_select_group_member`). The organizer name follows 011's disclosure rule: it's shown only while the creator is still a group member. Suppression log lines carry meetup and group ids only, no personal data.
