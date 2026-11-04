# Data Retention: Account Deletion

**Feature**: 016-account-deletion (FR-021) · **Status**: provider values confirmed from the dashboards (2026-11-04) · **Used by**: 017 privacy policy

This is what LoopMeet does with a user's data when they delete their account, and what is left afterwards. The 017 privacy policy is written from this document, so keep the two in step.

## Requesting deletion

- A user deletes their account in the app: Profile → Delete Account, then type `DELETE`. The 017 website will offer the same request through the same Api.
- Permanent deletion is scheduled for **30 days** after the request. The date is shown on screen when the user confirms. The value is set in the database (`request_account_deletion`) and mirrored by `AccountDeletionPolicy.GracePeriod` for display. Tests pin both.
- The user is signed out on the device where they asked.

## During the 30 days: hidden, not yet deleted

From the moment of the request, everyone else sees LoopMeet as if the account were already gone. This is enforced in the database (row-level security), not only in the app:

- the user's profile (name, email, phone, photo)
- groups the user owns, with all their members, meetups and invitations
- the user's memberships in other groups (they drop out of member lists and counts)
- meetups the user created in other groups
- invitations the user sent, and invitations addressed to the user by id or email
- no push notifications or reminders are sent to the user, or about anything they own

The data itself is unchanged during this time. If the user signs in, the only screen available says when the account will be deleted, with **Restore account** and **Sign out**:

- **Restore account**, available until the deletion date, cancels the deletion. Everything comes back exactly as it was, for the user and everyone else.
- Signing in, signing out, or the app refreshing its session in the background never cancels the deletion.

## Permanent deletion

An automatic job runs every hour (at minute 7). It deletes every account whose 30 days have passed, **within 1 hour of the date, and in any case within 24 hours**. A step that fails is retried on each hourly run, and an alert is logged after 24 failed attempts.

The steps, in order:

| Step | What is deleted |
| --- | --- |
| Push notifications | The user's OneSignal user, with all its subscriptions and aliases (keyed by the user id) |
| Sign in with Apple | Every Apple refresh token stored for the user is revoked with Apple, then deleted. This removes LoopMeet from the user's Apple ID "Sign in with Apple" list. |
| Photos | Every file in the user's avatar folder, including photos the user replaced earlier. Replaced photos are kept until the account is deleted. |
| Database | Groups the user owns, with their meetups, memberships and invitations; the user's memberships in other groups; meetups they created; invitations they sent or received (by id or email); their devices, notification delivery records and notification open records; stored Apple tokens; their profile |
| Account | The sign-in account itself (email, password hash and linked Google or Apple identities) |

Deleting the sign-in account also deletes the deletion record, so **no completion marker is kept**. The user's email is free to sign up again straight away, and creates a new, empty account.

Notification event records (`notification_events`) are kept. They hold only a notification type, ids of the group or meetup it was about, and timestamps. Once the purge has run, those ids point at nothing, so they don't identify anyone.

## What remains, and for how long

LoopMeet keeps no copy of a deleted user's data. Some copies held by providers can't be removed selectively, and expire on the provider's own schedule:

| Where | What it may contain | Kept for |
| --- | --- | --- |
| Supabase database backups | A full copy of the database as it was at backup time | 7 days. |
| Supabase logs (API, Auth, Postgres, Edge Functions) | Request metadata such as user ids and IP addresses. Our edge functions log user ids, never emails or tokens. | 7 days. |
| DigitalOcean App Platform logs (Api) | Request logs and the Api's structured logs, keyed by user id, never email | Not stored. |
| OneSignal | The deleted OneSignal user, until OneSignal finishes processing the deletion | OneSignal deletes users asynchronously after accepting the request. |
| Apple | Apple's own record that the user used Sign in with Apple with LoopMeet | Governed by Apple's privacy policy. LoopMeet revokes its access. |

**Api logs and token errors:** the Api sets `IdentityModelEventSource.ShowPII` only in Development, so in staging and production, token validation errors don't log token details, which can include an email. `LoggingPrivacyTests` keeps it that way.

There is no legal requirement to keep any LoopMeet user data after deletion. If one arises, add it here explicitly.

## Logging

Every request, restore, refused restore and permanent-deletion step is logged by user id only, never by email:
- Api: `AccountDeletionRequested`, `AccountDeletionAlreadyPending`, `AccountDeletionRestored`, `AccountDeletionRestoreRefused`
- `account-purge`: `account_purge_started`, `account_purge_step_completed`, `account_purge_step_failed`, `account_purge_completed`, `account_purge_stuck`, `apple_token_unavailable`
