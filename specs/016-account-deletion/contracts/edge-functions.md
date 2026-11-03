# Edge Function Contracts: Account Deletion

## `account-purge` (new)

**Trigger**: `pg_cron` job `account-purge-hourly` (`7 * * * *`), `POST {}` with the publishable key, the same as `reminders-scheduler`. Extra invocations are harmless because only due rows are processed (research D4).

**Secrets**:
- `SUPABASE_URL`, `SUPABASE_SERVICE_ROLE_KEY` (existing)
- `ONESIGNAL_APP_ID`, `ONESIGNAL_REST_API_KEY` (existing)
- `APPLE_TEAM_ID`, `APPLE_KEY_ID`, `APPLE_PRIVATE_KEY`, `APPLE_CLIENT_ID` (new)
- `AVATAR_BUCKET_NAME` (new; must match the Api's `Supabase:AvatarBucketName`)

**Behavior**: `claim_due_account_purges(10)`, then for each claimed row the steps not yet in `completed_steps`, in order:

| Step | Action | Counts as done |
| --- | --- | --- |
| `onesignal` | `DELETE https://api.onesignal.com/apps/{ONESIGNAL_APP_ID}/users/by/external_id/{user_id}`, header `Authorization: Key {ONESIGNAL_REST_API_KEY}` | `202`, `404` |
| `apple` | For each `apple_authorizations` row: `POST https://appleid.apple.com/auth/revoke` (form: `client_id`, `client_secret`, `token`, `token_type_hint=refresh_token`), then delete the row | `200` for every row. With no rows, logs `apple_token_unavailable` if the user has an `apple` identity, and is done. |
| `storage` | List `{AVATAR_BUCKET_NAME}/{user_id}/` and remove all objects, repeating until empty | Listing is empty |
| `database` | `rpc('purge_account_data', { p_user_id })` | No error |
| `auth` | `auth.admin.deleteUser(user_id)` | Success or "user not found" |

After each step: `rpc('record_account_purge_step', { p_user_id, p_step, p_error: null })`. On failure: `p_error = '<step>_<status|exception code>'`, and that user's run stops (FR-017). Other claimed users continue.

**Response** (for logs and manual runs):

```json
{ "ok": true, "claimed": 2, "completed": 1, "failed": 1 }
```

**Log lines** (JSON, `user_id` only, never an email or token):
- `account_purge_started` `{user_id, attempt, resume_from}`
- `account_purge_step_completed` `{user_id, step}`
- `account_purge_step_failed` `{user_id, step, error_code}`
- `account_purge_completed` `{user_id, attempts}`
- `account_purge_stuck` (error level) `{user_id, attempts, last_error}` when `attempts >= 24`
- `apple_token_unavailable` `{user_id}`

**Testability**: `index.ts` only wires real ports. The orchestration lives in `purge-runner.ts`:

```ts
runPurge(ports: PurgePorts, now: Date): Promise<PurgeSummary>
interface PurgePorts { claim, recordStep, deleteOneSignalUser, listAppleAuthorizations, revokeApple, deleteAppleAuthorization, hasAppleIdentity, listAvatarObjects, removeAvatarObjects, purgeDatabase, deleteAuthUser, log }
```

## `apple-authorization` (new)

**Caller**: the MAUI app right after a successful Sign in with Apple, via `Supabase.Client.Functions.Invoke("apple-authorization", …)` with the user's access token. 017 web will call it with a refresh token instead of a code.

**Request**:

```json
{ "authorizationCode": "c1a2…" }
```

or, for 017 web:

```json
{ "refreshToken": "r9…", "clientId": "io.loopmeet.web" }
```

**Behavior**:
1. Verify the bearer token with `auth.getUser(jwt)`. Reject with `401` otherwise.
2. With a code, `POST https://appleid.apple.com/auth/token` (form: `client_id=APPLE_CLIENT_ID`, `client_secret`, `code`, `grant_type=authorization_code`), then take `refresh_token`.
3. Upsert `apple_authorizations (user_id, client_id, refresh_token)`.

**Responses**:
- `204` when stored
- `400 {"code":"invalid_request"}`
- `401`
- `502 {"code":"apple_exchange_failed"}` (Apple's status is logged, the body is not)

**Logs**: `apple_authorization_stored` `{user_id, client_id}` and `apple_authorization_failed` `{user_id, status}`.

**Client rule**: the app calls this fire-and-forget after sign-in completes. A failure is logged and **never** blocks or fails sign-in. The next Apple sign-in tries again.

## `_shared/apple-auth.ts` (new)

```ts
createClientSecret(env: AppleEnv, now: Date): Promise<string>     // ES256 JWT: iss=TEAM_ID, iat, exp=iat+300, aud="https://appleid.apple.com", sub=clientId; header kid=KEY_ID
exchangeAuthorizationCode(env, code, fetchImpl?): Promise<string> // refresh_token
revokeRefreshToken(env, clientId, token, fetchImpl?): Promise<void>
```

## `_shared/account-status.ts` (new)

```ts
isAccountActive(supabase, userId): Promise<boolean>        // no account_deletions row
```

## Changed functions

- **`notifications-dispatch/recipient-resolver.ts`**
  - meetups: reads `active_memberships` instead of `memberships`.
  - invitations: returns `[]` unless `isAccountActive(invited_user_id)`.
- **`reminders-scheduler/index.ts`**: `fetchRecipients` reads `active_memberships`. A pending owner's groups therefore produce no plan entries, and pending members get no reminders (FR-008).
