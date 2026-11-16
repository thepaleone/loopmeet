# Spec Kit prompts: safety controls (report, leave, block)

Run these from the repo root on a clean `main`. Each block is the text to paste after the command. Design input lives in `plan/safety-controls/` (see its README). Before starting, set up abuse@loopmeet.io and an account with the email provider (Resend unless you choose another), with loopmeet.io verified as a sending domain.

---

## 1. `/speckit.specify`

```
Safety controls for LoopMeet, needed to pass App Store guideline 1.2 (report objectionable content, block abusive users) and Google Play's in-app reporting requirement. Before writing the spec, read plan/safety-controls/README.md and plan/child-safety/README.md. Then, as the first step, move plan/safety-controls/ into the new feature directory as design/. Leave plan/child-safety/ where it is, because a later feature uses it. Write three independently deliverable user stories:

US1 - Report a concern (P1). A "Report a concern" row on Profile opens a report form:
- the group, chosen from the reporter's groups;
- the person, chosen from that group's members, or "the group itself";
- a reason: inappropriate photo or name, harassment or threats, child safety, spam, other;
- details: required text, up to 1,000 characters.
Optionally, a Report action on a member in Group Detail opens the form with the group and person pre-selected. Submitting saves the report through the Api, with exact ids for reporter, reported user and group, and a server timestamp, and sends it by email to abuse@loopmeet.io. Supabase can't send general email, so this goes through an email provider called from an Edge Function or the Api. The reporter sees a confirmation that says what happens next. If the report can't be submitted, the form offers to open the device's mail app with the same content addressed to abuse@loopmeet.io. If no mail app exists, it shows the address to copy. Nothing typed is lost. A user can't report themselves. Rate-limit reports per user to stop flooding.

US2 - Leave a group (P2). Any member except the owner can leave a group from Group Detail after a confirmation. Immediately they no longer see the group or its meetups, no longer count in its member lists, and get no more notifications for it. The group's owner and members see the member list update. The owner sees no Leave action. Ownership transfer and deleting a group are out of scope.

US3 - Remove and block from a group (P2):
- Remove: the group owner can remove a member from Group Detail's member list, after a confirmation.
- Block: the owner can also block the member. A blocked user is removed and can't be re-invited to that group, matched by user id and by their account email. Any pending invitations from that group to them are cancelled, and the group disappears for them.
- Manage blocks: the owner can see the group's blocked people and unblock them.
- Member-side block: a member can choose "Leave and block", which stops that group from inviting them again.
- Notifications: removed and blocked users get no notifications from the group. The owner gets no notification when a member leaves.

Edge cases to specify:
- leaving or removal while a meetup the person created is upcoming (the meetup stays with the group);
- inviting a blocked email (the owner sees a neutral "can't invite this person" without saying why);
- a user pending account deletion (016);
- reports about users later deleted.

Privacy and retention:
- Add reports and blocks, and the email provider as a new processor, to specs/016-account-deletion/data-retention.md, web/public/privacy/index.html, web/app-privacy.md, web/data-safety.md and web/tests/privacy-policy.test.js, in one change.
- Reports are kept for the period data-retention.md sets, and may outlive account deletion when kept for legal preservation. Blocks are deleted with the group or with either user's account.

Delivery: one spec, but each user story is implemented on its own branch off the feature branch, named <feature>-report, <feature>-leave and <feature>-block, and merged as its own PR in that order. Update plan/child-safety/ so its page draft and prompts describe this report form instead of the mail-only "Report a concern". That later feature then only adds the standards page and its links.

Out of scope: automated content filtering, blocking users across the whole app (blocks are per group), deleting groups, transferring ownership, and an in-app moderation console (reports are handled by email and the child-safety runbook).
```

---

## 2. `/speckit.clarify`

```
Confirm in the spec:
(a) the email provider (Resend by default) and the sending address;
(b) whether the email is sent by a database webhook to an Edge Function or by an outbox job like notifications-dispatch;
(c) the reason list and the details length limit;
(d) the report rate limit;
(e) how long reports are kept, and how that fits the legal preservation in plan/child-safety/README.md;
(f) whether "Leave and block" is included (recommended, because Apple's 1.2 is about members blocking abusive users);
(g) whether blocks are lifted only by the owner, or also by a member undoing their own "Leave and block";
(h) what the reported user sees, if anything. Recommend: nothing.
```

---

## 3. `/speckit.plan`

