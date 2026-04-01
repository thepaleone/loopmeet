# Data Model: Group Meetups

**Feature Branch**: `006-group-meetups`
**Date**: 2026-03-30

## New Entity: Meetup

### Database Table: `meetups`

| Column | Type | Nullable | Default | Notes |
|--------|------|----------|---------|-------|
| `id` | `uuid` | NOT NULL | `gen_random_uuid()` | Primary key |
| `group_id` | `uuid` | NOT NULL | — | FK → `groups(id) ON DELETE CASCADE` |
| `created_by_user_id` | `uuid` | NOT NULL | — | The user who created the meetup |
| `title` | `varchar` | NOT NULL | — | Meetup title (non-empty) |
| `scheduled_at` | `timestamptz` | NOT NULL | — | Date/time of meetup in UTC |
| `place_name` | `varchar` | NULL | `NULL` | Display name of location (e.g., "Central Park") |
| `place_address` | `varchar` | NULL | `NULL` | Formatted address |
| `latitude` | `double precision` | NULL | `NULL` | Geographic latitude |
| `longitude` | `double precision` | NULL | `NULL` | Geographic longitude |
| `place_id` | `varchar` | NULL | `NULL` | Google Place ID (for future use) |
| `created_at` | `timestamptz` | NOT NULL | `now()` | Record creation timestamp |
| `updated_at` | `timestamptz` | NOT NULL | `now()` | Last update timestamp |

### Constraints

- `PRIMARY KEY (id)`
- `FOREIGN KEY (group_id) REFERENCES groups(id) ON DELETE CASCADE` — deleting a group cascades to its meetups
- Location fields are all-or-nothing: if `place_name` is set, `latitude` and `longitude` must also be set (enforced at application layer, not DB constraint, to keep the schema simple)

### Indexes

- `idx_meetups_group_scheduled` on `(group_id, scheduled_at)` — supports the primary query pattern (upcoming meetups for a group, ordered by date)
- `idx_meetups_scheduled_at` on `(scheduled_at)` — supports the home page cross-group query

### RLS Policies

Following the existing pattern where RLS is enabled but policies are permissive for group members:

```sql
-- All group members can read meetups for their groups
CREATE POLICY "meetups_select_group_member"
    ON meetups FOR SELECT
    USING (
        EXISTS (
            SELECT 1 FROM memberships
            WHERE memberships.group_id = meetups.group_id
              AND memberships.member_user_id = auth.uid()
        )
    );

-- All group members can insert meetups (UI restricts to owner, but RLS is permissive per FR-010)
CREATE POLICY "meetups_insert_group_member"
    ON meetups FOR INSERT
    WITH CHECK (
        EXISTS (
            SELECT 1 FROM memberships
            WHERE memberships.group_id = meetups.group_id
              AND memberships.member_user_id = auth.uid()
        )
    );

-- All group members can update meetups
CREATE POLICY "meetups_update_group_member"
    ON meetups FOR UPDATE
    USING (
        EXISTS (
            SELECT 1 FROM memberships
            WHERE memberships.group_id = meetups.group_id
              AND memberships.member_user_id = auth.uid()
        )
    );

-- All group members can delete meetups
CREATE POLICY "meetups_delete_group_member"
    ON meetups FOR DELETE
    USING (
        EXISTS (
            SELECT 1 FROM memberships
            WHERE memberships.group_id = meetups.group_id
              AND memberships.member_user_id = auth.uid()
        )
    );
```

### State Transitions

Meetups have no status field. Their lifecycle is:
1. **Created** → row inserted with `scheduled_at` in the future
2. **Updated** → any field modified, `updated_at` refreshed
3. **Deleted** → row hard-deleted (no soft-delete)
4. **Past** → not a state change; simply filtered out by queries when `scheduled_at <= now()`

## Relationships

```text
groups (1) ──── (*) meetups
  │                    │
  │                    └── created_by_user_id (references auth.users)
  │
  └──── (*) memberships ──── determines read/write access to meetups
```

## Domain Entity: `Meetup` (LoopMeet.Core)

```csharp
public sealed class Meetup
{
    public Guid Id { get; init; }
    public Guid GroupId { get; init; }
    public Guid CreatedByUserId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTimeOffset ScheduledAt { get; init; }
    public string? PlaceName { get; init; }
    public string? PlaceAddress { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? PlaceId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}
```

## Supabase Record: `MeetupRecord` (LoopMeet.Infrastructure)

Follows the existing pattern where `*Record` classes map to Postgrest table models with `[Table]` and `[Column]` attributes from the Supabase SDK.

```csharp
[Table("meetups")]
public sealed class MeetupRecord : BaseModel
{
    [PrimaryKey("id")]
    public string Id { get; set; } = string.Empty;

    [Column("group_id")]
    public string GroupId { get; set; } = string.Empty;

    [Column("created_by_user_id")]
    public string CreatedByUserId { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("scheduled_at")]
    public string ScheduledAt { get; set; } = string.Empty;

    [Column("place_name")]
    public string? PlaceName { get; set; }

    [Column("place_address")]
    public string? PlaceAddress { get; set; }

    [Column("latitude")]
    public double? Latitude { get; set; }

    [Column("longitude")]
    public double? Longitude { get; set; }

    [Column("place_id")]
    public string? PlaceId { get; set; }

    [Column("created_at")]
    public string CreatedAt { get; set; } = string.Empty;

    [Column("updated_at")]
    public string UpdatedAt { get; set; } = string.Empty;
}
```

## Client-Side Models (LoopMeet.App)

### MeetupSummary (for list display)

```csharp
public sealed class MeetupSummary
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset ScheduledAt { get; set; }
    public string? PlaceName { get; set; }
    public string? PlaceAddress { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? GroupName { get; set; }  // populated for home page cards

    // Calculated properties
    public bool HasLocation => !string.IsNullOrWhiteSpace(PlaceName);
    public string LocationDisplay => HasLocation ? PlaceName! : "TBD";
    public string DateDisplay => ScheduledAt.LocalDateTime.ToString("ddd, MMM d");
    public string TimeDisplay => ScheduledAt.LocalDateTime.ToString("h:mm tt");
    public string DateTimeDisplay => $"{DateDisplay} at {TimeDisplay}";
}
```

### HomeMeetupCard (for home page display with group context)

Reuses `MeetupSummary` with `GroupName` populated.

## Validation Rules

| Field | Rule |
|-------|------|
| `title` | Non-empty, max 200 characters |
| `scheduled_at` | Must be in the future (at creation time only; edits allow moving to a different future date) |
| `place_name` | If provided, `latitude` and `longitude` must also be provided |
| `latitude` | Range: -90 to 90 |
| `longitude` | Range: -180 to 180 |
