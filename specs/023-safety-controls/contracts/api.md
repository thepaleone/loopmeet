# Api contract: safety controls

## Conventions

- **Authorization:** every endpoint requires a bearer token (`.RequireAuthorization()`). The caller is `CurrentUserService.UserId`.
- **Errors:** errors use the existing `ErrorResponse { code, message }`. A `message` is shown to the user as written.
- **Account status:** a user pending deletion gets the existing `403 account_pending_deletion` from `AccountDeletionGateMiddleware`, before any endpoint here runs.
- **JSON:** camelCase, as in the existing contracts.

## US1: reports

### `POST /reports`

Request:

```json
{
  "groupId": "uuid",
  "reportedUserId": "uuid or null",
  "reason": "inappropriate_photo_or_name | harassment_or_threats | child_safety | spam | other",
  "details": "string, 1–1000 characters after trimming"
}
```

| Status | Code | Message | When |
| --- | --- | --- | --- |
| `201` | | body `{ "id": "uuid", "receivedAt": "ISO 8601" }` | recorded. The email follows asynchronously |
| `400` | `invalid_report` | "Choose a reason and describe what happened (up to 1,000 characters)." | validator or `invalid_report` |
| `403` | `not_group_member` | "You can only report groups you belong to." | |
| `422` | `invalid_reported_user` | "Choose someone in this group other than yourself." | self, or not a member |
| `429` | `report_limit_reached` | "You've sent 10 reports today. You can send more later, or email abuse@loopmeet.io." | 10 in 24 hours |

`Location: /reports/{id}`. Reports are never read back through the Api: there is no `GET`.

## US2: leave a group

### `POST /groups/{groupId}/leave`

No body.

| Status | Code | Message |
| --- | --- | --- |
| `204` | | left |
| `404` | `not_group_member` | "You're not a member of this group." The App treats this as already left: it returns to Groups and shows no error (race edge case) |
| `409` | `owner_cannot_leave` | "Group owners can't leave their own group." |

## US3: remove, block and lift

### `POST /groups/{groupId}/leave-and-block`

No body. The statuses are the same as `/leave`. A success also blocks the group from inviting the caller.

### `DELETE /groups/{groupId}/members/{userId}?block=false|true`

| Status | Code | Message |
| --- | --- | --- |
| `204` | | removed (and blocked when `block=true`). Without `block`, also returned when the person had already left (race) |
| `404` | `not_group_member` | "They've already left this group." Only with `block=true`, because a block needs a current member |
| `403` | `not_group_owner` | "Only the group's owner can do that." |
| `409` | `cannot_remove_owner` | "The group's owner can't be removed." |

### `GET /groups/{groupId}/blocks`

The owner's Blocked list. It returns only blocks the owner set.

```json
[{ "userId": "uuid", "displayName": "string", "blockedAt": "ISO 8601" }]
```

`403 not_group_owner` for anyone else. The list is cached as `group-blocks:{groupId}` for 30 seconds, and invalidated on block and lift.

### `DELETE /groups/{groupId}/blocks/{userId}`

The owner lifts a block they set. It returns `204`, `403 not_group_owner`, or `404 block_not_found`, "This person isn't blocked."

### `GET /blocks`

The caller's own blocks (Leave and block, or Decline and block).

```json
[{ "groupId": "uuid", "groupName": "string", "blockedAt": "ISO 8601" }]
```

It's cached as `my-blocks:{userId}`. `groupName` is read at request time.

### `DELETE /blocks/{groupId}`

The caller lifts their own block. It returns `204` or `404 block_not_found`.

### `POST /invitations/{invitationId}/decline-and-block`

The statuses are the same as the existing `/decline`: `204`, or `404` when the invitation isn't pending or isn't addressed to the caller. A success also blocks the group.

### Changed: `POST /groups/{groupId}/invitations`

New: `409 invitee_blocked`, "This person can't be invited to this group." It's returned whoever set the block, and before the existing duplicate and member checks.

### Changed: `POST /invitations/{invitationId}/accept`

New: `409 invitation_unavailable`, "This invitation is no longer available." The invitation becomes `cancelled`.

## Cache invalidation (research D9)

| Action | Keys removed |
| --- | --- |
| leave, leave-and-block, remove, block | `group-detail:{groupId}`; `groups:{userId}` for the person and every remaining member; `MeetupQueryService.HomeCacheKey(person)`; `meetups:{groupId}` |
| block, leave-and-block, decline-and-block, lift | `group-blocks:{groupId}`; `my-blocks:{person}` |
| decline-and-block | `pending-invitations:{email}` (existing) |

## App (Refit)

| Interface | Method | Route |
| --- | --- | --- |
| `ISafetyApi` (new) | `SubmitReportAsync(ReportRequest)` | `POST /reports` |
| `ISafetyApi` | `GetMyBlocksAsync()` | `GET /blocks` |
| `ISafetyApi` | `LiftMyBlockAsync(groupId)` | `DELETE /blocks/{groupId}` |
| `IGroupsApi` | `LeaveGroupAsync(groupId)` | `POST /groups/{groupId}/leave` |
| `IGroupsApi` | `LeaveAndBlockAsync(groupId)` | `POST /groups/{groupId}/leave-and-block` |
| `IGroupsApi` | `RemoveMemberAsync(groupId, userId, block)` | `DELETE /groups/{groupId}/members/{userId}` |
| `IGroupsApi` | `GetGroupBlocksAsync(groupId)` | `GET /groups/{groupId}/blocks` |
| `IGroupsApi` | `LiftGroupBlockAsync(groupId, userId)` | `DELETE /groups/{groupId}/blocks/{userId}` |
| `IInvitationsApi` | `DeclineAndBlockAsync(invitationId)` | `POST /invitations/{invitationId}/decline-and-block` |

`ISafetyApi` gets a thin `SafetyApi` wrapper, the same as `GroupsApi`.
