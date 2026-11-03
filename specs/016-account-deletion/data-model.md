# Data Model: Account Deletion

All changes ship in one migration, `supabase/migrations/<timestamp>_account_deletion.sql`. Existing tables keep their columns; only their policies change.

## New tables

### `account_deletions`

One row per user while a deletion is pending or in progress. **The row's absence means the account is active, or that permanent deletion has finished.**

| Column | Type | Notes |
| --- | --- | --- |
| `user_id` | `uuid` PK | `references auth.users(id) on delete cascade`. The cascade removes the row when the `auth` purge step deletes the user, which is how completion is recorded (research D5). |
| `requested_at` | `timestamptz not null` | Set by `request_account_deletion()` to `now()`. |
| `purge_after` | `timestamptz not null` | `requested_at + interval '30 days'`, set in SQL only (D2). |
| `status` | `text not null` | `pending` or `purging` (check constraint). |
| `completed_steps` | `text[] not null default '{}'` | Subset of `onesignal`, `apple`, `storage`, `database`, `auth`, in order. |
| `attempts` | `int not null default 0` | Purge runs that touched this row. |
| `last_error` | `text null` | Error *code* of the last failed step (for example `onesignal_503`). Never a provider body or PII. |
| `claimed_at` | `timestamptz null` | Purge lease. A row can be claimed when it is null or older than 15 minutes. |
| `updated_at` | `timestamptz not null default now()` | |

**RLS**: enabled.
- One policy, `account_deletions_select_self`: `for select using (user_id = auth.uid())`.
- No insert, update or delete policies. Clients write only through the D2 functions.

**State transitions**:

```text
(no row) --request_account_deletion()--> pending
pending  --request_account_deletion()--> pending            (no change: FR-005)
pending  --restore_account_deletion(), now < purge_after--> (no row)   (FR-012)
pending  --restore_account_deletion(), now >= purge_after--> error deletion_in_progress
pending  --claim_due_account_purges(), now >= purge_after--> purging
purging  --restore_account_deletion()--> error deletion_in_progress
purging  --step fails--> purging (attempts+1, last_error, lease released)
purging  --auth step--> (no row, via cascade)               (complete)
```

### `apple_authorizations`

Apple refresh tokens, kept only so they can be revoked at permanent deletion (D7).

| Column | Type | Notes |
| --- | --- | --- |
| `user_id` | `uuid` | `references auth.users(id) on delete cascade` |
| `client_id` | `text` | The Apple client that issued the token: `io.loopmeet.app` for native, the Services ID for web (017). |
| `refresh_token` | `text not null` | Secret. Never logged and never returned to clients. |
| `created_at` / `updated_at` | `timestamptz` | |
| PK | `(user_id, client_id)` | A newer sign-in replaces the token for that client. |

**RLS**: enabled with **no policies**, so only `service_role` (edge functions) can read or write. Deleted by the `apple` step after revocation, and again by `purge_account_data` as a backstop.

## New SQL functions

The three `*_is_active` helpers live in a `private` schema (`create schema if not exists private; grant usage on schema private to authenticated, service_role;`). PostgREST only exposes `public`, so no client can call them as RPCs to probe whether someone is pending deletion, while policies can still execute them.

| Function | Runs as | Who may execute | Purpose |
| --- | --- | --- | --- |
| `private.account_is_active(p_user_id uuid) → boolean` | definer, `stable` | `authenticated`, `service_role` | `not exists (account_deletions where user_id = p_user_id)` |
| `private.group_is_active(p_group_id uuid) → boolean` | definer, `stable` | `authenticated`, `service_role` | The group exists and `private.account_is_active(owner_user_id)` |
| `private.email_is_active(p_email text) → boolean` | definer, `stable` | `authenticated`, `service_role` | No pending user has this auth email (case-insensitive) |
| `request_account_deletion() → account_deletions` | definer | `authenticated` | Inserts `(auth.uid(), now(), now() + 30 days, 'pending')` on conflict do nothing, and returns the row. Raises if `auth.uid()` is null. |
| `restore_account_deletion() → boolean` | definer | `authenticated` | Deletes the caller's row if `status = 'pending' and purge_after > now()` and returns `true`. If a row exists otherwise, raises `deletion_in_progress`. With no row it returns `false`, so repeat calls are idempotent. |
| `claim_due_account_purges(p_limit int) → setof account_deletions` | definer | `service_role` only | Due rows with a free lease: sets `status = 'purging'`, `claimed_at = now()`, `attempts + 1`. Uses `for update skip locked`. |
| `record_account_purge_step(p_user_id uuid, p_step text, p_error text) → void` | definer | `service_role` only | Appends the step on success, or sets `last_error` and clears `claimed_at` on failure. |
| `purge_account_data(p_user_id uuid) → void` | definer | `service_role` only | The `database` step (D5), in one transaction. Idempotent. |

