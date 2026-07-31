# Contract: Meetup Read Contracts & UI Surfaces (011-meetup-interactions)

**Date**: 2026-07-30. Interfaces are fixed here before implementation per Constitution VI. Server types live in `LoopMeet.Api.Contracts`; client types in `LoopMeet.App.Features.Meetups`.

## 1. `MeetupListItemResponse` (new — replaces `UpcomingMeetupResponse`)

```csharp
namespace LoopMeet.Api.Contracts;

public sealed class MeetupListItemResponse
{
    public Guid Id { get; init; }
    public Guid GroupId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTimeOffset ScheduledAt { get; init; }
    public string? PlaceName { get; init; }
    public string? PlaceAddress { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? PlaceId { get; init; }
    public string? Timezone { get; init; }
    public Guid CreatedByUserId { get; init; }

    // Added by this feature — resolved at read time, never stored.
    public string GroupName { get; init; } = string.Empty;
    public string CreatedByDisplayName { get; init; } = string.Empty;
    public Guid GroupOwnerUserId { get; init; }
}

public sealed class MeetupsResponse
{
    public IReadOnlyList<MeetupListItemResponse> Meetups { get; init; } = Array.Empty<MeetupListItemResponse>();
}

public sealed class UpcomingMeetupsResponse
{
    public IReadOnlyList<MeetupListItemResponse> Meetups { get; init; } = Array.Empty<MeetupListItemResponse>();
}
```

- `UpcomingMeetupResponse` is **deleted**; its only property beyond `MeetupResponse` (`GroupName`) survives here, so existing assertions on `GroupName` keep their meaning.
- `MeetupResponse` and both request contracts are **unchanged**. `MeetupResponse` remains the `POST`/`PATCH` echo and MUST NOT gain these three fields (research D6).
- All members stay `{ get; init; }`: cached responses are JSON-round-tripped when Redis is configured, and `System.Text.Json` requires deserializable members.

**Endpoints affected** (routes, auth, and status codes all unchanged):

| Endpoint | Before | After |
| --- | --- | --- |
| `GET /groups/{groupId}/meetups` | `MeetupsResponse` of `MeetupResponse` (no group name) | `MeetupsResponse` of `MeetupListItemResponse` |
| `GET /meetups/upcoming` | `UpcomingMeetupsResponse` of `UpcomingMeetupResponse` | `UpcomingMeetupsResponse` of `MeetupListItemResponse` |
| `POST`/`PATCH`/`DELETE` | unchanged | unchanged |

## 2. `MeetupQueryService` resolution contract

```csharp
public MeetupQueryService(
    IMeetupRepository meetupRepository,
    IGroupRepository groupRepository,          // new
    IMembershipRepository membershipRepository, // new
    IUserRepository userRepository,             // new
    ICacheService cacheService,
    ILogger<MeetupQueryService> logger)
```

Both public methods keep their signatures, cache keys (`meetups:{groupId}`, `home-meetups:{userId}`), and 30-second TTL. Inside the cache factory, each resolves display data through one shared private helper:

```text
ResolveAsync(meetups, cancellationToken):
    groupIds   = meetups.Select(GroupId).Distinct()
    groups     = IGroupRepository.ListByIdsAsync(groupIds)          -> id -> (Name, OwnerUserId)
    members    = IMembershipRepository.ListMembersByGroupsAsync(groupIds)
                                                                    -> set of (GroupId, UserId)
    creatorIds = meetups.Where(m => members.Contains((m.GroupId, m.CreatedByUserId)))
                        .Select(CreatedByUserId).Distinct()
    users      = IUserRepository.ListByIdsAsync(creatorIds)         -> id -> DisplayName
    project each meetup, applying:
        GroupName            = groups[GroupId]?.Name           ?? string.Empty
        GroupOwnerUserId     = groups[GroupId]?.OwnerUserId     ?? Guid.Empty
        CreatedByDisplayName = members.Contains((GroupId, CreatedByUserId))
                                 ? users[CreatedByUserId]?.DisplayName ?? string.Empty
                                 : string.Empty
```

**Guarantees**:
- Exactly three lookups per uncached request regardless of meetup count — no per-row query.
- A creator who is not a current member of that meetup's group MUST yield `string.Empty` (data-model INV-1).
- Missing group/membership/profile rows MUST yield empty values, never an exception (INV-2).
- No caller-specific value may be added to these responses — the payload is shared across every reader of the cache key (INV-3).
- Both methods MUST log the resolution outcome, e.g. `Loaded meetups for group {GroupId} count={Count} organizersResolved={Resolved} organizersUnresolved={Unresolved}`, so an unexpected fallback rate is observable (Constitution VII).

## 3. Repository changes

```csharp
// LoopMeet.Core/Interfaces/IMembershipRepository.cs — added
Task<IReadOnlyList<Membership>> ListMembersByGroupsAsync(
    IReadOnlyList<Guid> groupIds, CancellationToken cancellationToken = default);

// LoopMeet.Core/Interfaces/IMeetupRepository.cs — signature simplified
Task<IReadOnlyList<Meetup>> ListUpcomingByUserAsync(
    Guid userId, CancellationToken cancellationToken = default);
    // was: Task<IReadOnlyList<(Meetup Meetup, string GroupName)>>
```