```
Plan under .specify/memory/constitution.md, using the | Principle | Status | Notes | Constitution Check table. Read CLAUDE.md first, especially the 006, 012, 016 and 017 gotchas. The constitution's Additional Constraints apply directly: social features need privacy defaults and safety controls, and every stored data type needs documented retention.

Constraints:
- Layering (CLAUDE.md "Real architecture"):
  - Core: models, interfaces and FluentValidation validators for reports, leave and block.
  - Infrastructure: repositories through Supabase.Client.From<T>() with manual Map() methods.
  - Api: minimal-API endpoints with paired CommandService/QueryService, Result-enum statuses, and invalidation of the 30-second groups cache on leave, remove and block.
  - App: talks to the Api through Refit.
  - The Api's Supabase client stays per-request and impersonates the caller, never the service role.
- Database:
  - New migrations for abuse_reports and group_blocks. Membership deletion paths for leave and remove.
  - Invitation creation refuses blocked users by id and by email.
  - RLS on every new table. Every new or recreated policy on groups, memberships, invitations, meetups and user_profiles keeps the private.account_is_active / private.group_is_active conditions, scoped to authenticated (016).
  - active_memberships must exclude departed, removed and blocked users, so notification recipients drop them.
  - Extend the account-purge Edge Function for the new tables.
  - pgTAP tests (supabase test db) for every policy and refusal rule.
- Email: one Edge Function (TypeScript, Deno) sends the report email with the provider key held in Supabase secrets. Deno tests import jsr: specifiers directly (CLAUDE.md gotcha). The email holds what's needed to act on the report: ids, display names, group name, reason, details and time. It carries no auth tokens. If an Edge Function is added, add it to ci-scope.sh and ci-scope.test.sh, in the same way existing functions are mapped.
- App:
  - A Report form page, a Leave action and confirmation, and Remove/Block actions with confirmations on Group Detail's member list.
  - A "Blocked" list for owners.
  - A Profile row using the 022 row pattern.
  - ViewModels are sealed partial ObservableObject classes with Async commands.
  - The mail-app fallback uses MAUI's Email API, then shows the address to copy.
  - Accessibility: SemanticProperties on icon-only actions.
- Tests (Principle II):
  - xUnit with hand-rolled fakes and no mocking framework.
  - Core validator tests.
  - Api tests with WebApplicationFactory and in-memory repositories, covering every Result status (self-report refused, rate limit, non-member refused, owner can't leave, non-owner can't remove or block, blocked invite refused).
  - ViewModel tests including the fallback path.
  - Source-inspection tests for XAML bindings.
  - Deno tests for the Edge Function.
  - pgTAP for RLS.
- Logging: structured logging on report, leave, remove and block, never with the reporter's email or the report text (LoggingPrivacyTests).
- Privacy and retention: data-retention.md, the privacy policy, app-privacy.md, data-safety.md and privacy-policy.test.js change in one PR, with the US1 branch. List the email provider as a processor.
- Contracts: write the new Api endpoints and the Edge Function's input in contracts/ before implementation (Principle VI).
- quickstart.md, run on staging, iPhone and Android:
  - File a report from Profile and from a member, and receive it at abuse@loopmeet.io.
  - Force the fallback with no network.
  - Leave a group.
  - Remove a member, block another, and try to re-invite them by email.
  - "Leave and block", if included.
  - Confirm notifications stop for removed and blocked users.
  - Delete an account and confirm blocks are purged and reports kept as the retention doc says.
  - This walk-through is also the screen-recording script for App Review.
```

---

## 4. `/speckit.tasks`

```
Generate tasks.md with one phase per user story, in the order US1 report, US2 leave, US3 block. Each phase starts with "create branch <feature>-<story> from the feature branch" and ends with "open a PR for this story". Tests come before implementation in every phase. Put shared setup (migrations both stories need, contracts) in the phase of the first story that needs it, not in a shared phase, so each PR stands alone. Keep tasks.md ticked in place.
```

---

## 5. `/speckit.analyze`, then `/speckit.implement`

```
/speckit.analyze: check spec, plan and tasks against each other and against the constitution, and fix any gaps before implementing.
/speckit.implement: work one phase at a time on that phase's branch. Keep dotnet test, deno test and supabase test db green after each phase, with no dead code, commented-out code or TODOs. Stop after each phase's PR task so it can be reviewed and merged before the next branch starts.
```

---

## After it ships

Update the App Review notes (Store Assets → AppStore → App-Review-Notes.txt) so the Report and Block lines describe the real flows. Record the quickstart walk-through on a physical iPhone for the review.
