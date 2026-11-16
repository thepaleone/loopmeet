# Feature Specification: Safety Controls (Report, Leave, Block)

**Feature Branch**: `023-safety-controls`
**Created**: 2026-11-16
**Status**: Draft
**Input**: User description: "Safety controls for LoopMeet, needed to pass App Store guideline 1.2 (report objectionable content, block abusive users) and Google Play's in-app reporting requirement." Full prompt and design input: [design/](design/) (README and Spec Kit prompts).

## Background

App Store Review rejected LoopMeet under guideline 1.2. Apps with user-generated content must let people:

- report objectionable content;
- block abusive users.

Google Play's Child Safety Standards policy also requires a way to report a concern without leaving the app.

LoopMeet has none of these controls today. There is no report action and no support address in the app. A member can't leave a group, and an owner can't remove anyone.

LoopMeet's user content is display names, profile photos, group names, meetup details and invitations.

- **Who people see:** members only see people who share a group with them.
- **How people join:** only by an email invitation that they accept.
- **Missing features:** there are no public profiles, no search for strangers and no chat.

Reports therefore always concern a group the reporter belongs to, and the people in it.

The child-safety standards page (`plan/child-safety/`, a later feature) points readers to this feature's Report a concern. It also relies on reports reaching a monitored address.

## Clarifications

### Session 2026-11-16

- Q: Which email provider and sender send the report email? → A: SendGrid, from the verified sender `noreply@loopmeet.io`, to `abuse@loopmeet.io`, with Reply-To set to the reporter's account email.
- Q: How does a recorded report reach the email provider? → A: A database webhook on each new report calls a function that sends the email at once. A sweep every 5 minutes resends any report not yet delivered. A report still undelivered 24 hours after it was received is logged as an alert.
- Q: Are the reason list and the details limit approved? → A: Yes. The reasons are inappropriate photo or name, harassment or threats, child safety, spam, and other. Details are required, up to 1,000 characters.
- Q: What is the report rate limit? → A: 10 reports per reporter in any rolling 24 hours.
- Q: How long are reports kept, given the legal preservation in the child-safety design? → A: 1 year from receipt, unless the law requires keeping a report longer. Then it's kept for as long as the law requires.
- Q: Is Leave and block included? → A: Yes. Decline and block (on an invitation) is included too.
- Q: Who can lift a block? → A: Each person lifts only their own blocks. The owner lifts blocks the owner set, from the group's Blocked people list. A member lifts their own Leave and block or Decline and block, from Profile → Blocked groups. Neither can lift the other's.
- Q: What does the reported person see? → A: Nothing.
- Q: Can someone invited but not yet a member file a report from the invitation? → A: No. Reports cover only groups the reporter belongs to. An invitee can decline, or Decline and block.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Report a concern (Priority: P1)

A signed-in user who sees something wrong in LoopMeet opens Profile → Report a concern. They choose:

- the group;
- the person in it, or the group itself;
- a reason.

Then they describe what happened and submit. LoopMeet keeps a record of the report and sends it to the safety inbox, `abuse@loopmeet.io`. The reporter sees a confirmation that explains what happens next. The reported person is told nothing.

A member's entry in Group Detail also offers Report. It opens the same form with the group and the person already chosen.

**Why this priority**:

- Reporting is the control both stores require.
- It's the entry point for the child-safety commitments that follow.
- Without it, App Review rejects again.

**Independent Test**: on staging, a user in a shared group files a report from Profile and another from a member in Group Detail. Both reports arrive at `abuse@loopmeet.io` with the right group, person, reason and details. With the network off, the same report goes out through the device's mail app instead.

**Acceptance Scenarios**:

1. **Given** a signed-in user in at least one group, **When** they open Profile → Report a concern, **Then** a form offers:
   - the groups they belong to;
   - for the chosen group, its other members, or "The group itself (name or meetups)";
   - the reasons: inappropriate photo or name, harassment or threats, child safety, spam, other;
   - a required details field.
2. **Given** a member in Group Detail, **When** another member's entry offers Report and they choose it, **Then** the form opens with that group and person already selected, and both can still be changed.
3. **Given** a complete form, **When** the user submits, **Then**:
   - the report is recorded with the reporter, the group, the person (if one was chosen), the reason, the details and the time it was received;
   - a confirmation says the report was sent, that the safety team reviews every report, and that they may get an email reply.
4. **Given** a recorded report, **Then** within 5 minutes `abuse@loopmeet.io` receives an email. It contains everything needed to act on the report:
   - the report's reference;
   - the reason and the details;
   - the time;
   - the group's name and reference;
   - the reported person's display name and reference;
   - the reporter's display name, reference and account email, so the safety team can reply.
