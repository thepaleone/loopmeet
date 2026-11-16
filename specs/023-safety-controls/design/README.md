# Safety controls: report, leave a group, block from a group (design input)

App Store Review rejected LoopMeet under guideline 1.2: apps with user-generated content need a way to report objectionable content and to block abusive users. Google Play's Child Safety Standards policy also requires in-app reporting. `speckit-prompts.md` starts the feature. This folder is input to the spec, not a decision.

## Can Supabase send the report email?

**Not on its own.** Supabase sends authentication emails only (confirm sign-up, reset password, magic links), through its built-in or custom SMTP. It has no general-purpose "send an email" API. To send a report email you need an email provider called from an Edge Function or from the Api. Resend is Supabase's documented partner; Postmark, SendGrid and Mailgun also work. That adds a new data processor, which the privacy policy, `web/app-privacy.md` and `web/data-safety.md` must list (017/021 "keep in step" rule).

Options:

| Option | How it works | Trade-offs |
|---|---|---|
| **A. In-app form, store and email (recommended)** | The form posts to the Api, which saves the report in a new `abuse_reports` table. An Edge Function sends the report to abuse@loopmeet.io through the email provider. It runs either from a database webhook or as an outbox job, like `notifications-dispatch`. | Works without a mail app, so the user never leaves LoopMeet. Leaves a record you can act on and preserve, which supports the child-safety runbook and NCMEC preservation. Can identify the reported person exactly. Costs a new table (with RLS, retention rules and an account-purge entry), a new processor and an Edge Function. |
| **B. Open the user's mail app** | MAUI's `Email.ComposeAsync` opens a new message to abuse@loopmeet.io with the subject and details prefilled. | Smallest change, with no new processor or table. Needs a configured mail app (common on Android, less so on iPhone), and the user leaves the app. The report is free text, and there's no record unless you keep the email. Google accepts a support email; Apple usually does too. |

The prompts below use **A**, and fall back to **B** when the request can't be sent (offline or a server error), so a report is never lost.

## Picking who is reported

LoopMeet has no usernames. Users have a display name, which isn't unique, and an email, which other members don't see. So the form shouldn't ask the reporter to type a name. Instead:

1. Pick the group, from the reporter's groups.
2. Pick the member, from that group's members, or choose "The group itself (name or meetups)".
3. Choose a reason: inappropriate photo or name, harassment or threats, child safety, spam, other.
4. Add details (required, up to 1,000 characters).

That records exact ids (`reported_user_id`, `group_id`) for whoever handles the report. The entry point is a **Report a concern** row on Profile. A "Report" action on a member in Group Detail is a nice extra that pre-selects the group and member.

## Leave a group / remove and block a member

Neither exists today. There is no leave, remove-member or delete-group code in the Api or the App.

- **Leave group:** any member except the owner can leave from Group Detail, after a confirmation. They stop seeing the group and its meetups and stop getting its notifications at once. Owners can't leave. Handing over ownership and deleting groups are out of scope.
- **Remove and block (owner):** the owner removes a member from Group Detail's member list and can also block them from the group. A blocked user is removed, can't be re-invited (the invitation is refused by the user's id and by their email), and the group no longer appears to them. Their pending invitations to that group are cancelled. The owner can see and lift blocks for the group.
- **Member-side block (decide in clarify):** a member could also "Leave and block", so that group can't invite them again. This protects members from an abusive owner, which is what Apple's "block abusive users" is aimed at. Recommended.

## Constraints from the repo (CLAUDE.md gotchas)

- **Row-level security (RLS):** every new or recreated policy on `groups`, `memberships`, `invitations`, `meetups` and `user_profiles` must keep the `private.account_is_active` / `private.group_is_active` conditions, scoped `to authenticated` (016). New tables need RLS too. Test with pgTAP (`supabase test db`).
- **Notification recipients:** these are read from `active_memberships`, never `memberships`. Removed, blocked and departed members must drop out of that view or out of memberships (016).
- **Meetup RLS:** this deliberately lets all members create, edit and delete meetups, while the UI restricts that to owners (006). Don't "fix" it here.
- **Account deletion and purge (016):** new tables (`abuse_reports`, `group_blocks`) need entries in `account-purge` and in `data-retention.md`. Reports may need to outlive the reporter's or the reported user's deletion when they're kept for legal preservation (see `plan/child-safety/README.md`).
- **Api conventions:** minimal-API endpoints with paired `{Domain}CommandService` / `{Domain}QueryService`, Result-enum statuses (not exceptions), and a 30-second TTL cache in query services. Invalidate the groups cache on leave, remove and block.
- **App conventions:** `sealed partial` ViewModels with `[ObservableProperty]` / `[RelayCommand]` and Async command methods; Shell routes; Profile rows use the 022 full-width row pattern.
- **Logging:** structured logging on report, leave, remove and block, and never the reporter's email or the report text (`LoggingPrivacyTests`).
- **Privacy:** a new processor (the email provider), new data (reports, blocks) and their retention periods must be added together to the privacy policy, `data-retention.md`, `app-privacy.md`, `data-safety.md` and `privacy-policy.test.js`.

## Branching

The work is one spec with three user stories. Each story is implemented on its own branch off the feature branch, as its own small PR (the constitution says one PR per user story). The order is set by what each story needs: leave and remove change memberships, which block builds on. A suggested order:

1. `<NNN>-safety-controls-report`: Report a concern (form, `abuse_reports`, email, fallback).
2. `<NNN>-safety-controls-leave`: Leave group.
3. `<NNN>-safety-controls-block`: Owner remove and block (and member-side block, if chosen).