- `ListMembersByGroupsAsync` MUST return an empty list for an empty input without querying, and MUST be a single query filtered on the group-id set (the group-scoped path calls it with one id, so both list paths share one membership code path).
- `MeetupRepository.ListUpcomingByUserAsync` drops its group-name lookup block; group data now comes from the query service (research D5). The in-memory fake must be updated in step.

## 4. Client model contract

```csharp
// LoopMeet.App/Features/Meetups/Models/MeetupModels.cs — MeetupSummary additions
public string CreatedByDisplayName { get; init; } = string.Empty;
public Guid GroupOwnerUserId { get; init; }

public string OrganizerDisplay => MeetupOrganizerText.Format(CreatedByDisplayName);
public bool CanOpenLocation => Latitude is not null && Longitude is not null;
```

```csharp
// LoopMeet.App/Features/Meetups/MeetupOrganizerText.cs — new, pure, no MAUI dependency
public static class MeetupOrganizerText
{
    public const string UnknownOrganizer = "A group member";

    /// FR-011: never render a blank organizer or an internal identifier.
    public static string Format(string? displayName) =>
        string.IsNullOrWhiteSpace(displayName) ? UnknownOrganizer : displayName;
}
```

`CanOpenLocation` replaces the guard currently duplicated inside `HomeViewModel.OpenLocationAsync` and `GroupDetailViewModel.OpenLocationAsync`; both commands and the details screen bind to it so a location can never be openable in one place and not another.

## 5. Details screen contract

**Route**: `meetup-detail`, registered in `AppShell.xaml.cs` alongside the existing detail routes.

**Navigation** (identical from both entry points — no meetup object is passed):

```csharp
Shell.Current.GoToAsync("meetup-detail", new Dictionary<string, object>
{
    ["groupId"] = meetup.GroupId,
    ["meetupId"] = meetup.Id
});
```

**View model** (`MeetupDetailViewModel`):

```csharp
public sealed partial class MeetupDetailViewModel : ObservableObject
{
    // Applied from query properties before LoadAsync
    public void ApplyParameters(Guid groupId, Guid meetupId);

    [RelayCommand] private Task LoadAsync();          // re-reads on every appearance (INV-5)
    [RelayCommand] private Task OpenLocationAsync();  // only reachable when CanOpenLocation
    [RelayCommand] private Task EditAsync();          // only reachable when IsOwner

    // Bound state
    public string Title { get; }
    public string DateTimeDisplay { get; }
    public string LocationDisplay { get; }   // "TBD" when unset, via existing MeetupSummary logic
    public string GroupName { get; }
    public string OrganizerDisplay { get; }  // FR-011 fallback applied
    public bool CanOpenLocation { get; }     // gates the map control
    public bool IsOwner { get; }             // gates the pencil
    public bool IsLoading { get; }
    public bool IsNotFound { get; }
    public bool HasError { get; }
}
```

- `LoadAsync` MUST call `MeetupsApi.GetGroupMeetupsAsync(groupId)` and select by `meetupId`; absence MUST set `IsNotFound` (not an error) per data-model §4.
- `IsOwner` MUST be `GroupOwnerUserId == AuthService.GetCurrentUserId()` and nothing else — not the entry point, not `CreatedByUserId` (FR-016).
- `EditAsync` navigates to the existing `edit-meetup` route with `groupId` + `meetupId`, unchanged.
- The page MUST expose no delete action (FR-014).

## 6. Form save-control contract (FR-001..FR-005)

Both `CreateMeetupPage.xaml` and `EditMeetupPage.xaml`:

- The page title `Label` moves into a two-column row (title stretches, save control pinned right); the row sits **outside** the scrolling region so it cannot be scrolled away or covered by the keyboard.
- The save control is an `ImageButton`-style icon reusing `ic_save.png`, bound to the existing `SaveCommand`. No new command, no changed validation, no changed error label.
- `SemanticProperties.Description` MUST be set ("Save meetup" / "Save changes") since the control has no visible text (FR-005).
- The former bottom `<Button Text="Create Meetup" …>` / `<Button Text="Save Changes" …>` MUST be deleted, not hidden (FR-003).
- The existing `ShowFormFields` collapse-during-location-search behavior is untouched; the save row remains visible in that state (spec edge case).

## 7. Card contract (FR-019, FR-020)

For both `HomePage.xaml` and `GroupDetailPage.xaml` meetup templates:

| Element | Gesture | Destination |
| --- | --- | --- |
| Card border (whole card) | tap | `meetup-detail` |
| Location text | **none** — the existing `TapGestureRecognizer` on the label is removed | (inherits card tap) |
| Map glyph on the location row, visible iff `CanOpenLocation` | tap | native maps app |

- The glyph is a text glyph, consistent with the app's existing `🗑`/`✓` usage (FR-021) — no new image asset.
- On `GroupDetailPage` the surrounding `SwipeView` and its owner-gated delete item are **unchanged** (FR-018); only the border's tap command changes from `EditMeetupCommand` to the details command, and that command MUST NOT be owner-gated (FR-008).
- `GroupDetailViewModel.EditMeetupCommand` remains for the details screen's use; its owner guard stays.

## 8. Versioning / migration notes

- API and client ship together (single mobile client, no external consumers), so the `MeetupsResponse` element-type change needs no versioning. An older client reading the new payload would simply ignore three unknown fields.
- No database migration. No cache-key change: entries written before deployment deserialize with the new fields defaulted and expire within 30 seconds (research D9).
