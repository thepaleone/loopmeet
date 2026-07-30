# Research: Meetup Interaction Improvements

**Feature**: 011-meetup-interactions | **Date**: 2026-07-30
**Sources**: codebase archaeology (file:line references below); requested approach recorded in the `/speckit.plan` input (extend backend read contracts, no extra client calls, no navigation-parameter threading).

## 1. Starting facts

| # | Fact | Evidence |
| --- | --- | --- |
| F1 | Meetups store only `created_by_user_id`; no display name anywhere, no FK to profiles. | `supabase/migrations/20260330120000_add_meetups.sql:4`; `src/LoopMeet.Core/Models/Meetup.cs:7` |
| F2 | `GroupName` is populated on the upcoming list only, not the group-scoped list. | `MeetupQueryService.cs:32-45` vs `:61-75` |
| F3 | Nothing in meetup data identifies the group's **owner**, so ownership is unknowable client-side from a meetup alone. | `Contracts/MeetupContracts.cs` (no owner field); `GroupDetailViewModel.cs:159-161` learns it from `GET /groups/{id}` |
| F4 | The established server-side name-resolution flow is membership → `IUserRepository.ListByIdsAsync` → `DisplayName`, with `?? string.Empty` for unresolved ids. | `GroupQueryService.cs:90-108`, `:138-153`; `InvitationQueryService.cs:45-84` |
| F5 | There is **no GET-meetup-by-id endpoint**; `EditMeetupViewModel` already loads a single meetup by fetching the group list and filtering client-side. | `MeetupsEndpoints.cs`; `EditMeetupViewModel.cs:198-200` |
| F6 | `meetups:{groupId}` is invalidated on create/update/delete; `home-meetups:{userId}` is **never** invalidated (30 s TTL only). | `MeetupCommandService.cs:81`, `:140`, `:160` |
| F7 | Both list endpoints return only future meetups (`scheduled_at > now`). | `MeetupRepository.cs:29-42`, `:63-73` |
| F8 | `MeetupResponse` doubles as the create/update echo, and both client view models discard that echo. | `MeetupCommandService.cs:84`, `:143`; `CreateMeetupViewModel.cs:210`, `EditMeetupViewModel.cs:247` |
| F9 | Meetup endpoint tests never seed `User` rows, so every test meetup's creator id points at a nonexistent user. | `MeetupsEndpointsTests.cs:238-266` |
| F10 | `meetup.created` / `meetup.updated` push notifications route to `//groups/group-detail?groupId={target_id}` — `target_id` is the **group** id, and no meetup-details route exists. | `NotificationRouteMap.cs`; `supabase/functions/_shared/notification-mapping-registry.ts:17-31` |

## 2. Decisions

### D1 — Resolve organizer display name in `MeetupQueryService`, scoped to current group membership

**Decision**: In both list paths, collect the distinct `CreatedByUserId` values, resolve them through the group's current membership roster, then through `IUserRepository.ListByIdsAsync`. A creator who is **not currently a member** of the meetup's group resolves to no name (see D2 for what is displayed).
**Rationale**: Matches F4's existing pattern. Membership-scoping is not incidental — the spec's own privacy rationale for showing the organizer is that "group members already see [the display name] in the group's member list". A departed member is no longer in that list, so surfacing their name would exceed the stated basis for the disclosure. Membership-scoping keeps the disclosure exactly aligned with it.
**Alternatives considered**: Resolve straight from `IUserRepository` ignoring membership — one fewer lookup and preserves historical accuracy for departed organizers, but discloses a name that the group roster no longer shows. Rejected on the privacy alignment above; noted here because it is a one-line change if the product view differs.

### D2 — The fallback *string* lives in the client, not the API

