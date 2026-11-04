# Feature Specification: LoopMeet Website, Privacy Policy and Web Account Deletion

**Feature Branch**: `017-loopmeet-site`
**Created**: 2026-11-04
**Status**: Draft
**Input**: User description: "017: the loopmeet.io site. A static site in web/ of this repo on Cloudflare Pages, with Home, Privacy, Support and Account pages. The Account page lets people sign in with email, Google or Apple and request account deletion, see its status and restore within 30 days, using 016's endpoints. Rewrite the privacy policy (Palehorse, LLC, info@palehorse.net) to match what the app really collects, based on 016's data-retention.md. loopmeet.io uses production, and preview builds use staging. Plain HTML and a small amount of JavaScript."

## Background

Google Play requires two things for apps that let people create accounts:

- **Deletion from the web:** people can request deletion from a web page as well as inside the app. The App Store has the same rule for in-app deletion.
- **An accurate privacy policy:** the policy must match the Data safety form.

016 built deletion in the app, along with the request, status and restore operations a web page can call. The current privacy policy, at palehorse.net/privacy, doesn't mention profile photos, location used for place search, push-notification identifiers, or the companies that process data for LoopMeet.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Delete my account from the web (Priority: P1)

Someone who no longer has the app installed, or prefers a browser, goes to loopmeet.io. They sign in with the method they used in the app (email and password, Google or Apple) and request deletion. The page offers the same choices the app does:

- a list of the groups they own that will be deleted, with each group's member count
- typing DELETE to confirm
- the date their data will be permanently deleted

**Why this priority**: Google Play requires a web deletion path for the app to stay listed. Without it, the app can't move beyond internal testing.

**Independent Test**: Sign in on loopmeet.io with an account that owns a group, then request deletion. The page shows the deletion date. In the app, the account is now scheduled for deletion, exactly as if it had been requested there.

**Acceptance Scenarios**:

1. **Given** a visitor on the Account page, **When** they sign in with email and password, Google or Apple, **Then** the page shows their account's deletion status. It shows "not scheduled" for an active account.
2. **Given** a signed-in user with no deletion scheduled, **When** they choose to delete their account, **Then** the page explains what will be deleted, including each group they own with its member count. The delete button stays disabled until they type DELETE exactly.
3. **Given** they confirm, **When** the request succeeds, **Then** the page shows the date their data will be permanently deleted, 30 days later. Their account is then hidden from everyone else, exactly as with an in-app request.
4. **Given** an account already scheduled for deletion, **When** the user signs in on the web, **Then** the page shows the scheduled date instead of the delete form. Signing in never cancels the deletion.
5. **Given** sign-in fails (wrong password, or the provider window is cancelled), **When** it ends, **Then** the page says so and the visitor can try again. Nothing about the account changes.

---

### User Story 2 - Read an accurate privacy policy (Priority: P1)

A prospective user, or a store reviewer, opens loopmeet.io/privacy. In plain language, they learn:

- what LoopMeet collects, why, and who processes it
- how long it's kept
- how deletion works, including the 30-day grace period
- how to contact the company

It matches what the app actually does and what the store listings declare.

**Why this priority**: The store listings and Play's Data safety form must link to a policy that matches the app. The current policy doesn't.

**Independent Test**: Compare loopmeet.io/privacy with the app's real data use, as listed under Requirements, and with 016's data-retention document. Every item is covered and nothing contradicts them.

**Acceptance Scenarios**:

