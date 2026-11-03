# Research: Account Deletion with a 30-Day Grace Period

Decisions made while planning 016, each with the alternatives considered. References are to the codebase on `main` at 2026-11-03.

## Findings that shape the design

- **All Api data access goes through row-level security.** The Api builds a `Supabase.Client` per request that impersonates the caller's bearer token ([Program.cs](../../src/LoopMeet.Api/Program.cs)). Every read is therefore filtered by the policies in `supabase/migrations/*rls*.sql` and `*_add_meetups.sql`.
- **The publishable key is public.** Any signed-in user can call PostgREST directly with their own JWT, bypassing the Api. Anything enforced only in the Api is advisory.
- **The Api never uses its service-role key.** `SupabaseConfigOptions.ServiceOrSecretKey` is configured but unused. All privileged work today runs in edge functions (`notifications-dispatch`, `reminders-scheduler`) with `SUPABASE_SERVICE_ROLE_KEY`.
- **Edge functions bypass RLS.** Recipient resolution (`notifications-dispatch/recipient-resolver.ts`) and the reminder query (`reminders-scheduler/index.ts`) read `memberships` directly, so they need their own "hidden now" handling.
- **Group owners also have a membership row** (`role = 'owner'`, `GroupCommandService`). Meetup visibility is membership-based.
- **There is no group delete policy.** Users can't delete groups today, so group deletion only ever happens at permanent deletion, through privileged SQL.
- **Avatars are never cleaned up.** Each upload writes `{bucket}/{userId}/{guid}.{ext}` (`AvatarStorageService`) and old files are never removed, so the whole `{userId}/` prefix must be deleted. The bucket name is configuration: `avatars` by default, `supabucket` in `docker-compose.yml`.
- **Sign in with Apple discards the authorization code.** `AuthService.SignInWithAppleAsync` uses `SignInWithIdToken` and never keeps `credential.AuthorizationCode`. Supabase doesn't revoke Apple tokens when a user is deleted ([Apple forum 707364](https://developer.apple.com/forums/thread/707364)), and revocation needs a refresh or access token from exchanging that code within 5 minutes.
- **OneSignal users are keyed by `external_id = user_id`** (`user_devices_external_id_ck`). OneSignal's `DELETE /apps/{app_id}/users/by/external_id/{id}` removes the user with all subscriptions and aliases. It returns `202 Accepted` and deletes asynchronously ([OneSignal: Delete user](https://documentation.onesignal.com/reference/delete-user.md)).
- **`notification_open_events.user_id` is `on delete set null`.** Deleting the auth user alone would leave anonymised rows behind, but FR-014 requires them deleted, so the purge deletes them explicitly.

## D1 — "Hidden now" is enforced by RLS, not by moving data

**Decision**:
- Add `account_deletions` (one row per pending user) and two `security definer` helpers: `account_is_active(user_id)` and `group_is_active(group_id)`, the latter meaning the group's owner is active.
- AND those helpers into every existing select, update and delete policy on `groups`, `memberships`, `invitations`, `meetups` and `user_profiles`.
- AND `account_is_active(auth.uid())` into every insert policy, so a pending user can't create data by calling PostgREST directly (FR-009 at the data layer).
- A third helper, `email_is_active(email)`, hides invitations addressed by email to a pending user.

**Rationale**:
- Nothing is copied or moved, so restoring (FR-012) only removes the `account_deletions` row and everything is "exactly as it was".
- RLS is the only layer that also covers direct PostgREST access, so the guarantee doesn't depend on the Api.
- FR-007 ("fail as if they don't exist") holds by construction: hidden rows are invisible to selects, and updates and deletes match zero rows, which the existing repositories already map to not-found.

**Alternatives considered**:
- *Filtering in the Api only*: rejected, because direct PostgREST access and the edge functions would still see the data.
- *Moving data to archive tables*: rejected, because restore becomes a migration and ids and timestamps are at risk.
- *Supabase `ban_duration` on the auth user*: rejected, because it blocks sign-in, which restore needs (FR-010), and it needs service role at request time (FR-019).

**Consequence**: a pending user's own rows are hidden from them too. That's correct: FR-009 confines them to the status, restore and sign-out actions, which only touch `account_deletions`.

## D2 — Request and restore are caller-run database functions

