# API Contract: Meetups

**Feature Branch**: `006-group-meetups`
**Date**: 2026-03-30
**Base Path**: `/groups/{groupId}/meetups`

All endpoints require authentication (Bearer token via `ApiAuthHandler`).

---

## GET /groups/{groupId}/meetups

Returns upcoming meetups for a group (where `scheduled_at > now()`), sorted by `scheduled_at` ascending.

**Authorization**: Any group member (enforced by RLS).

**Path Parameters**:

| Parameter | Type | Description |
|-----------|------|-------------|
| `groupId` | `uuid` | The group ID |

**Response** `200 OK`:

```json
{
  "meetups": [
    {
      "id": "uuid",
      "groupId": "uuid",
      "title": "string",
      "scheduledAt": "2026-04-15T18:00:00Z",
      "placeName": "string | null",
      "placeAddress": "string | null",
      "latitude": 40.7829,
      "longitude": -73.9654,
      "placeId": "string | null",
      "createdByUserId": "uuid"
    }
  ]
}
```

**Response** `403 Forbidden`: User is not a member of the group.

---

## POST /groups/{groupId}/meetups

Creates a new meetup in the group.

**Authorization**: Any group member at RLS level (UI restricts to owner per FR-008).

**Path Parameters**:

| Parameter | Type | Description |
|-----------|------|-------------|
| `groupId` | `uuid` | The group ID |

**Request Body**:

```json
{
  "title": "string (required, max 200 chars)",
  "scheduledAt": "2026-04-15T18:00:00Z (required, must be future)",
  "placeName": "string | null",
  "placeAddress": "string | null",
  "latitude": 40.7829,
  "longitude": -73.9654,
  "placeId": "string | null"
}
```

**Response** `201 Created`:

```json
{
  "id": "uuid",
  "groupId": "uuid",
  "title": "string",
  "scheduledAt": "2026-04-15T18:00:00Z",
  "placeName": "string | null",
  "placeAddress": "string | null",
  "latitude": 40.7829,
  "longitude": -73.9654,
  "placeId": "string | null",
  "createdByUserId": "uuid"
}
```

**Response** `400 Bad Request`: Validation failure (empty title, past date, invalid coordinates).
**Response** `403 Forbidden`: User is not a member of the group.

---

## PATCH /groups/{groupId}/meetups/{meetupId}

Updates an existing meetup.

**Authorization**: Any group member at RLS level (UI restricts to owner per FR-008).

**Path Parameters**:

| Parameter | Type | Description |
|-----------|------|-------------|
| `groupId` | `uuid` | The group ID |
| `meetupId` | `uuid` | The meetup ID |

**Request Body** (all fields optional — only provided fields are updated):

```json
{
  "title": "string (max 200 chars)",
  "scheduledAt": "2026-04-20T19:00:00Z (must be future)",
  "placeName": "string | null",
  "placeAddress": "string | null",
  "latitude": 40.7829,
  "longitude": -73.9654,
  "placeId": "string | null"
}
```

To clear a location, send `placeName`, `placeAddress`, `latitude`, `longitude`, and `placeId` all as `null`.

**Response** `200 OK`: Updated meetup (same shape as POST response).
**Response** `400 Bad Request`: Validation failure.
**Response** `403 Forbidden`: User is not a member of the group.
**Response** `404 Not Found`: Meetup does not exist or does not belong to this group.

---

## DELETE /groups/{groupId}/meetups/{meetupId}

Hard-deletes a meetup.

**Authorization**: Any group member at RLS level (UI restricts to owner per FR-008).

**Path Parameters**:

| Parameter | Type | Description |
|-----------|------|-------------|
| `groupId` | `uuid` | The group ID |
| `meetupId` | `uuid` | The meetup ID |

**Response** `204 No Content`: Meetup deleted successfully.
**Response** `403 Forbidden`: User is not a member of the group.
**Response** `404 Not Found`: Meetup does not exist or does not belong to this group.

---

## GET /meetups/upcoming

Returns upcoming meetups across all groups the authenticated user belongs to, sorted by `scheduled_at` ascending. Used by the home page.

**Authorization**: Authenticated user. Results scoped to user's groups via RLS.

**Response** `200 OK`:

```json
{
  "meetups": [
    {
      "id": "uuid",
      "groupId": "uuid",
      "groupName": "string",
      "title": "string",
      "scheduledAt": "2026-04-15T18:00:00Z",
      "placeName": "string | null",
      "placeAddress": "string | null",
      "latitude": 40.7829,
      "longitude": -73.9654,
      "placeId": "string | null",
      "createdByUserId": "uuid"
    }
  ]
}
```

Note: Includes `groupName` so the home page can display which group each meetup belongs to (FR-016).

---

## Refit Interface (Client-Side)

```csharp
public interface IMeetupsApi
{
    [Get("/groups/{groupId}/meetups")]
    Task<MeetupsResponse> GetGroupMeetupsAsync(Guid groupId);

    [Post("/groups/{groupId}/meetups")]
    Task<MeetupSummary> CreateMeetupAsync(Guid groupId, [Body] CreateMeetupRequest request);

    [Patch("/groups/{groupId}/meetups/{meetupId}")]
    Task<MeetupSummary> UpdateMeetupAsync(Guid groupId, Guid meetupId, [Body] UpdateMeetupRequest request);

    [Delete("/groups/{groupId}/meetups/{meetupId}")]
    Task DeleteMeetupAsync(Guid groupId, Guid meetupId);

    [Get("/meetups/upcoming")]
    Task<UpcomingMeetupsResponse> GetUpcomingMeetupsAsync();
}
```
