# Research: Safety Controls (Report, Leave, Block)

Every open point from the plan's Technical Context is settled here. Facts about the code were read from the repo on 2026-11-16.

## D1. Writes go through SQL functions called by the Api as the caller

**Decision:** every new write is a `security definer` SQL function. The Api calls it with `Client.Rpc`, through its per-request client that impersonates the caller. The functions are:

- report: `submit_abuse_report`;
- leave: `leave_group`, and `leave_group_and_block` in US3;
- remove and block: `remove_group_member`;
- lift a block: `lift_group_block`;
- decline and block: `decline_invitation_and_block`.

Each one checks `auth.uid()` and `private.account_is_active(auth.uid())` itself, and raises a coded exception for each refusal, such as `report_limit_reached`. The repository maps those codes to Core result values, as `AccountDeletionRepository.RestoreAsync` maps `deletion_in_progress`.

**Rationale:**

- **No delete policy exists.** `memberships` has no delete policy at all. Writing one would let a member delete any row the policy matches. A function can express "only your own row, unless you own the group, and never the owner's row" exactly, and test it in pgTAP.
- **Inserts already go through functions.** `create_membership_from_invitation` (20260311) exists because direct inserts through the .NET client didn't see `auth.uid()`. 016 followed the same route for `request_account_deletion()`.
- **No privileged client.** The Api keeps its caller-impersonating client, so `PrivilegedAccessTests` (FR-019 of 016) is unchanged.
- **The server enforces the rules (FR-020).** Rate limit, self-report, owner checks and block checks run in one transaction, so they can't be bypassed by calling PostgREST directly.

**Alternatives considered:**

- *RLS delete and insert policies plus checks in the Api:* the rate limit and the "not the owner" rule would live only in the Api. That's bypassable through PostgREST with the same token.
- *A service-role client in the Api:* forbidden (CLAUDE.md, `PrivilegedAccessTests`).

## D2. Leaving, removal and blocking delete the membership row

**Decision:** leave and remove delete the `memberships` row. There is no `left_at` flag.

**Rationale:**

- **Notifications stop on their own.** `active_memberships` is `memberships` joined to active accounts. `notifications-dispatch` and `reminders-scheduler` read only that view (016 gotcha). With the row gone, the person drops out of every recipient list, so the view needs no change, and SC-004 holds by construction.
- **Rejoining works.** It is a new invitation and a new row (US2 scenario 7). `create_membership_from_invitation` uses `on conflict do nothing`, which is safe either way.
- **Their meetups stay (FR-012).** `meetups_select_group_member` checks the *viewer's* membership and the creator's account status, not the creator's membership. The 006 rule, that every member can edit, is unchanged.
- **Losing the history is acceptable.** No feature reads past memberships, and keeping them would be new stored data with no use.

**Alternative considered:** *a soft-delete column:* every policy and view on memberships would need a new condition, and the 016 policies would need recreating. That means more change and more risk, for history nobody reads.

## D3. Report storage outlives both accounts

**Decision:** `abuse_reports` stores ids without foreign keys to `auth.users` or `groups`, plus three snapshots taken when the report is filed:

- the group name;
- the reporter's display name;
- the reported person's display name.

The reporter's email is **not** stored. The email function reads it from `user_profiles` when it sends.

**Rationale:**

- **The report must survive deletion.** FR-023 and FR-024 keep a report for 1 year whatever happens to either account or the group. A foreign key with `on delete cascade` would erase it when the purge deletes the auth user or the group. With no foreign key, the ids simply point at nothing afterwards, as `notification_events` ids already do (data-retention.md).
- **The report must stay readable.** Without the snapshots, a preserved report would only hold dead ids once the purge runs. The child-safety runbook (later feature) needs to say who and what was reported.
- **Less is stored.** Not storing the email keeps it out of the table, so it isn't retained for a year. The email already in the safety inbox is enough to reply.

**Alternative considered:** *`on delete set null` foreign keys:* this loses who was reported, which is exactly what legal preservation needs.

## D4. Report email delivery: trigger plus sweep, both in a migration

**Decision:**