5. **Given** the user submits, **When** the report can't be sent (no network, or a server error), **Then**:
   - the form keeps everything they typed;
   - it offers to open the device's mail app with the same content, addressed to `abuse@loopmeet.io`;
   - if no mail app is available, it shows the address so it can be copied.
6. **Given** the reporter, **Then** they don't appear as a person they can report.
7. **Given** a user who has already sent 10 reports in the last 24 hours, **When** they submit another, **Then**:
   - it is refused with a message saying they can send more later, or email `abuse@loopmeet.io` directly;
   - what they typed is kept.
8. **Given** the details field, **Then** it accepts up to 1,000 characters, shows how many are left, and can't be submitted empty or blank.
9. **Given** any report, **Then** the reported person gets no notification, and nothing in their app changes because of it.

---

### User Story 2 - Leave a group (Priority: P2)

A member who no longer wants to be in a group leaves it from Group Detail, after confirming. The group and its meetups disappear for them straight away, and they stop getting its notifications. The group's owner can't leave.

**Why this priority**: leaving is the simplest way for someone to get away from a group they're uncomfortable in. Block (US3) builds on it.

**Independent Test**: on staging, a member leaves a group. The group vanishes from their Groups tab and Home. The owner's member list no longer shows them, and a new meetup in that group sends them no push.

**Acceptance Scenarios**:

1. **Given** a member who isn't the owner, **When** they open Group Detail, **Then** a Leave group action is available.
2. **Given** they choose Leave group, **Then** a confirmation names the group and says they'll need a new invitation to rejoin. Cancelling changes nothing.
3. **Given** they confirm, **Then**:
   - they return to the Groups tab;
   - the group and its meetups are gone from their Groups tab and Home;
   - they get no further notifications or reminders for that group.
4. **Given** a member has left, **Then** the owner and the remaining members no longer see them in the member list or member count the next time the group loads.
5. **Given** the group's owner, **Then** Group Detail offers them no Leave group action.
6. **Given** the member who left created upcoming meetups in that group, **Then** those meetups stay in the group for everyone else.
7. **Given** the owner later invites them again, **Then** they can accept and rejoin as a new member.

---

### User Story 3 - Remove and block (Priority: P2)

A group's owner can:

- **remove** a member from the group;
- **block** the member: remove them and stop them from being invited back;
- **manage blocks:** see who is blocked from the group and lift a block.

A member can protect themselves the same way: **Leave and block** stops that group from inviting them again. An invitee can **Decline and block** a group's invitation.

**Why this priority**:

- Apple's guideline 1.2 asks for blocking abusive users.
- In LoopMeet, abuse can only reach someone through a group they're in or are invited to.
- So blocking is set per group, by the owner or by the person being targeted.

**Independent Test**: on staging, the owner:

1. removes one member;
2. blocks another;
3. tries to re-invite the blocked member's email, which is refused;
4. lifts the block and invites them successfully.

A second member uses Leave and block, and the owner's invitation to them is refused with the neutral message.

**Acceptance Scenarios**:

1. **Given** the owner in Group Detail, **When** they open a member's actions, **Then** Remove from group and Block are offered. Neither is offered for the owner's own entry, and neither is shown to members who aren't the owner.
2. **Given** the owner removes a member after a confirmation, **Then** the member experiences exactly what Leave group gives them (US2 scenarios 3, 4 and 6), and is told nothing.
3. **Given** the owner blocks a member after a confirmation, **Then**:
   - the member is removed as in scenario 2;
   - any pending invitations from that group to them are cancelled;
   - the group no longer appears to them anywhere.
4. **Given** a person blocked from a group, **When** the owner invites their account email again, **Then** the invitation is refused with "This person can't be invited to this group".
5. **Given** a person blocked from a group, **When** an invitation to them reaches acceptance anyway (for example, one created at the same moment as the block), **Then** acceptance is refused, because the block matches their account. The server guarantees this, whatever the app does.
6. **Given** the owner opens the group's Blocked people list, **Then** it shows each blocked person's display name and when they were blocked. Unblock, after a confirmation, lets the group invite them again. It doesn't restore their membership.
7. **Given** a member who isn't the owner, **When** they choose Leave and block, after a confirmation, **Then**:
   - they leave as in US2;
   - the group can no longer invite them;
   - the owner's attempt gets the same neutral message as scenario 4, which doesn't say why.
8. **Given** a pending invitation, **When** the invitee chooses Decline and block, **Then**:
   - the invitation is declined;
   - the group can no longer invite them, as in scenario 7.