**Decision**: When the organizer cannot be resolved, the API returns an empty `CreatedByDisplayName` (exactly F4's `?? string.Empty` convention). The client renders the FR-011 neutral placeholder — "A group member" — from a pure helper, `MeetupOrganizerText.Format`.
**Rationale**: This is a deliberate, flagged deviation from a literal reading of the requested approach, which put the fallback in `MeetupQueryService`. Server-side resolution (the substantive part of the request) is unchanged; only the user-facing literal moves. Reasons: every other display fallback in the API is `string.Empty`, so a lone DTO carrying prose would be inconsistent; user-facing copy in a response body cannot be localized by the client; and as a pure client helper the fallback becomes genuinely unit-testable (Constitution II) instead of needing an endpoint test to assert copy.
**Alternatives considered**: Return "A group member" from the API — one less client concept and satisfies the instruction literally. Rejected for the three reasons above, but it is a two-line change in one method if the API is preferred as the source of that copy.

### D3 — Add `GroupOwnerUserId` to the read contract (beyond the two named gaps)

**Decision**: Include `GroupOwnerUserId` on the meetup read model.
**Rationale**: FR-015/FR-016 gate the edit affordance on group ownership *regardless of entry point*. From Group Detail the client happens to know the owner already; from Home it does not (F3), and would need one `GET /groups/{id}` per distinct group — the exact extra-client-call pattern the requested approach excludes. The field is free: the group rows are already fetched to supply `GroupName` (D4), and `Group.OwnerUserId` sits on the same row. Without it, FR-015 is not implementable from the Home entry point within the stated constraints.
**Alternatives considered**: (a) A server-computed boolean `IsCurrentUserGroupOwner` — slightly smaller client logic, but bakes the caller's identity into a cached response, and the caches are keyed by group/user in ways that would make that unsafe to share. Rejected. (b) One `GET /groups/{id}` from the details screen — rejected per the stated constraint.

### D4 — Populate `GroupName` on the group-scoped list from the group row the service now fetches

**Decision**: `MeetupQueryService` injects `IGroupRepository` and resolves `Name` + `OwnerUserId` for the distinct group ids in one `ListByIdsAsync` call, on both list paths.
**Rationale**: The group-scoped path knows its single group id, so this is trivial (F2). Doing it in the service — rather than extending the repository tuple — means one group fetch serves both `GroupName` and `GroupOwnerUserId`.
**Alternatives considered**: Extend `ListUpcomingByUserAsync`'s tuple with the owner id. Rejected: the service would then still need its own group fetch for the group-scoped path, duplicating work; see D5.

### D5 — Simplify `ListUpcomingByUserAsync` to return meetups only

**Decision**: Change `IMeetupRepository.ListUpcomingByUserAsync` from `IReadOnlyList<(Meetup, string GroupName)>` to `IReadOnlyList<Meetup>`, deleting the repository's fetch-all-groups-and-filter block (`MeetupRepository.cs:75-85`) now that the query service fetches group rows itself (D4).
**Rationale**: Keeping the tuple would mean two group lookups per request for strictly less data than one. Removing it leaves the repository doing data access and the query service doing denormalization (Constitution V), and deletes code rather than adding it (Constitution I).
**Alternatives considered**: Leave the signature alone and ignore the tuple's `GroupName`. Rejected — an unused return value is exactly the dead weight Principle I prohibits. Cost of the refactor: the in-memory fake and one existing assertion (`UpcomingReturnsMeetupsAcrossAllGroupsWithGroupName`) are touched; the assertion's property (`GroupName`) survives on the new contract, so the test's intent is preserved.

### D6 — One read contract for both list endpoints

**Decision**: Introduce `MeetupListItemResponse` (all existing meetup fields + `GroupName`, `CreatedByDisplayName`, `GroupOwnerUserId`) and use it for both `MeetupsResponse` and `UpcomingMeetupsResponse`. Delete `UpcomingMeetupResponse`. Leave `MeetupResponse` — the create/update echo — untouched.
**Rationale**: The two list DTOs were already ~90% duplicates; converging them means no net type growth and one shape for the client's single `MeetupSummary` model. Leaving the command echo alone avoids either (a) fields that are always empty on write responses (a contract that lies) or (b) injecting user/group/membership repositories into `MeetupCommandService` purely to populate an echo that both view models discard (F8).
**Alternatives considered**: Add the three fields to `MeetupResponse` and populate them in the command service too — the most uniform contract, at the cost of three new dependencies on the write path for unused data. Rejected as unjustified work; revisit if a client ever consumes the echo.

### D7 — Details screen loads from the group-scoped list using `(groupId, meetupId)`

**Decision**: New `meetup-detail` route taking `groupId` and `meetupId`; the view model calls `GET /groups/{groupId}/meetups` and selects by id — the pattern `EditMeetupViewModel` already uses (F5). No meetup object is threaded through navigation.
**Rationale**: Self-sufficient from two ids, so it works identically from Home, from Group Detail, and from a future deep link. It also lands on the one cache that *is* invalidated on meetup writes (F6), which is what makes FR-012 true after an owner returns from editing. A single call yields all five display fields plus ownership.
**Alternatives considered**: (a) Pass the `MeetupSummary` through `GoToAsync` — fastest render, no call, but breaks deep links and shows pre-edit values after an edit. Rejected per the stated constraint. (b) Add `GET /groups/{groupId}/meetups/{meetupId}` — cleaner and would fix D8's past-meetup limitation, but is beyond the requested change; deferred, not rejected (see D8).

### D8 — Not-found is a first-class state, because the list is upcoming-only

**Decision**: When the id is absent from the response, the details screen shows a plain "this meetup is no longer available" state with no edit affordance and no map control.
**Rationale**: Both list endpoints filter to future meetups (F7), so a meetup that has just passed, or one deleted by another member while the screen was open, legitimately returns nothing. The spec's edge cases require that this not present a broken edit path or a blank screen. This is also the reachability limit worth stating plainly: **the details screen can only display upcoming meetups**, because no entry point exposes past ones today; a future deep link to a past meetup would need the by-id endpoint from D7(b).
**Alternatives considered**: Silently navigating back — rejected as an unexplained dead end.

### D9 — Caching left exactly as it is

**Decision**: Keys, TTLs, and invalidation points unchanged. Cached payloads simply carry three more fields.
**Rationale**: `meetups:{groupId}` invalidation already covers the details screen's freshness requirement (D7). The `home-meetups:{userId}` key's lack of invalidation (F6) is pre-existing behavior, bounded by a 30 s TTL, and affects only how soon the Home *list* reflects an edit — not the details screen. Fixing it would need a key-prefix or per-member fan-out API on `ICacheService` that does not exist, so it stays out of scope and is recorded here as a known limitation. Note for deployment: with Redis configured, `CacheService` JSON-round-trips DTOs, so entries written before the change deserialize with the new fields defaulted — harmless within one TTL.
**Alternatives considered**: Add prefix invalidation to `ICacheService`. Rejected as unrelated scope.

## 3. Test surface (Constitution II)

- **Api endpoint tests** (`WebApplicationFactory` + in-memory fakes): `CreatedByDisplayName` populated for a creator who is a current member; empty for a creator who has left the group (D1); `GroupName` present on the group-scoped list (previously absent); `GroupOwnerUserId` correct on both lists. Requires seeding `User` rows, which meetup tests currently never do (F9) — the seeding pattern to copy is `GroupsEndpointsTests.cs:66-83`.
- **Pure client unit tests**: `MeetupOrganizerText.Format` — resolved name passes through, null/empty/whitespace yields the FR-011 placeholder (D2).
- **Source-inspection tests** (repo pattern): save icon present on the title row and the bottom save button gone from both forms; card templates bind the card tap to the details command and the map glyph to the maps command; the details page gates the pencil on ownership; `meetup-detail` route registered.
- **Quickstart device matrix**: keyboard-overlap behavior, native map launch, per-entry-point ownership gating, not-found state — the platform-only behavior that cannot be asserted off-device.
