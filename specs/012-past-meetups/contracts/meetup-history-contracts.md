# Contracts: Meetup History

**Feature**: 012-past-meetups | **Date**: 2026-10-30

These contracts are fixed before implementation (Constitution VI). Every endpoint requires authorization, and the Api impersonates the caller, so RLS restricts every read to members of the group. All changes are **additive**: older app builds keep working against the new Api.

---

## 1. `GET /groups/{groupId}/meetups/past` — NEW

Past meetups for one group, newest first, keyset-paged.

**Query parameters**

| Name | Type | Default | Rules |
| --- | --- | --- | --- |
| `cursor` | string | — | Opaque value echoed from a previous `nextCursor`. Omit it for the first page |
| `limit` | int | 20 | 1–50 |

**200 OK** — `PastMeetupsResponse`

```json
{
  "meetups": [
    {
      "id": "6f0c…",
      "groupId": "1a2b…",
      "title": "Taco night",
      "scheduledAt": "2026-10-28T02:00:00+00:00",
      "placeName": "La Taqueria",
      "placeAddress": "2889 Mission St",
      "latitude": 37.7509,
      "longitude": -122.4181,
      "placeId": "ChIJ…",
      "timezone": "America/Los_Angeles",
      "createdByUserId": "9c8d…",
      "groupName": "Thursday Crew",
      "createdByDisplayName": "Sam",
      "groupOwnerUserId": "9c8d…",
      "isPast": true
    }
  ],
  "nextCursor": "NjM4OTk…"
}
```

- `meetups` holds only meetups where `isPast` is true, ordered `scheduledAt DESC`, ties broken by `id DESC`.
- `nextCursor` is `null` when there are no older meetups. A page can contain **fewer than `limit` items and still have a non-null `nextCursor`**: rows near the cutoff that are still upcoming are skipped, not counted. Clients must page until `nextCursor` is `null`, not until a short page arrives.
- Paging is stable when meetups are inserted, edited or deleted between requests. No meetup is returned twice within one paging sequence.

**400 Bad Request** — `ErrorResponse`

| `code` | When |
| --- | --- |
| `invalid_cursor` | The cursor can't be decoded |
| `invalid_limit` | `limit` is outside 1–50 |

**Caching**: The response is cached for 30 s under `meetups:past:{groupId}` only when `cursor` is absent and `limit` is the default.

**Logging**: `Listing past meetups for group {GroupId} cursor={HasCursor} limit={Limit}` and `Listed past meetups for group {GroupId} count={Count} hasMore={HasMore}`.

---

## 2. `GET /groups/{groupId}/meetups/{meetupId}` — NEW

One meetup, whether upcoming or past.

**200 OK** — `MeetupListItemResponse` (same shape as a list element above, including `isPast`).

**404 Not Found** — The meetup doesn't exist, belongs to another group, or isn't visible to the caller under RLS. No body.

**Caching**: none.

---

## 3. `GET /groups/{groupId}/meetups` — CHANGED (behavior only)

The shape is unchanged apart from the added `isPast` field, which is always `false` here.

**Behavior change**: A meetup stays in this list until the end of its scheduled calendar day in its effective timezone. Previously it disappeared once `scheduledAt` passed. Order is unchanged (`scheduledAt ASC`).

## 4. `GET /meetups/upcoming` (Home) — CHANGED (behavior only)

Same change as §3, applied to every group the caller belongs to.

---

## 5. `PATCH /groups/{groupId}/meetups/{meetupId}` — CHANGED (validation only)

The request and response shapes are unchanged. Validation of `scheduledAt` changes:

| Stored meetup | `scheduledAt` in request | Result |
| --- | --- | --- |
| any | `null` or absent | Time unchanged. Other fields validated as today |
| any | equal to the stored value | Treated as unchanged. **No future-time check** |
| upcoming | different, `> now` | Accepted |
| upcoming | different, `<= now` | `400 invalid_meetup_schedule`, "Scheduled time must be in the future." (unchanged message) |
| past | different, any instant | Accepted. If the new value is in the future, the meetup becomes upcoming again |

On success the Api invalidates `meetups:{groupId}` **and** `meetups:past:{groupId}`. `DELETE` and `POST` on the same resource invalidate both keys as well; otherwise they're unchanged.

---

## 6. Database webhook → `notifications-dispatch` — CHANGED

