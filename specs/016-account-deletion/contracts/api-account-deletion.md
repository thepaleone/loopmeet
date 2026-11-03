# Api Contract: Account Deletion

All endpoints require a valid Supabase bearer token (`RequireAuthorization`) and act on the caller only. JSON is camelCase, timestamps are ISO-8601 UTC, and errors use the existing `ErrorResponse { code, message }`. These endpoints are client-agnostic: the MAUI app uses them now and the 017 web page will use them later (FR-004).

## `GET /users/account-deletion`

The caller's deletion status. It is allowed while pending.

**200**

```json
{ "status": "none" }
```

```json
{ "status": "pending", "requestedAt": "2026-11-03T18:00:00Z", "deletesAt": "2026-12-03T18:00:00Z" }
```

`status` is one of `none`, `pending` or `purging`. `purging` means the date has passed and restore is no longer possible.

## `GET /users/account-deletion/preview`

What a deletion request would remove. This feeds the confirmation screen (FR-001). It is allowed while pending, and returns the existing date in that case.

**200**

```json
{
  "deletesAt": "2026-12-03T18:00:00Z",
  "ownedGroups": [
    { "id": "…", "name": "Friday Climbers", "memberCount": 3 },
    { "id": "…", "name": "Book Club", "memberCount": 1 }
  ]
}
```

- `deletesAt` is `now + 30 days` (`AccountDeletionPolicy.GracePeriod`), or the stored date if already pending.
- `memberCount` includes the owner and is read through RLS as the caller.
- `ownedGroups` is ordered by name and is `[]` when the caller owns none.

## `POST /users/account-deletion`

Requests deletion (FR-003). It is idempotent (FR-005).

**Request**

```json
{ "confirmation": "DELETE" }
```

**202**: the same body as `GET /users/account-deletion` with `status: "pending"`. A repeat request returns the original dates.

**400** `{ "code": "invalid_confirmation", "message": "Type DELETE to confirm." }` when `confirmation` isn't exactly `DELETE` (ordinal, no trimming). The server checks this too, so every client enforces FR-002.

**409** `{ "code": "deletion_in_progress", "message": "This account is already being deleted." }` when the status is `purging`.

Side effects:
- invalidates `account-deletion:{userId}`
- logs `AccountDeletionRequested` or `AccountDeletionAlreadyPending`

## `DELETE /users/account-deletion`

Restores the account (FR-011, FR-012).

**200** `{ "status": "none" }`. This is also returned when nothing was pending, so the call is idempotent.

**409** `{ "code": "deletion_in_progress", "message": "Your account can no longer be restored." }` when `purge_after` has passed or purging has started (US3 scenario 6).

Side effects:
- invalidates `account-deletion:{userId}`
- logs `AccountDeletionRestored` or `AccountDeletionRestoreRefused`

## Gate: every other authenticated endpoint

While the caller is `pending` or `purging`, **every authenticated request outside `/users/account-deletion*`** gets:

**403**

```json
{ "code": "account_pending_deletion", "message": "This account is scheduled for deletion.", "deletesAt": "2026-12-03T18:00:00Z" }
```

- The status is cached for 30 seconds per user.
- Unauthenticated requests are unaffected.
- `403` is distinct from the `401` session handling in `ApiAuthHandler`, so clients must not treat it as a sign-out.

## App client additions (`IUsersApi` / `UsersApi`)

```csharp
[Get("/users/account-deletion")]          Task<AccountDeletionStatusResponse> GetAccountDeletionAsync();
[Get("/users/account-deletion/preview")]  Task<AccountDeletionPreviewResponse> GetAccountDeletionPreviewAsync();
[Post("/users/account-deletion")]         Task<AccountDeletionStatusResponse> RequestAccountDeletionAsync([Body] AccountDeletionRequest request);
[Delete("/users/account-deletion")]       Task<AccountDeletionStatusResponse> RestoreAccountAsync();
```

`AccountStatusHandler` (a `DelegatingHandler` placed after `ApiAuthHandler`) inspects `403` responses. If the body's `code` is `account_pending_deletion`, it calls `AccountDeletionGate.OnPendingDeletion(deletesAt)` and passes the response through unchanged.
