# API Contracts: Profile Avatar Management (005-profile-avatar)

## New Endpoint: Upload Avatar

### `POST /users/avatar`

Uploads a new avatar image for the authenticated user. The backend stores the image in
configured cloud storage (Supabase Storage) and updates the user's profile avatar URL.

**Authentication**: Required — Bearer token via `Authorization` header (same as all existing endpoints).

**Request**

```
Content-Type: multipart/form-data

Part:
  name:         image
  content-type: image/jpeg | image/png
  body:         <binary image data>
```

**Response — 200 OK**

Returns the full updated `UserProfileResponse`:

```json
{
  "displayName": "Jane Smith",
  "email": "jane@example.com",
  "phone": null,
  "avatarUrl": "https://storage.supabase.co/v1/object/public/avatars/user-uuid/avatar.jpg",
  "avatarSource": "upload",
  "userSince": "2025-01-15T10:00:00Z",
  "groupCount": 3,
  "canChangePassword": true,
  "hasEmailProvider": true,
  "requiresCurrentPassword": true,
  "requiresEmailForPasswordSetup": false
}
```

**Error Responses**

| Status | Condition |
|--------|-----------|
| 400 | Missing or invalid image part; unsupported content type |
| 401 | Missing or expired auth token |
| 413 | Image exceeds maximum allowed size |
| 500 | Storage upload failure |

---

## Modified Endpoint: PATCH /users/profile

The existing `UserProfileUpdateRequest` body gains one optional field:

```json
{
  "displayName": "Jane Smith",
  "avatarOverrideUrl": null,
  "socialAvatarUrl": "https://lh3.googleusercontent.com/..."
}
```

| Field | Type | Behaviour |
|-------|------|-----------|
| `displayName` | string | Replaces stored display name |
| `avatarOverrideUrl` | string? | If non-null, sets avatar to this URL unconditionally |
| `socialAvatarUrl` | string? | If non-null **and** current stored avatar is null/empty, sets avatar to this URL; otherwise ignored |

**Precedence** (server-side): `avatarOverrideUrl` > `socialAvatarUrl` > current stored value.

The client **never** sends both fields populated at the same time.

---

## Unchanged Endpoints (reference)

| Method | Path | Used by |
|--------|------|---------|
| `GET` | `/users/profile` | `ProfileViewModel.LoadAsync`, `LoginViewModel` (post-sign-in) |
| `POST` | `/users/profile` | `CreateAccountViewModel` (new account upsert, includes `SocialAvatarUrl`) |
| `PATCH` | `/users/profile` | `ProfileViewModel.SaveProfileAsync` (display name only after this feature) |
