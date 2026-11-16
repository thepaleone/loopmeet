# Data Model: Safety Controls

One new migration per user story, so each pull request stands alone (US2 needs no new table):

| Story | Migration | Adds |
| --- | --- | --- |
| US1 | `<ts>_abuse_reports.sql` | the `abuse_reports` table, report functions, the email trigger, and the sweep and retention cron jobs |
| US2 | `<ts>_leave_group.sql` | `leave_group` |
| US3 | `<ts>_group_blocks.sql` | the `group_blocks` table, block functions, the recreated `invitations_insert_owner`, `create_membership_from_invitation` and `purge_account_data` |

Existing tables are unchanged in shape. `memberships` rows are deleted on leave, remove and block (research D2). `invitations.status` gains the value `cancelled`. The column is `varchar` with no check constraint, and no webhook fires on invitation updates.

## abuse_reports (US1)

| Column | Type | Rules |
| --- | --- | --- |
| `id` | `uuid` primary key | `gen_random_uuid()` |
| `reporter_user_id` | `uuid not null` | the caller. **No foreign key**: the report outlives the account (D3) |
| `group_id` | `uuid not null` | a group the reporter is a member of when filing. No foreign key |
| `reported_user_id` | `uuid null` | null means "the group itself". Otherwise a member of `group_id`, not the reporter. No foreign key |
| `reason` | `text not null` | `check (reason in ('inappropriate_photo_or_name','harassment_or_threats','child_safety','spam','other'))` |
| `details` | `text not null` | `check (char_length(btrim(details)) between 1 and 1000)`. Stored trimmed |
| `group_name` | `text not null` | snapshot at filing |
| `reporter_display_name` | `text not null` | snapshot at filing |
| `reported_display_name` | `text null` | snapshot at filing. Null when `reported_user_id` is null |
| `received_at` | `timestamptz not null` | `now()`, the server's time |
| `email_sent_at` | `timestamptz null` | set when SendGrid accepts the email |
| `email_attempts` | `int not null default 0` | |
| `email_last_error` | `text null` | an error code only, never the email content |
| `email_claimed_at` | `timestamptz null` | a 10-minute send lease (D4) |
| `email_alerted_at` | `timestamptz null` | set once when `report_email_stuck` is logged |
| `legal_hold_until` | `timestamptz null` | set by an operator under the child-safety runbook. It postpones the retention delete |

**Indexes:**

- `(reporter_user_id, received_at)`, for the rate limit;
- a partial index on `(received_at) where email_sent_at is null`, for the sweep.

**RLS:** enabled, with no policies for `anon` or `authenticated`. Users never read reports back, and they write only through `submit_abuse_report`. `service_role` (the Edge Function) bypasses RLS.

**Lifecycle:**

```text
filed ──trigger/sweep──▶ claimed ──202──▶ sent
                           │ └─error──▶ unsent (attempts+1) ──sweep──▶ claimed …
                           └─ unsent 24 h after received ──▶ alert logged once
any state ──received_at + 1 year, no active legal hold──▶ deleted (daily job)
```

## group_blocks (US3)

| Column | Type | Rules |
| --- | --- | --- |
| `group_id` | `uuid not null` | references `groups(id) on delete cascade` |
| `blocked_user_id` | `uuid not null` | the blocked person's account |
| `blocked_email` | `text not null` | lower-cased account email when blocked |
| `set_by` | `text not null` | `check (set_by in ('owner','self'))` |
| `created_at` | `timestamptz not null default now()` | |

**Keys and indexes:**

- primary key `(group_id, blocked_user_id)`;
- index on `(group_id, blocked_email)`, for invitation checks;
- index on `(blocked_user_id)`, for "my blocks" and the purge.

**RLS (select only), every policy `to authenticated` and keeping the 016 conditions:**

- `group_blocks_select_owner`: the caller owns the group, `set_by = 'owner'`, `private.group_is_active(group_id)` and `private.account_is_active(blocked_user_id)`.
- `group_blocks_select_self`: `blocked_user_id = auth.uid()`, `set_by = 'self'` and `private.group_is_active(group_id)`.

**State:**

- A row exists while the block is in force. `lift_group_block` deletes it.
- The group's deletion cascades to it.
- `purge_account_data` deletes it when either account is purged: the blocked person by id or email, and the owner through the group cascade.

## Functions

All functions are `security definer set search_path = public`. Each raises `account_inactive` when `private.account_is_active(auth.uid())` is false. Error codes are raised as the exception message, matched by the repositories (D1).