**Decision**:
- `request_account_deletion()` and `restore_account_deletion()` are `security definer` functions that act only on `auth.uid()`. They compute times with `now()` and the 30-day interval in SQL.
- The Api calls them with the caller-impersonating client (`client.Rpc`, the same pattern as `create_membership_from_invitation`).
- Clients can read their own row through a select policy. There are no direct insert, update or delete policies.

**Rationale**:
- FR-019 holds because no elevated access is used at request time.
- Because the times come from the database, a user calling PostgREST directly can't backdate or postpone their own deletion.
- The 30-day rule lives in one authoritative place.
- FR-005 (request twice, same date) is `insert … on conflict do nothing` followed by returning the existing row.
- Restore after the deadline (FR-012), or once purging has started, raises a named exception that the Api maps to `409 deletion_in_progress`.

**Alternatives considered**: computing the times in the Api and inserting through a table policy. Rejected because a client could forge `purge_after`.

**Duplication note**: the confirmation screen shows the projected date before any row exists, so the Api computes `now + 30 days` for the preview. A pgTAP test asserts the SQL interval and an Api test asserts the preview, both at 30 days, and CLAUDE.md gets a gotcha entry (the same treatment as the 012 lifecycle rule).

## D3 — A pending session is redirected by one Api signal, handled once in the app

**Decision**:
- The Api adds `AccountDeletionGateMiddleware` after authorization. For an authenticated caller whose account is pending, every request outside `/users/account-deletion*` gets `403 {"error":"account_pending_deletion","deletesAt":…}`.
- The caller's status is cached for 30 s under `account-deletion:{userId}`, using the existing `ICacheService` TTL pattern. Request and restore invalidate the caller's key.
- In the app, a new `AccountStatusHandler` (a `DelegatingHandler` placed after `ApiAuthHandler`) recognises that response and notifies a singleton `AccountDeletionGate`. The gate navigates once to `//pending-deletion`.

**Rationale**:
- One mechanism covers cold start, interactive sign-in, background session refresh (FR-011) and other devices (US1 scenario 6), with no new checks in the startup path. This respects the 010 rule that startup must not block on warm-up.
- RLS (D1) is the security guarantee. The gate is the user-experience signal, so the 30 s cache can only ever show a stale device empty lists.
- `ApiAuthHandler` stays "never navigates", in line with its contract.

**Alternatives considered**:
- *A JWT custom claim set by an auth hook*: rejected, because claims only change on token refresh (up to an hour later).
- *A status call before every navigation to Home*: rejected, because it adds a blocking request to startup (010 bug C2).
- *An uncached per-request lookup*: rejected, because it adds a database round trip to every Api call. The cache costs at most 30 s of a stale "active" status.

**Visible effect**: after signing in, Home may appear for a moment before the pending screen replaces it. Lists are empty during that moment because of RLS.

## D4 — Permanent deletion runs in a scheduled edge function

**Decision**: a new `account-purge` edge function runs hourly via `pg_cron` + `net.http_post`, the same mechanism as `reminders-scheduler`.
- Each run claims up to 10 due rows: `purge_after <= now()` and a `claimed_at` lease older than 15 minutes, via the service-role-only function `claim_due_account_purges(limit)`.
- It then runs the steps for each claimed row in D5's order.

**Rationale**:
- It reuses the existing scheduler pattern and the secrets already stored there (`SUPABASE_SERVICE_ROLE_KEY`, `ONESIGNAL_REST_API_KEY`, `ONESIGNAL_APP_ID`).
- The Api keeps zero service-role usage (FR-019), and the CLAUDE.md rule against service-role in the Api holds.
- Running hourly keeps permanent deletion well within FR-014's 24-hour bound.

**Alternatives considered**: an Api `IHostedService` using the service-role key. Rejected: it introduces the Api's first privileged client and needs OneSignal and Apple secrets in a second place.

**Invocation note**: as with `reminders-scheduler`, the function is triggered with the publishable key. Invoking it early is harmless because it only ever processes rows that are already due.

## D5 — Ordered, recorded, resumable steps

**Decision**: the steps run in this order:
1. `onesignal`
2. `apple`
3. `storage`
4. `database`
5. `auth`

- Each step is idempotent ("already gone" counts as success). On success it is appended to `account_deletions.completed_steps`.
- A failure stops that user's run. The error code (not the message body) goes into `last_error`, `attempts` increases, and the next run resumes after the last completed step (FR-017).
- The `database` step is one transaction inside `purge_account_data(user_id)`, a `security definer` function executable only by `service_role`. It deletes the user's owned groups (which cascades to their meetups, memberships and invitations), their memberships elsewhere, invitations they sent or received (by user id or by their auth email), meetups they created, their devices, delivery attempts and open events, and their profile and Apple authorizations.
- The `auth` step calls `auth.admin.deleteUser`. The `account_deletions` row is deleted by `on delete cascade` from `auth.users`, so **completion means the row no longer exists** (FR-018: no completion marker is kept).