9. **Given** a person used Leave and block or Decline and block, **Then** they can see the groups they blocked in a Blocked groups list on Profile, and unblock one there. The owner can't lift a block a member set.
10. **Given** a person removed or blocked from a group, **Then** they get no notifications for that group from that moment, including reminders for meetups they had joined.
11. **Given** a member leaves, is removed or is blocked, **Then** the owner gets no notification about it.

---

### Edge Cases

- **Meetups the person created:** a member who leaves, is removed or is blocked may have created an upcoming meetup. It stays with the group, and members see it as before (US2 scenario 6).
- **Pending account deletion:** a person whose account is pending deletion (016) is already hidden from everyone. They can't be picked in the report form, and they can't be removed or blocked, because they don't appear in member lists. A user pending deletion can't use any of these controls, because the only screen they can reach is the pending-deletion screen.
- **Reports about deleted users:** a report about a person who later deletes their account is still kept, as data-retention.md sets out, and stays readable to the safety team. The email already sent is unaffected.
- **Reporter deletes their account:** reports they filed are kept, as data-retention.md sets out. Their blocks are deleted with the account.
- **Group owner deletes their account:** the group is deleted (016), together with its blocks.
- **Blocked email changes:** a block names the person's account and the email address they had when they were blocked. An invitation to either is refused, and acceptance is refused for the blocked account whichever email the invitation used.
- **Racing actions:** the owner removes someone at the moment that member leaves. The result is the same: they're out of the group, and neither person sees an error. A removal that arrives second succeeds. A leave that arrives second is treated by the app as already left, and returns to Groups. A block that arrives after the person left isn't applied, and the owner sees "They've already left this group."
- **Report while offline:** the mail-app fallback (US1 scenario 5) sends the same content, so a report is never lost. A fallback email has no database record, and the safety inbox handles it like any other email.
- **Owner blocks then re-adds:** the owner unblocks and invites. The person accepts as a new member, with no memory of earlier memberships.
- **Only member besides the owner leaves:** the group stays, with only the owner.
- **Reporting the owner:** members can report the group's owner like any other member.
- **No groups:** a user in no groups who opens Report a concern sees that reports cover groups they belong to, with `abuse@loopmeet.io` shown so it can be copied, for anything else.

## Requirements *(mandatory)*

### Functional Requirements

#### Report a concern (US1)

- **FR-001**: Profile MUST have a "Report a concern" row, in the 022 full-width row style, that opens the report form.
- **FR-002**: Group Detail MUST offer Report on every member's entry except the viewer's own. It opens the form with that group and person selected.
- **FR-003**: The form MUST let the reporter choose:
  - a group from those they belong to;
  - a person in that group, other than themselves, or "The group itself (name or meetups)";
  - one reason: inappropriate photo or name, harassment or threats, child safety, spam, or other;
  - details: required, not blank, at most 1,000 characters.
- **FR-004**: A submitted report MUST be recorded with:
  - the reporter, the group and the reported person (if any);
  - the reason and the details;
  - the server's receipt time.

  The system MUST refuse a report that names a group the reporter isn't a member of, or a person who isn't in that group.
- **FR-005**: Each recorded report MUST be emailed to `abuse@loopmeet.io` from `noreply@loopmeet.io`, with Reply-To set to the reporter's account email and the contents in US1 scenario 4.
  - **Timing:** sending starts as soon as the report is recorded, and the report arrives within 5 minutes.
  - **Retries:** a report not yet delivered is retried every 5 minutes, and an alert is logged if it's still undelivered 24 hours after it was received.
  - **No loss:** a report MUST NOT be lost because the email failed.
- **FR-006**: A user MUST NOT be able to submit more than 10 reports in any 24-hour period. Extra reports are refused with a message that suggests emailing `abuse@loopmeet.io`.
- **FR-007**: If a report can't be submitted, the form MUST keep its contents, and offer to open the device's mail app with the same content addressed to `abuse@loopmeet.io`. Without a mail app, it MUST show the address so it can be copied.
- **FR-008**: The reported person MUST NOT be notified of, or able to see, any report about them.

#### Leave a group (US2)

- **FR-009**: Any member except the owner MUST be able to leave a group from Group Detail, after a confirmation.
- **FR-010**: From the moment a member leaves, is removed or is blocked, they MUST NOT:
  - see the group, its members or its meetups;
  - count in its member lists;
  - receive its notifications or reminders.
- **FR-011**: The owner MUST NOT be offered Leave group. Handing over ownership and deleting groups are out of scope.
- **FR-012**: Meetups created by a person who leaves, is removed or is blocked MUST stay with the group.

#### Remove and block (US3)