- **Trigger:** an `after insert` trigger on `abuse_reports` calls `net.http_post` to the new `report-email` Edge Function with `{ "report_id": … }`. It reads `project_url` and `publishable_key` from Vault, as the purge cron does.
- **Sweep:** a `pg_cron` job, `report-email-sweep` at `*/5 * * * *`, calls the same function with `{}`.
- **Fresh databases:** a database with no Vault secrets (local, CI) skips both, with a notice, exactly as `account_deletion_purge.sql` does.
- **Claiming:** the function asks `claim_report_emails(p_report_id, p_limit)` for work. That function marks rows claimed, with a 10-minute lease, so the trigger call and a sweep never send the same report twice.
- **Marking:** after each send, the function reports the outcome with `mark_report_email(p_report_id, p_error)`.

**Rationale:**

- **Clarification (b).** The trigger sends at once. The sweep retries anything that failed and raises the alert (FR-005).
- **Reproducible.** It lives in a migration rather than in the dashboard, where the 008 notification webhooks live. So it is reproducible, reviewed, and checked by pgTAP: the trigger exists, and the claim is exclusive.
- **Safe to expose.** `verify_jwt = false`, like `account-purge`: the function only ever sends existing, undelivered reports to a fixed address. Calling it adds nothing and leaks nothing.

**Alternatives considered:**

- *A dashboard Database Webhook (as 008 did):* not in version control, and it can't be tested.
- *Sending from the Api:* the Api would need the SendGrid key and a retry loop, and a crash between saving and sending would lose the email until the sweep came anyway.

## D5. SendGrid

**Decision:**

- **API:** call the SendGrid v3 `POST https://api.sendgrid.com/v3/mail/send` endpoint with `fetch`. Add no SDK.
- **Key:** `SENDGRID_API_KEY` is a Supabase secret.
- **Addresses:** from `noreply@loopmeet.io` (verified, clarification), to `abuse@loopmeet.io`. `reply_to` is the reporter's current `user_profiles.email`, or is omitted if the profile is gone.
- **Content:** plain text only.
- **Subject:** `LoopMeet report <short id>: <reason label>`. It contains no user-written text.

**Rationale:**

- **Small surface.** One HTTPS call is the whole integration. An SDK would add an npm dependency to an Edge Function for one request.
- **Contained user text.** Plain text avoids HTML injection from report details. User text stays in the body, so a hostile group name can't be used to spoof the subject line in the safety inbox.
- **Non-2xx means failure.** A non-2xx response is recorded as a failure, and the sweep retries it. SendGrid returns 202 on acceptance.

**Alternative considered:** *Resend:* the design input's default, replaced by the maintainer's choice in clarification (a).

## D6. Rate limit

**Decision:** `submit_abuse_report` takes `pg_advisory_xact_lock` on the reporter's id. It then counts the reporter's reports with `received_at > now() - interval '24 hours'`, and raises `report_limit_reached` at 10.

**Rationale:**

- **The database enforces it.** That makes it hold however the function is called.
- **No double-submit past the limit.** The lock serialises one reporter's concurrent submissions, so two at once can't both pass the count.
- **The count is cheap.** An index on `(reporter_user_id, received_at)` keeps it fast.

## D7. Blocks: one table, set by the owner or by the person themselves

**Decision:** `group_blocks` has these columns:

- `group_id`, a foreign key to `groups` with `on delete cascade`;
- `blocked_user_id`;
- `blocked_email`, stored lower-cased;
- `set_by`: `owner` or `self`;
- `created_at`.

It has `unique (group_id, blocked_user_id)`. The RLS select policies are:

- the owner sees rows where `set_by = 'owner'`;
- the blocked person sees rows where `set_by = 'self'`.

Both policies keep the 016 `private.group_is_active` and `private.account_is_active` conditions, scoped to `authenticated`. There are no insert, update or delete policies: the D1 functions write.

**Rationale:**

- **Clarification (g):** each side lifts only its own blocks. RLS also hides the other side's blocks, so an owner can't see that a member blocked the group, which FR-016 requires.
- **Both identifiers are matched.** Matching on both the id and the email at the time of the block covers the "blocked email changes" edge case.
- **Unique per group and person.** A second block on the same person (for example, the owner blocks someone who had already used Leave and block) keeps the existing row, and the existing `set_by` wins. A person's own self-block isn't converted into one the owner can lift.