**Rationale**:
- External steps go first, while the user record and identifiers still exist to retry with.
- The auth user is deleted last, because its email is needed for email-addressed invitations, and deleting it is the step that makes completion final.

**Alternatives considered**:
- *A `completed` state that keeps the row*: rejected, because FR-018 allows it but nothing needs it. The structured completion log (D12) is the audit trail.
- *A `restored` state*: rejected. Restore deletes the row and a later request creates a new one, which keeps the state set to `pending` and `purging`.

**Monitoring**: a row with `attempts >= 24` (about a day of failures) logs `account_purge_stuck` at error level every run until someone resolves it.

## D6 — OneSignal

**Decision**: `DELETE https://api.onesignal.com/apps/{ONESIGNAL_APP_ID}/users/by/external_id/{userId}` with the REST key. Both `202` and `404` count as done.

**Rationale**: it removes every subscription and alias in one call. The documentation says deletion is asynchronous on OneSignal's side, and the data-retention document says so.

## D7 — Sign in with Apple revocation

**Decision**:
- **Capture at sign-in.** After a successful Apple sign-in, the app sends `credential.AuthorizationCode` to a new edge function, `apple-authorization`, using the user's session (`Supabase.Client.Functions`).
- **Exchange.** The function verifies the caller with `auth.getUser`, exchanges the code at `https://appleid.apple.com/auth/token`, and upserts the refresh token into `apple_authorizations (user_id, client_id)`. That table has RLS enabled and no policies, so only the service role can read it.
- **Revoke at purge.** The `apple` step revokes every stored token with `POST https://appleid.apple.com/auth/revoke` (`token_type_hint=refresh_token`), using the `client_id` that issued it.
- **Client secret.** The ES256 client-secret JWT is built in `_shared/apple-auth.ts` with WebCrypto from the new edge secrets `APPLE_TEAM_ID`, `APPLE_KEY_ID`, `APPLE_PRIVATE_KEY` and `APPLE_CLIENT_ID` (`io.loopmeet.app`).

**Rationale**:
- Revocation is impossible without a token, and the code expires 5 minutes after sign-in, so capturing at sign-in is the only reliable source.
- Keeping exchange and revocation in one TypeScript module means one secret location and one implementation.
- Calling an edge function from the app departs from "the app talks to the Api only". It's justified because this is part of the Apple sign-in itself, which already goes directly to Supabase.

**Edge cases**:
- An Apple user with no stored token (for example, signed in before this shipped) is logged as `apple_token_unavailable`, and the step completes, since nothing can be revoked. Apple sign-in hasn't shipped to any store yet, so real users will have tokens.
- A failed exchange at sign-in is logged and retried on the next Apple sign-in. It never blocks sign-in.
- 017 web sign-in with Apple will send Supabase's `provider_refresh_token` to the same function, with the web Services ID as `client_id`.

**Alternatives considered**:
- *Re-authorize with Apple when requesting deletion*: rejected. It doesn't work for the web request path or for an unattended run 30 days later without a stored token anyway.
- *Exchange in the Api*: rejected, because it duplicates the client-secret logic in C# and splits the Apple secrets across two hosts.

**Validation**: Deno tests cover JWT construction and request shapes. End-to-end validation waits for iOS builds (013), as noted in the quickstart.

## D8 — Notifications skip pending users and their groups

**Decision**:
- A view, `active_memberships`, selects memberships whose member and whose group's owner are both active. It is granted to `service_role` only.
- `recipient-resolver.ts` and `reminders-scheduler` read it instead of `memberships`, so a pending owner's group has no recipients and pending members are never recipients (FR-008).
- The invitation recipient is checked with `_shared/account-status.ts → isAccountActive`.
- Nothing is queued while a user is pending, so FR-013 (no catch-up after restore) holds by construction.

**Alternatives considered**: filtering in each function's TypeScript. Rejected because the rule would be duplicated three times. The view keeps it in one SQL definition next to the RLS helpers.

## D9 — Owned-group preview is an Api endpoint