Webhook payload shape (unchanged): `{ type, table, record, old_record }`.

**New decision step, run before notification-type resolution:**

| `table` | `type` | `old_record` past? | `record` past? | Outcome |
| --- | --- | --- | --- | --- |
| `meetups` | `UPDATE` | yes | yes | **Suppressed.** Response `{ "skipped": true, "reason": "past_meetup" }` |
| `meetups` | `UPDATE` | yes | no | `meetup.updated` (unchanged path) |
| `meetups` | `UPDATE` | no | any | Unchanged (`meetup.updated` / `meetup.canceled`) |
| `meetups` | `DELETE` | any | — | Unchanged: `{ "skipped": true, "reason": "unsupported_event" }` (no push) |
| `meetups` | `INSERT` | — | — | Unchanged: `meetup.created` |
| other | any | — | — | Unchanged |

"Past" is evaluated with `isMeetupPast(scheduled_at, timezone, now)` from `_shared/meetup-lifecycle.ts`, where `now` is the time the function handles the request.

**Suppression log line** (one JSON object per suppressed event, on stdout):

```json
{ "level": "info", "message": "notification_suppressed", "reason": "past_meetup",
  "table": "meetups", "type": "UPDATE", "meetupId": "6f0c…", "groupId": "1a2b…" }
```

### Exported TypeScript surface

```ts
// supabase/functions/_shared/meetup-lifecycle.ts
export const DEFAULT_MEETUP_TIMEZONE = "America/Los_Angeles";
export function isMeetupPast(scheduledAt: string | Date, timezone: string | null | undefined, now: Date): boolean;

// supabase/functions/notifications-dispatch/webhook-router.ts  (resolveNotificationType unchanged)
export type SuppressionReason = "past_meetup";
export function resolveSuppression(payload: WebhookPayload, now: Date): SuppressionReason | null;
```

---

## 7. Core / Infrastructure interfaces — CHANGED

```csharp
// LoopMeet.Core/Meetups/MeetupLifecycle.cs — NEW
public static class MeetupLifecycle
{
    public const string DefaultTimezone = "America/Los_Angeles";
    public static readonly TimeSpan ClassificationBand = TimeSpan.FromHours(26);
    public static bool IsPast(DateTimeOffset scheduledAt, string? timezone, DateTimeOffset now);
}

// LoopMeet.Core/Meetups/MeetupPage.cs — NEW
public readonly record struct MeetupPageCursor(DateTimeOffset ScheduledAt, Guid Id);
public sealed record MeetupPage(IReadOnlyList<Meetup> Items, MeetupPageCursor? NextCursor);

// LoopMeet.Core/Interfaces/IMeetupRepository.cs — CHANGED
Task<IReadOnlyList<Meetup>> ListUpcomingByGroupAsync(Guid groupId, DateTimeOffset now, CancellationToken ct = default);   // + now
Task<IReadOnlyList<Meetup>> ListUpcomingByUserAsync(Guid userId, DateTimeOffset now, CancellationToken ct = default);     // + now
Task<MeetupPage> ListPastByGroupAsync(Guid groupId, DateTimeOffset now, MeetupPageCursor? before, int limit, CancellationToken ct = default); // NEW
```

`now` is passed in rather than read inside the repository. The query service gets it from an injected `TimeProvider`, so tests control the clock, and the in-memory fake applies `MeetupLifecycle` identically.

---

## 8. App client surface — CHANGED

```csharp
// IMeetupsApi (Refit) — additions
[Get("/groups/{groupId}/meetups/past")]
Task<PastMeetupsResponse> GetPastMeetupsAsync(Guid groupId, [Query] string? cursor = null, [Query] int? limit = null);

[Get("/groups/{groupId}/meetups/{meetupId}")]
Task<MeetupSummary> GetMeetupAsync(Guid groupId, Guid meetupId);

// MeetupsApi wrapper — matching pass-through methods.
// MeetupSummary — + bool IsPast.
// PastMeetupsResponse — NEW: List<MeetupSummary> Meetups; string? NextCursor.
```

`MeetupDetailViewModel.LoadAsync` and `EditMeetupViewModel.LoadAsync` call `GetMeetupAsync`. A 404 (`ApiException` with `StatusCode == NotFound`) maps to the existing not-found state in detail. Edit keeps its existing "Could not load meetup details." message.
