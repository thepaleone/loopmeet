# Quickstart: Safety Controls

Manual checks on **staging**, on a physical iPhone (a TestFlight or development build) and an Android phone. The rows marked 🎥 are also the screen-recording script for App Review (guideline 1.2): record them on the iPhone in that order.

## One-time setup

1. **Safety inbox:** `abuse@loopmeet.io` receives mail, and someone reads it.
2. **SendGrid:** `noreply@loopmeet.io` is a verified sender. Create an API key with the **Mail Send** permission only.
3. **Supabase secrets (staging):** set them with `supabase secrets set --project-ref <staging ref>`:
   - `SENDGRID_API_KEY`;
   - `REPORT_EMAIL_FROM=noreply@loopmeet.io`;
   - `REPORT_EMAIL_TO=abuse@loopmeet.io`.

   Then deploy: `supabase functions deploy report-email --project-ref <staging ref>`.
4. **Vault:** check that the `project_url` and `publishable_key` secrets exist (016 set them up for the purge). Without them, the migration skips the trigger call and the sweep job, and logs a notice.
5. **Migrations:** apply them to staging with `supabase db push`.
6. **Api:** deploy it from a `staging/**` tag.
7. **Test accounts:** three, never your own:
   - **Owner** owns the group "Safety QA".
   - **Member A** and **Member B** belong to "Safety QA".
   - **Outsider** belongs to no group.

## US1: Report a concern

| # | Steps | Expected | iPhone | Android |
| --- | --- | --- | --- | --- |
| 1 🎥 | Time it (SC-001: under 1 minute). Member A: Profile → Report a concern. Choose "Safety QA", then Member B, then "Harassment or threats". Type details, then Submit | A confirmation that says the report was sent and may get a reply. Within 5 minutes, `abuse@loopmeet.io` gets "LoopMeet report …: Harassment or threats" with the group, Member B, Member A's name and email, and the details. Reply goes to Member A's email | Pass | Pass |
| 2 🎥 | Time it (SC-001: under 45 seconds, to Submit). Member A: Groups → "Safety QA". Tap Member B's tile, then Report | The form opens with "Safety QA" and Member B already chosen | Pass | Pass |
| 3 | Member A: report "The group itself (name or meetups)" | The email says "Reported: The group itself" | Pass | Pass |
| 4 | Member A: the person list | Member A isn't listed | Pass | Pass |
| 5 | Details: empty, spaces only, then 1,001 characters | Submit stays inactive. The counter shows what's left, and stops input at 1,000 | Pass | Pass |
| 6 | Turn on airplane mode, then submit a report | The text is kept. "Send by email instead" opens the mail app addressed to `abuse@loopmeet.io`, with the same content | Pass | Pass |
| 7 | As row 6, on a device with no mail account configured | The address is shown with Copy, and copying works | Pass | Pass |
| 8 | File 10 reports, then an 11th | The 11th is refused with the "sent 10 reports today" message, and the text is kept | Pass | Pass |
| 9 | Member B: check notifications and the app | Nothing indicates a report | Pass | Pass |
| 10 | Staging, during row 1: in the SQL editor, run `select email_sent_at, email_attempts from abuse_reports order by received_at desc limit 1` | `email_sent_at` is set and `email_attempts` is 1 | Pass | Pass |

## US2: Leave a group

| # | Steps | Expected | iPhone | Android |
| --- | --- | --- | --- | --- |
| 11 🎥 | Member A: "Safety QA" → Leave group → Leave group → confirm | You're back on Groups. "Safety QA" and its meetups are gone from Groups and Home | Pass | Pass |
| 12 | Owner: open "Safety QA" | Member A isn't in the members or the count (after a pull to refresh, within 30 seconds) | Pass | Pass |
| 13 | Owner: create a meetup in "Safety QA" | Member A gets no push | Pass | Pass |
| 14 | Owner: Group Detail | There's no Leave group button | Pass | Pass |
| 15 | Before row 11, Member A creates a meetup for next week | After row 11, the Owner and Member B still see it | Pass | Pass |
| 16 | Owner invites Member A again, and Member A accepts | Member A is back in the group as a new member | Pass | Pass |

## US3: Remove and block

| # | Steps | Expected | iPhone | Android |
| --- | --- | --- | --- | --- |
| 17 🎥 | Owner: tap Member A's tile → Remove from group → confirm | Member A is gone from the list. Member A sees no notification, and the group is gone for them | | |
| 18 🎥 | Owner: tap Member B's tile → Block → confirm | Member B is removed. Blocked people lists Member B, with the date | | |
| 19 🎥 | Owner: invite Member B's email again | Refused: "This person can't be invited to this group" | | |
| 20 | No manual step. Accepting requires the caller's own email, so a blocked person can't reach acceptance through the app. The server-side refusal (`409 invitation_unavailable`) is covered by the Api and pgTAP tests | Automated: `InvitationsWriteEndpointsTests`, `group_blocks.test.sql` | n/a | n/a |
| 21 | Owner: Blocked people → Member B → Unblock → confirm. Invite Member B, and Member B accepts | The invite succeeds, and Member B rejoins as a new member | | |
| 22 🎥 | Member A (re-invited, a member again): Leave group → Leave and block → confirm | Member A leaves. The owner's invitation to Member A gets the same neutral message as row 19 | | |
| 23 | Member A: Profile → Blocked groups | Lists "Safety QA". Unblock lets the owner invite again. The owner's Blocked people never showed Member A | | |
| 24 | Outsider: receive an invitation, then Decline → Decline and block | Declined. A new invitation to the Outsider is refused with the neutral message. "Safety QA" is in the Outsider's Blocked groups | | |
| 25 | Owner: tap your own tile | No Remove or Block. Report isn't offered on your own tile either | | |
| 26 | Member B (not the owner): tap Member A's tile | Report only. No Remove or Block | | |
| 27 | After rows 17 and 18, the Owner creates a meetup | Member A and Member B get no push | | |

## Account deletion and retention

| # | Steps | Expected | Result |
| --- | --- | --- | --- |
| 28 | Member B: delete the account, then force the purge as 016's quickstart does | `select count(*) from group_blocks where blocked_user_id = '<B>'` is 0. Member B's reports, and reports about Member B, are still in `abuse_reports`, with their names | |
| 29 | `select delete_expired_abuse_reports()` after setting one test report's `received_at` to 13 months ago (staging only) | That report is deleted. One with `legal_hold_until` in the future is kept | |
| 30 | Supabase logs for `report-email`, and the Api logs | Events carry ids only. No email address, name or report text | |

## Store follow-up

After US3 merges, update the App Review notes so the report and block lines describe these flows. Then attach the 🎥 recording.