1. **Given** the privacy page, **When** a reader looks for what's collected, **Then** they find every category the app uses:
   - account details, and the sign-in method used
   - profile photo
   - groups, invitations (including the invitee's email) and meetups with their places
   - device location, which can be precise, used only to suggest nearby places and not stored
   - place-search text
   - push-notification identifiers and delivery records
   - the Sign in with Apple token kept for revocation
   - operational logs
2. **Given** the privacy page, **When** a reader looks for who processes data, **Then** they find every company LoopMeet uses, and what each one does.
3. **Given** the privacy page, **When** a reader looks up deletion and retention, **Then** they find the 30-day grace period, what's deleted and when, and what providers keep and for how long, with the same values as 016's data-retention document.
4. **Given** the privacy page, **When** a reader wants to ask about their data, **Then** they find Palehorse, LLC and info@palehorse.net.

---

### User Story 3 - Change my mind from the web (Priority: P2)

A user who scheduled deletion, in the app or on the web, signs in on loopmeet.io within 30 days. They see the scheduled date and a Restore account button. Restoring cancels the deletion and brings everything back, exactly as restoring in the app does.

**Why this priority**: It isn't required by the stores, but the endpoint already exists. Without it, a web-only user would have to install the app to undo a mistake.

**Independent Test**: Request deletion in the app, then sign in on loopmeet.io and choose Restore account. In the app, the account is active again and its groups are back for everyone.

**Acceptance Scenarios**:

1. **Given** a signed-in user whose deletion is scheduled, **When** they choose Restore account, **Then** the deletion is cancelled and the page shows the account as active.
2. **Given** a user whose deletion date has passed, or whose deletion is in progress, **When** they sign in, **Then** the page says the account is being deleted and can no longer be restored, and offers no Restore button.
3. **Given** a signed-in user, **When** they choose Sign out, **Then** they're signed out of the site, and nothing about the deletion changes.

---

### User Story 4 - Find out about LoopMeet and get help (Priority: P3)

A visitor lands on loopmeet.io from a store listing, a search or an invitation. The home page says what LoopMeet is and links to the app stores. The support page answers common questions, including how to delete your account from the app or the web, and gives the contact address. Every page links to Privacy, Support and Account.

**Why this priority**: The stores ask for a support URL, and visitors need a way into the other pages. It's useful but simple.

**Independent Test**: Open loopmeet.io. Each page is reachable from every other page, the support page explains both deletion paths and the contact address, and the store links work.

**Acceptance Scenarios**:

1. **Given** the home page, **When** a visitor reads it, **Then** they see a short description of LoopMeet and links to the store listings that are live.
2. **Given** the support page, **When** a visitor looks for help, **Then** they find:
   - how to delete their account in the app (Profile → Delete Account) and on the web (the Account page)
   - what happens during the 30 days
   - the contact address
3. **Given** any page, **When** a visitor looks at the header or footer, **Then** it links to Home, Privacy, Support and Account.

---

### Edge Cases

- **Account pending deletion:** after sign-in, the site's own requests return the "scheduled for deletion" response. The Account page treats this as the scheduled-deletion view, never as an error or a sign-out.
- **Unknown sign-in:** a visitor signs in with an account that doesn't exist, or with a different method than the one they used in the app. Sign-in fails, or creates a separate empty account, as the sign-in provider does today. The page tells them which account they're signed in as (the email) before they confirm a deletion.
- **Apple's private relay email:** a user who hid their email with Apple sees that relay address as the signed-in account. Deletion works as normal.
- **Sign in with Apple on the web:** the page also passes Apple's token to LoopMeet so it can be revoked at permanent deletion, as the app does. If that fails, sign-in and deletion still work.
- **Request already made:** a request made again, from the app or the web, keeps the original date (016).
- **Accounts that can't be restored:** an account in the middle of permanent deletion, or already deleted, can't be restored. A deleted account can no longer sign in, and the page explains that.
- **Scripts unavailable:** with scripts disabled or blocked, Home, Privacy and Support still read fully. The Account page says it needs JavaScript and gives the email address for deletion requests.
- **Previews and real accounts:** there is one LoopMeet backend, so a preview build's Account page acts on real accounts. Previews are checked with test accounts only.
- **Page sizes:** every page reads well on a phone and on a desktop.

## Requirements *(mandatory)*

### Functional Requirements

#### Site

- **FR-001**: loopmeet.io MUST serve four pages: Home, Privacy (`/privacy`), Support (`/support`) and Account (`/account`). Each MUST link to the other three from a shared header or footer.
- **FR-002**: Home, Privacy and Support MUST be fully readable without JavaScript.
- **FR-003**: Pages MUST be readable on phone and desktop screens, follow basic accessibility (headings, labels, contrast, keyboard use), and load quickly on a mobile connection.
- **FR-004**: Every page MUST identify the operator as Palehorse, LLC and give the contact info@palehorse.net.

#### Account page

- **FR-005**: The Account page MUST let a visitor sign in with email and password, Google, or Apple. The accounts are the same ones the apps use.
- **FR-006**: After sign-in, the page MUST show which account is signed in (its email) and that account's deletion status: none, scheduled with the date, or in progress.
- **FR-007**: With no deletion scheduled, the page MUST:
  - show the deletion explanation, including each owned group with its member count
  - require typing exactly DELETE before the request can be sent
  - after a successful request, show the permanent-deletion date
- **FR-008**: With deletion scheduled, the page MUST show the date and offer Restore account and Sign out. Only Restore account cancels the deletion, as in the app.
- **FR-009**: Once restore is no longer possible, because the date has passed or deletion is in progress, the page MUST say so and MUST NOT offer Restore.
- **FR-010**: The page MUST use 016's request, preview, status and restore operations as the signed-in user. Behaviour MUST therefore be identical to the app's, including keeping the original date on a repeat request.
- **FR-011**: After a Sign in with Apple on the web, the page MUST pass Apple's refresh token, with the web client's identifier, to 016's Apple authorization step, so the purge can revoke it. A failure there MUST NOT block sign-in or deletion.
- **FR-012**: The page MUST NOT store anything about the visitor beyond the sign-in session the provider keeps in the browser. Signing out MUST clear that session.
- **FR-013**: Failures MUST show a short, specific message the visitor can act on, such as "Couldn't reach LoopMeet. Check your connection and try again." Raw errors MUST NOT be shown.

#### Privacy policy

- **FR-014**: The privacy policy MUST cover every data category the app and backend use:

  | Category | What it covers |
  | --- | --- |
  | Account | email, password (stored hashed by the sign-in provider), display name, optional phone, time zone, sign-in method (Google or Apple identity) |
  | Profile photo | uploaded, or taken from the Google account picture |
  | What you create | groups; memberships; invitations, including the invitee's email; meetups with title, time and place (name, address, coordinates) |
  | Device location | precise when the user allows it (the app declares fine location), taken only when you search for a place, sent to Google to suggest nearby places, and not stored |
  | Place-search text | sent to Google to find places |
  | Push notifications | device identifiers, platform, language, time zone, notification permission, and delivery and open records |
  | Sign in with Apple | the refresh token, kept only to revoke it at deletion |
  | Operational logs | user identifiers and request metadata |

- **FR-015**: The policy MUST name each company that processes data for LoopMeet and its role:

  | Company | Role |
  | --- | --- |
  | Supabase | database, sign-in and file storage |
  | DigitalOcean | hosting the LoopMeet service |
  | Google | Sign-In, and Places for place search |
  | Apple | Sign in with Apple, and push delivery on iOS |
  | OneSignal | push notifications |
  | Firebase Cloud Messaging | push delivery on Android |
  | Cloudflare | hosting the website |

- **FR-016**: The policy MUST state the deletion and retention rules exactly as 016's data-retention document does:
  - the 30-day grace period, and what is hidden during it
  - what is deleted, and that it happens within 24 hours of the date
  - that no copy is kept after completion
  - provider backups (7 days) and logs (Supabase 7 days; the Api's logs not stored)
  - OneSignal's asynchronous deletion
  - Apple's own records
- **FR-017**: The policy MUST state that LoopMeet doesn't sell personal data or use it for advertising, and that there are no third-party analytics or ad SDKs in the app. These are true today: the app's only third-party SDKs are Supabase and OneSignal.
- **FR-018**: The policy MUST say how to exercise data rights: deletion in the app or on the web, and other requests by email. It MUST show a "last updated" date.
- **FR-019**: The policy's content MUST be consistent with the Play Data safety answers. A short mapping from policy sections to Data safety categories MUST be kept with the site's source, so the two can be checked together whenever either changes.

#### Environments and the LoopMeet service

- **FR-020**: loopmeet.io and its preview builds (branches and pull requests) MUST use the single LoopMeet backend, the one the app uses. The backend address MUST be fixed in the site's configuration, never chosen by visitor input.
- **FR-021**: The LoopMeet service MUST accept requests from the site's origins (loopmeet.io and its preview builds), and MUST NOT accept them from any other website.
- **FR-022**: Sign-in providers MUST return visitors to the Account page of the same site they started from, production or preview.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can go from loopmeet.io to a confirmed deletion request, including sign-in, in under 2 minutes.
- **SC-002**: An account deleted or restored on the web looks the same in the app as one deleted or restored in the app, for the user and for everyone else.
- **SC-003**: Every data category and processor the app uses appears in the privacy policy, and a side-by-side check against the app and the Data safety form finds no mismatch.
- **SC-004**: Home, Privacy and Support load and read fully on a phone over a mobile connection in under 2 seconds, with or without JavaScript.
- **SC-005**: Google Play accepts loopmeet.io/account as the web deletion URL, and loopmeet.io/privacy as the privacy policy URL.
- **SC-006**: No website other than loopmeet.io and its preview builds can call the LoopMeet service from a browser.

## Assumptions

- **Domain and hosting:** in this spec, "loopmeet.io" means the site's public address. The site launches on its Cloudflare Pages address, and a custom domain (likely `account.loopmeet.io`) is pointed at it afterwards. That changes only configuration. The site's source lives in `web/` in this repo, and previews are built for branches and pull requests.
- **One backend:** the backend used so far (called staging until now) is promoted to production. The site and the app share it, and no separate staging backend exists. Renaming the app's build configurations and deploy tags is outside this feature.
- **Apple on the web:** this needs an Apple web sign-in identifier (a Services ID) and its return addresses. These come with the Apple details already being gathered for 016, and the Account page offers Apple once they're configured.
- **Store links:** Home links only to listings that are live: Google Play now, the App Store after 013.
- **No new account features:** the site offers only what 016 already provides. It doesn't edit profiles or manage groups.
- **Testing:**
  - The Account page's logic gets automated tests.
  - The pages get automated checks for links, accessibility basics, and the policy covering every listed category and processor.
  - Sign-in with each provider, and a full delete-and-restore with a test account, are checked by hand in a quickstart.
- **Out of scope:**
  - Terms of Service
  - redirecting palehorse.net/privacy, which you'll handle separately
  - updating the store listings to the new URLs, a manual step you'll take after launch