- **FR-013**: Only the group's owner MUST be able to remove or block a member, each after a confirmation. The owner can't remove or block themselves.
- **FR-014**: Blocking MUST:
  - remove the member;
  - cancel the group's pending invitations to them;
  - record the block against both their account and the email address they had at that moment.
- **FR-015**: The system MUST refuse:
  - an invitation from a group to a person or email blocked from it;
  - a blocked person's acceptance of any invitation to that group.

  The inviter sees "This person can't be invited to this group", whoever set the block.
- **FR-016**: The owner MUST be able to list the group's blocked people (display name and date) and unblock them. The owner can't see or lift blocks set by members.
- **FR-017**: A member MUST be able to Leave and block, and an invitee MUST be able to Decline and block, each after a confirmation. Either one blocks that group from inviting them.
- **FR-018**: A person MUST be able to list the groups they blocked, on Profile → Blocked groups, and unblock them.
- **FR-019**: Leaving, removal and blocking MUST NOT notify anyone.

#### Cross-cutting

- **FR-020**: Every new rule MUST be enforced by the server, not only hidden in the app. Examples:
  - a non-owner can't remove or block;
  - a person can't report themselves;
  - a blocked person can't be invited.
- **FR-021**: Reports, leaving, removal and blocking MUST be logged by account reference, never by email address or report text.
- **FR-022**: Privacy and retention MUST be documented together, in one change:
  - reports and blocks, as new stored data;
  - their retention;
  - the email provider, as a new data processor.

  The documents are the data-retention document, the privacy policy, the App Store privacy answers and the Play data safety answers.
- **FR-023**: Account deletion (016) MUST delete a user's blocks, those they set and those set against them. It MUST keep their reports, filed and received, for the retention period in FR-024.
- **FR-024**: Reports MUST be kept for 1 year from receipt, then deleted. A report preserved for a legal obligation is kept for as long as that obligation requires.
- **FR-025**: The controls MUST be usable with a screen reader. Every icon-only action has a spoken description.

### Key Entities

- **Report**: a concern sent by a user. It holds:
  - the reporter;
  - the group;
  - the reported person, or none when the concern is about the group itself;
  - the reason, from a fixed list, and the details (up to 1,000 characters);
  - the time received;
  - whether the safety email was delivered.

  Kept 1 year, longer under a legal hold. It outlives either account's deletion.
- **Group block**: one person barred from one group. It holds:
  - the group;
  - the blocked person's account and their email at that time;
  - who set it: the owner, or the person themselves;
  - when it was set.

  It is deleted when it's lifted, when the group is deleted, or when either account is deleted.
- **Membership** (existing): a person's place in a group. Leaving, removal and blocking end it.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can file a report from Profile in under 1 minute, and from a member in Group Detail in under 45 seconds.
- **SC-002**: 100% of recorded reports reach `abuse@loopmeet.io`, 95% of them within 5 minutes. None is lost when the email provider is briefly unavailable.
- **SC-003**: With no network, 100% of report attempts end with the mail-app fallback or the copyable address, and keep what the user typed.
- **SC-004**: After leaving, being removed or being blocked, a person receives 0 notifications from that group.
- **SC-005**: 100% of attempts to invite a blocked person, by email or by account, are refused.
- **SC-006**: The next App Store submission passes review without a guideline 1.2 rejection.
- **SC-007**: Every server-side rule in FR-020 has an automated test, and every existing automated test still passes.

## Assumptions

- **Safety inbox:** `abuse@loopmeet.io` is set up and monitored before US1 ships. The later child-safety page uses it as its safety contact.
- **Email provider:** Supabase can't send general email, only sign-in emails, so reports are sent through SendGrid. The maintainer has verified `noreply@loopmeet.io` as a sender. SendGrid becomes a new data processor, disclosed under FR-022.
- **Reporter's email:** the reporter's account email goes into the safety email only, in the body and as Reply-To, so the team can reply. It never appears in logs.
- **Delivery:** each user story is delivered on its own branch off `023-safety-controls` and merged as its own pull request, in this order:
  1. US1, report: `023-safety-controls-report`.
  2. US2, leave: `023-safety-controls-leave`.
  3. US3, block: `023-safety-controls-block`.
- **Child-safety follow-up:** after this feature, `plan/child-safety/` is updated so its page draft and prompts describe this report form, and `abuse@loopmeet.io` as the safety contact. That later feature then adds only the standards page, its links, the legal-preservation retention text and the operator runbook.

**Out of scope:**

- automated content filtering;
- blocking people across the whole app (blocks are per group);
- deleting groups and transferring ownership;
- an in-app moderation console (reports are handled from the safety inbox);
- the child-safety standards page itself.
