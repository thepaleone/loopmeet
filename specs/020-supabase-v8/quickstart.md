# 020 quickstart: device and staging checks

The unit tests cover the session format and expiry. They can't cover how Postgrest serializes against a real database, or the Gotrue session lifecycle on a device. These checks do.

## Staging Api

Deploy this branch's Api from a `staging/**` tag. Then, with a staging build of the app on Android:

| # | Check | Expected | Result |
| --- | --- | --- | --- |
| 1 | Create a group, rename it, then open it. | Each step succeeds, and the group shows its new name, owner and member count. | |
| 2 | Create a meetup with a place, then edit its time and delete it. | The place, the time in the group's time zone, and the delete all behave as before. | |
| 3 | Invite a second test account, then accept the invitation as that account. | The invitation shows up, and accepting it adds the membership. This uses the `create_membership_from_invitation` RPC. | |
| 4 | Open Profile, change the display name and avatar, then relaunch. | Both persist. | |
| 5 | Request account deletion, then restore. | The scheduled date shows, and restore brings the account back. These are the `request_account_deletion` and `restore_account_deletion` RPCs. | |

## Android session lifecycle

| # | Check | Expected | Result |
| --- | --- | --- | --- |
| 6 | **Update over the old version.** Install a build from `main` before this change and sign in. Install this branch's build over it without uninstalling, then launch. | Home opens signed in, with no login screen. | |
| 7 | After row 6, force-quit and relaunch twice. | Still signed in. The session was rewritten in the new format on the first launch. | |
| 8 | Sign out, then sign in with email, with Google and, on iOS once 013 lands, with Apple. | Each sign-in works and survives a relaunch. | |
| 9 | Sign in, turn on airplane mode, then relaunch. | Home opens from the cached session, with no sign-out (010 FR-011a). Turning the network back on renews the session. | |
| 10 | Leave the app in the background for more than an hour, so the access token expires, then resume it. | The session renews, and data loads without a sign-out. | |