`execute` is revoked from `public` and `anon` on all of these, and from `authenticated` on the last three.

### `purge_account_data(p_user_id)` deletes, in this order

1. It looks up `v_email` from `auth.users` (it may be null on a re-run).
2. `notification_open_events where user_id = p_user_id`.
3. `notification_delivery_attempts where user_id = p_user_id`.
4. `user_devices where user_id = p_user_id`.
5. `invitations where invited_by_user_id = p_user_id or invited_user_id = p_user_id or (v_email is not null and lower(invited_email) = lower(v_email))`.
6. `meetups where created_by_user_id = p_user_id`.
7. `groups where owner_user_id = p_user_id`. This cascades to those groups' memberships, invitations and meetups.
8. `memberships where member_user_id = p_user_id`.
9. `apple_authorizations where user_id = p_user_id`.
10. `user_profiles where id = p_user_id`.

`notification_events` rows aren't keyed by user, and they only reference group or meetup ids, so they are left alone. `notifications-dispatch` already ignores `DELETE` webhook events for meetups (012 D7), so the cascades don't send pushes. A pgTAP test asserts this.

## New view

`active_memberships` (`security_invoker = false`). `select`, granted to `service_role` only:

```sql
select m.* from memberships m
join groups g on g.id = m.group_id
where private.account_is_active(m.member_user_id) and private.account_is_active(g.owner_user_id)
```

## Policy changes (existing tables)

Every policy is dropped and recreated with its existing predicate kept intact, **AND** the additions below.

| Table | Policy | Added condition |
| --- | --- | --- |
| `groups` | select | `private.account_is_active(owner_user_id)` |
| `groups` | insert | `private.account_is_active(auth.uid())` |
| `groups` | update | `private.account_is_active(owner_user_id)` |
| `memberships` | select | `private.group_is_active(group_id) and private.account_is_active(member_user_id)` |
| `memberships` | insert | `private.account_is_active(auth.uid())` (except the existing `service_role` branch) |
| `invitations` | select | `private.group_is_active(group_id) and private.account_is_active(invited_by_user_id) and (invited_user_id is null or private.account_is_active(invited_user_id)) and private.email_is_active(invited_email)` |
| `invitations` | insert | `private.account_is_active(auth.uid())` |
| `invitations` | update | same as select |
| `meetups` | select, update, delete | `private.group_is_active(group_id) and private.account_is_active(created_by_user_id)` |
| `meetups` | insert | `private.account_is_active(auth.uid())` |
| `user_profiles` | select | `private.account_is_active(id)` |
| `user_profiles` | insert, update | `private.account_is_active(auth.uid())` |

The helper functions `is_group_owner`, `is_group_member`, `is_group_invited_recipient` and `can_insert_membership` are unchanged. They're always combined with the new conditions in the policies above.

## Scheduling

`cron.schedule('account-purge-hourly', '7 * * * *', net.http_post(.../functions/v1/account-purge ...))` uses the same vault secrets as `reminders-scheduler`. Minute 7 keeps it clear of the 5-minute reminder runs.

## C# model (Core)

```csharp
namespace LoopMeet.Core.Models;

public sealed record AccountDeletion(Guid UserId, DateTimeOffset RequestedAt, DateTimeOffset DeletesAt, AccountDeletionStatus Status);

public enum AccountDeletionStatus { Pending, Purging }
```

`LoopMeet.Core.Interfaces.IAccountDeletionRepository`:

```csharp
Task<AccountDeletion?> GetAsync(Guid userId, CancellationToken ct);          // caller's own row via RLS
Task<AccountDeletion> RequestAsync(CancellationToken ct);                    // request_account_deletion()
Task<AccountRestoreResult> RestoreAsync(CancellationToken ct);               // restore_account_deletion()
```

`AccountRestoreResult` is `Restored`, `NotPending` or `DeletionInProgress`. `DeletionInProgress` maps from the SQL exception.

`LoopMeet.Core.Accounts.AccountDeletionPolicy.GracePeriod = TimeSpan.FromDays(30)` is used only for the preview's projected date. The authoritative value is in SQL (D2 duplication note).
