# Data Model: Meetup Interaction Improvements

**Feature**: 011-meetup-interactions | **Date**: 2026-07-30

**No database or schema changes.** Every field added below is resolved at read time from existing rows (`meetups`, `groups`, `memberships`, `user_profiles`). Nothing new is persisted, and no retention behavior changes.

## 1. Meetup read model (server)

The shape returned by both list endpoints. Existing fields unchanged; the three additions are resolved per D1/D3/D4.

| Field | Source | Added? | Notes |
| --- | --- | --- | --- |
| `Id`, `GroupId`, `Title`, `ScheduledAt` | `meetups` row | — | |
| `PlaceName`, `PlaceAddress`, `Latitude`, `Longitude`, `PlaceId` | `meetups` row | — | Location is "openable" only when `Latitude` **and** `Longitude` are present. |
| `Timezone` | `meetups` row | — | |
| `CreatedByUserId` | `meetups` row | — | Retained; identity, not display. |
| `GroupName` | `groups.name` for `GroupId` | **new on the group-scoped list** (already present on upcoming) | `string.Empty` if the group row is missing. |
| `CreatedByDisplayName` | `user_profiles.display_name`, gated on current membership of `GroupId` | **new on both lists** | `string.Empty` when the creator is not a current member or has no profile row. Display fallback is applied by the client (D2). |
| `GroupOwnerUserId` | `groups.owner_user_id` for `GroupId` | **new on both lists** | Sole input to the client's edit-affordance gate (FR-016). |

### Resolution rules

Per list request, regardless of how many meetups are returned — three lookups, no per-row query:

1. `IGroupRepository.ListByIdsAsync(distinct groupIds)` → `Name`, `OwnerUserId`.
2. `IMembershipRepository.ListMembersByGroupsAsync(distinct groupIds)` → the set of `(GroupId, UserId)` pairs currently in each group.
3. `IUserRepository.ListByIdsAsync(distinct creatorIds ∩ members of that meetup's group)` → `DisplayName`.

- **INV-1**: `CreatedByDisplayName` is non-empty only when `(meetup.GroupId, meetup.CreatedByUserId)` is a current membership pair — a departed creator resolves to empty, matching the spec's stated privacy basis.
- **INV-2**: A missing group row, missing membership, or missing profile row yields `string.Empty`, never an exception and never an identifier leaked into a display field.
- **INV-3**: Resolution is read-only and cache-shared: the resolved values live inside the cached response body and are therefore identical for every caller of that cache key. No caller-specific value (such as a computed "am I the owner" boolean) may be added to these responses.

## 2. Meetup write echo (server) — unchanged

`MeetupResponse`, returned by `POST`/`PATCH`, keeps exactly its current fields. It gains none of the three, because it echoes what was written rather than a display projection, and both client view models discard it (research F8/D6).

## 3. Client meetup model

`MeetupSummary` (one class, used by both list endpoints and the discarded command echo) gains:

| Member | Kind | Purpose |
| --- | --- | --- |
| `CreatedByDisplayName` | data | Raw resolved name; may be empty. |
| `GroupOwnerUserId` | data | Compared against the current user id to gate the edit affordance. |
| `OrganizerDisplay` | computed | `MeetupOrganizerText.Format(CreatedByDisplayName)` — the FR-011 placeholder when empty. Bound by the UI; the raw field is not. |

Existing computed members (`HasLocation`, `LocationDisplay` → "TBD", `DateTimeDisplay`) are reused as-is by the details screen, so card and details presentation cannot drift apart.

`CanOpenLocation` is introduced as the single expression of "openable" (`Latitude` and `Longitude` both present), replacing the guard currently duplicated inside two `OpenLocationAsync` command bodies. Cards and the details screen both bind their map control's visibility to it (FR-010, FR-020).

## 4. Details screen states

| State | Entered when | Presentation |
| --- | --- | --- |
| `Loading` | Screen appears; request in flight | Progress indicator only |
| `Loaded` | Meetup found in the group's list | All five fields; map control iff `CanOpenLocation`; pencil iff owner |
| `NotFound` | Id absent from the response (deleted, or now in the past — the lists are upcoming-only) | "No longer available" message; **no** pencil, **no** map control |
| `LoadFailed` | Request threw (offline, server error) | Retry-able error message; no partial data shown as if current |

- **INV-4**: The pencil is rendered only in `Loaded` **and** only when `GroupOwnerUserId` equals the current user id — never in `NotFound`/`LoadFailed`, so a stale or missing meetup can never present an edit path (spec edge case).
- **INV-5**: Arriving at the screen always re-reads (no cached view-model state carried across navigations), which is what makes FR-012 hold after returning from an edit.

## 5. Key entity relationships (unchanged)

```text
Group ──owns──> Meetup            (meetups.group_id)
Group ──has──>  Membership ──> User   (memberships.group_id, member_user_id)
Meetup ──created_by──> User       (meetups.created_by_user_id, no FK)
```

This feature adds no relationship. It reads the existing `Meetup → User` link for the organizer name (via `Membership` per INV-1) and the existing `Group → owner` link for the edit gate.