| Function | Story | Who may call | Does | Raises |
| --- | --- | --- | --- | --- |
| `submit_abuse_report(p_group_id, p_reported_user_id, p_reason, p_details)` returns `(id, received_at)` | US1 | `authenticated` | validates, snapshots names, inserts | `not_group_member`, `invalid_reported_user` (self, or not a member of the group), `invalid_report` (reason or details), `report_limit_reached` |
| `claim_report_emails(p_report_id uuid null, p_limit int)` returns rows | US1 | `service_role` | claims unsent, unleased rows (one, or the oldest `p_limit`), increments attempts | |
| `mark_report_email(p_report_id, p_error text null)` | US1 | `service_role` | sets `email_sent_at`, or records the error and clears the lease | |
| `stuck_report_emails()` returns rows | US1 | `service_role` | rows unsent 24 hours after `received_at` with `email_alerted_at` null, and sets `email_alerted_at` | |
| `delete_expired_abuse_reports()` returns `int` | US1 | `pg_cron` only | deletes rows where `received_at < now() - interval '1 year'` and `legal_hold_until` is null or past | |
| `leave_group(p_group_id)` | US2 | `authenticated` | deletes the caller's membership | `not_group_member`, `owner_cannot_leave` |
| `leave_group_and_block(p_group_id)` | US3 | `authenticated` | `leave_group`, then inserts a `self` block, in one transaction | as `leave_group` |
| `remove_group_member(p_group_id, p_member_user_id, p_block boolean)` | US3 | `authenticated` (owner) | deletes the membership. With `p_block`: inserts an `owner` block (keeping an existing row) and sets the group's pending invitations to that person, by id or email, to `cancelled`. Without `p_block`, a member already gone is not an error (race edge case). With `p_block`, the person must still be a member | `not_group_owner`, `cannot_remove_owner`, `not_group_member` (block only) |
| `lift_group_block(p_group_id, p_blocked_user_id uuid null)` | US3 | `authenticated` | owner: deletes an `owner` block on `p_blocked_user_id`. Self (null id): deletes the caller's `self` block | `not_group_owner`, `block_not_found` |
| `decline_invitation_and_block(p_invitation_id)` | US3 | `authenticated` (the invitee) | declines a pending invitation addressed to the caller's id or email, and inserts a `self` block | `invitation_not_found` |
| `invitation_is_blocked(p_group_id, p_email)` returns `boolean` | US3 | `authenticated` (owner) | true if the email, or the account using that email, is blocked by either side | `not_group_owner` |
| `caller_is_blocked_from_group(p_group_id)` returns `boolean` | US3 | `authenticated` | true if the caller's id or email is blocked | |
| `private.group_block_matches(p_group_id, p_email, p_user_id)` | US3 | used inside policies and functions | the shared matcher | |

**Recreated in US3, each keeping its 016 conditions:**

- `invitations_insert_owner`: adds `and not private.group_block_matches(group_id, invited_email, invited_user_id)`.
- `create_membership_from_invitation`: raises `blocked_from_group` when the matcher is true.
- `purge_account_data`: deletes the user's `group_blocks` rows before the profile row.

## Core models (C#)

| Model | Fields |
| --- | --- |
| `AbuseReport` | `Id`, `GroupId`, `ReportedUserId?`, `Reason` (`ReportReason` enum), `Details`, `ReceivedAt` |
| `ReportReason` | `InappropriatePhotoOrName`, `HarassmentOrThreats`, `ChildSafety`, `Spam`, `Other`, with an explicit snake_case code map |
| `GroupBlock` | `GroupId`, `BlockedUserId`, `DisplayName` (resolved at read), `GroupName` (for "my blocks"), `CreatedAt` |
| `ReportSubmission` (result) | `Submitted(AbuseReport)`, `NotGroupMember`, `InvalidReportedUser`, `Invalid`, `LimitReached` |
| `MembershipExitResult` | `Done`, `NotGroupMember`, `OwnerCannotLeave`, `NotGroupOwner`, `CannotRemoveOwner` |

**Validator:** `AbuseReportValidator` (FluentValidation, Core) checks:

- `Reason` is defined;
- `Details` trimmed is 1–1,000 characters;
- `ReportedUserId`, when present, isn't `Guid.Empty`.

Leave and block take only route ids, so they need no validator (Principle IV).

## What is not stored

- The reporter's email in `abuse_reports`. It's read when the email is sent (D3).
- Report text in logs (D14).
- Membership history after leaving (D2).