## D8. Refusing blocked invitations, and refusing acceptance

**Decision:**

- **Owner-side check:** `invitation_is_blocked(p_group_id, p_email)` is a `security definer` function that returns a boolean. It answers only when the caller owns the group, and raises otherwise. It matches:
  - the email against `blocked_email`;
  - the email's account, found through `user_profiles.email`, against `blocked_user_id`.

  `InvitationCommandService.CreateAsync` calls it after the owner check, and returns the new `InvitationCommandStatus.Blocked`. The endpoint turns that into `409 invitee_blocked`, "This person can't be invited to this group". The message is the same whoever set the block (FR-015).
- **Defense in depth:** `invitations_insert_owner` is recreated with an added `not private.group_block_matches(group_id, invited_email, invited_user_id)`, keeping its 016 conditions, `to authenticated`.
- **Acceptance:** `AcceptAsync` calls `caller_is_blocked_from_group(p_group_id)` before changing the invitation. It returns `InvitationCommandStatus.Blocked` as `409 invitation_unavailable`, "This invitation is no longer available", and marks the invitation `cancelled`. `create_membership_from_invitation` is recreated with the same check, so the RPC path can't bypass it.

**Rationale:**

- **The policy alone isn't enough.** It would refuse the insert, but PostgREST reports a policy failure as a generic error that the Api can't tell apart from other failures. The explicit check gives the exact status, and the policy keeps direct PostgREST calls honest.
- **Blocks stay hidden from the owner.** The owner can't read member-set blocks, so the check has to be a `security definer` function that returns only true or false.

## D9. Cache invalidation

**Decision:** leave, remove and block invalidate:

- `group-detail:{groupId}`;
- `groups:{userId}` for the person who left and for every remaining member, because member counts show in each member's groups list;
- `MeetupQueryService.HomeCacheKey` for the person who left;
- `meetups:{groupId}`.

Block and lift also invalidate the new `group-blocks:{groupId}` and `my-blocks:{userId}`.

**Rationale:** the 30-second TTL pattern (CLAUDE.md) bounds staleness for everyone else. Explicit invalidation makes the leaver's own view change at once (US2 scenario 3). `MeetupCommandService` already invalidates per member in the same way.

## D10. Account deletion and retention

**Decision:**

- **Purge:** `purge_account_data(p_user_id)` is recreated with one more statement. It deletes `group_blocks` where `blocked_user_id = p_user_id`, or where `blocked_email` is the user's profile email, before the profile row goes. Blocks on groups the user owns cascade with the group.
- **The `account-purge` Edge Function doesn't change.** Its database step already calls `purge_account_data`, so the new table needs no new purge step. The prompt asked to "extend the account-purge Edge Function", and this meets that need through the function it already calls.
- **Reports aren't touched by the purge.** They expire through a daily `pg_cron` job, `abuse-reports-retention` at `17 3 * * *`, which deletes reports older than 1 year unless `legal_hold_until` is in the future. The job is pure SQL, so it needs no Vault secrets and is scheduled everywhere.
- **Legal hold:** `legal_hold_until` is set by hand under the later child-safety runbook. This feature adds the column and honours it.

**Rationale:** FR-023 and FR-024 as clarified (e). A pure SQL job is the simplest scheduler, with no HTTP hop.

## D11. The App's testable rules

**Decision:** the report form's rules live in MAUI-free static classes in `Features/Safety/`, linked into `LoopMeet.App.Tests` like 022's `ProfileText`:

| Class | What it holds |
| --- | --- |
| `ReportReasons` | the five reasons, with their labels and Api codes |
| `ReportDraft` | `CanSubmit`, the remaining characters and the trimmed details |
| `ReportFallbackMail` | the subject and body for the mail-app fallback |
| `SafetyErrorText` | Api error code to message |

The ViewModels call these, and are checked by source inspection.

**Rationale:**

- **The ViewModels can't be built in tests.** `LoopMeet.App.Tests` is plain `net10.0` and can't build ViewModels that call `Shell`, `Email` or `Clipboard` (022 gotcha).
- **Every rule a user notices is pure and unit-testable:** the fallback content, the limit, and which error shows. That is the "ViewModel tests including the fallback path" the prompt asks for, in the form this repo can run.