**Decision**: `GET /users/account-deletion/preview` returns the projected deletion date and the caller's owned groups with member counts, read through RLS as the caller.

**Rationale**: the 017 web page needs the same information, so it belongs in the contract rather than being assembled by each client from `/groups`.

## D10 — Testing

**Decision**:
- **Database (new):** pgTAP tests in `supabase/tests/database/`, run with `supabase test db` against a local stack (`supabase start`), in a new `database-tests` CI job. They cover:
  - every changed policy, for a pending owner, a pending member and an active outsider
  - request and restore semantics: 30 days, idempotent, expired restore refused
  - `purge_account_data` completeness and re-run safety
  - `active_memberships`
- **Edge functions:** Deno tests for:
  - `purge-runner.ts`, with injected fakes for Supabase, OneSignal, Apple and storage: ordering, resume after failure, idempotency, stuck logging
  - `apple-auth.ts`: JWT claims and signature against a generated key; exchange and revoke requests
  - `account-status.ts`
- **Api:** `WebApplicationFactory` tests with a new `InMemoryAccountDeletionRepository`, covering:
  - the endpoints and the 409 mapping
  - the gate middleware: 403 shape, allowlist, cache invalidation
  - preview counts
  - log fields
- **App:**
  - view-model tests for the exact-`DELETE` rule and the pending-screen actions
  - source-inspection tests for XAML bindings, route registration, the handler order and that the Apple sign-in path calls the registrar
  - a quickstart device matrix

**Rationale**: the security-critical part of this feature is SQL (RLS and the purge), which the in-memory Api fakes can't exercise. Source-inspecting migrations would only check that strings are present, not behaviour. This is the first database test layer in the repo; see Complexity Tracking in the plan.

**Local note**: `supabase start` needs a container runtime. With podman, point the CLI at podman's socket (`DOCKER_HOST=unix://…/podman.sock`).

## D11 — Sign-out and other devices

**Decision**:
- After a successful request, the app shows the date, then calls `SessionCoordinator.SignOutAsync(SignOutReason.UserInitiated)`, which signs out on this device only.
- Other devices are not signed out remotely. Their next Api call gets the D3 signal and shows the pending screen (US1 scenario 6).

**Alternatives considered**: a global sign-out via the admin API. Rejected because it needs service role at request time (FR-019), and the pending screen is the specified behaviour anyway.

## D12 — Structured logs

**Decision**:
- The Api logs `AccountDeletionRequested`, `AccountDeletionAlreadyPending`, `AccountDeletionRestored`, `AccountDeletionRestoreRefused` and `AccountDeletionGateBlocked` (debug), with `{UserId}`, `{DeletesAt}` and the existing correlation id.
- Edge functions log JSON lines (`account_purge_started`, `account_purge_step_completed`, `account_purge_step_failed` with `step` and `error_code`, `account_purge_completed`, `account_purge_stuck`, `apple_authorization_stored`, `apple_authorization_failed`, `apple_token_unavailable`) with `user_id`.
- No email addresses, names, provider response bodies or tokens are logged (FR-020). Provider failures log the status code only.

## D13 — Retention facts for FR-021

**Decision**: `data-retention.md` in this spec folder states what is deleted and when, and what remains and for how long. It is the source text for the 017 privacy policy. The provider-held items must state actual values taken from the provider dashboards during implementation (task in tasks.md):
- the Supabase backup window (plan-dependent; daily backups on Pro are kept 7 days)
- Supabase and DigitalOcean log retention
- OneSignal's asynchronous deletion

These are documented values to check, not open design questions.

## D14 — Avatar bucket name is shared configuration

**Decision**: `account-purge` reads the bucket from a new `AVATAR_BUCKET_NAME` edge secret, which must equal the Api's `Supabase:AvatarBucketName` for that environment. The quickstart deployment checklist records both values.

**Alternatives considered**: hard-coding `avatars`. Rejected because `docker-compose.yml` defaults to `supabucket`, so the two would silently diverge and leave avatars behind.

## D15 — Cached lists for other users

**Decision**: other users' cached Api lists (`groups:{userId}` and similar, 30 s TTL) are not invalidated across users when someone requests deletion. Hidden items can therefore remain in another user's cached list for up to 30 seconds. Any action on them fails as not found (RLS).

**Rationale**: invalidating every affected member's cache keys would mean enumerating them at request time for a 30-second gain. SC-003's "next refresh" is read as "next refresh after the existing 30-second cache expires", and the spec wording is aligned with that.