**Alternative considered:** *wrapping `Email` and `Clipboard` in interfaces to construct the ViewModel:* that adds abstractions with one user each (Principle IV), and still leaves `Shell`.

## D12. Mail-app fallback

**Decision:** when submitting fails (`HttpRequestException`, a timeout or a 5xx), the form keeps its fields and offers "Send by email instead":

- If `Email.Default.IsComposeSupported`, it calls `Email.Default.ComposeAsync`, addressed to `abuse@loopmeet.io`, with `ReportFallbackMail`'s subject and body: the group name, the person's display name, the reason, the details and the app version. It never includes ids the user can't see.
- Otherwise, it shows an alert with the address and a Copy button, which uses `Clipboard.Default.SetTextAsync`.

A 4xx refusal shows its message instead, because resending the same content by email wouldn't change a self-report or non-member refusal. The exception is `429`, whose message already points to the address (FR-006).

## D13. Where the controls go in the App

| Control | Place | Pattern |
| --- | --- | --- |
| Report a concern | Profile, a new row in the Account card | 022 settings row |
| Blocked groups | Profile, a row under Report a concern | 022 settings row, opening `BlockedGroupsPage` |
| Report, Remove from group, Block | Group Detail: tapping a member tile opens an action sheet | `DisplayActionSheet`, then a confirmation for Remove and Block |
| Leave group, Leave and block | Group Detail, a `Leave group` button for non-owners, below the members | an action sheet with both choices, then a confirmation |
| Blocked people (owner) | Group Detail, a `Blocked people` button for the owner, next to Edit Group | `GroupBlocksPage` |
| Decline and block | Pending Invitations: Decline opens an action sheet, Decline or Decline and block | the existing command flow |

**Rationale:**

- **Small changes.** Action sheets reuse what the platform already offers, and add no new controls.
- **Accessible.** Member tiles get `SemanticProperties.Description` ("Actions for {name}"), because they become tappable (FR-025).

## D14. Logging

**Decision:**

- **Api events:** `ReportSubmitted` and `ReportRefused`, with the report id, group id, reason code and status. Also `GroupLeft`, `GroupMemberRemoved`, `GroupMemberBlocked`, `GroupBlockLifted` and `InvitationDeclinedAndBlocked`. They are keyed by user and group ids only.
- **`report-email` events:** `report_email_sent`, `report_email_failed`, with `report_id` and an error code, and `report_email_stuck` when a report is still undelivered 24 hours after it was received. Each stuck report is logged once, recorded by `email_alerted_at`.
- **New test:** `LoggingPrivacyTests` gains a source-inspection test. No log template in the new services, endpoints or the Edge Function may contain `Email`, `Details` or `ReplyTo`.

**Rationale:** Principle VII and FR-021. The 016 Api logs never hold an email (data-retention.md).

## D15. Privacy documents

**Decision:** each story's pull request changes all of these together, for its own data. US1 covers reports and SendGrid. US3 adds blocks, so the published policy never describes a control that isn't shipped yet.

- **`data-retention.md`:** a new "Reports and blocks" section with the periods in D10 (the blocks half in US3). SendGrid is added to the providers table: it holds report emails on its own retention schedule.
- **Privacy policy:**
  - section 2 gains "Reports" in US1, renamed "Reports and blocks" in US3;
  - the processors table gains SendGrid;
  - section 6 says reports are kept for 1 year, or longer when the law requires.
- **`privacy-policy.test.js`:** checks SendGrid and the 1-year period (US1), and the block retention (US3).
- **`data-safety.md` (Play):** the "Other user-generated content" row adds the purpose "Fraud prevention, security, and compliance".
- **`app-privacy.md` (App Store):** "Other User Content" maps to the new policy section. Its purposes stay "App Functionality", which Apple defines to include implementing security measures.

Neither store form counts SendGrid as "sharing", because it is a service provider acting for us, as Supabase and OneSignal are today.

**Rationale:**

- **Update together.** The 017 and 021 gotchas say these change together.
- **Only Play's purpose changes.** Play's purpose list has a specific purpose for moderation, while Apple's doesn't.
