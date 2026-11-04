# Tasks: LoopMeet Website, Privacy Policy and Web Account Deletion

**Input**: [spec.md](./spec.md), [plan.md](./plan.md), [research.md](./research.md), [contracts/site.md](./contracts/site.md), [quickstart.md](./quickstart.md)
**Tests**: Required (Constitution II). Each test task comes before the code it covers, and is seen to fail first.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: can run in parallel (different files, no unfinished dependencies)
- **[Story]**: the user story from spec.md

**Delivery** (plan.md):

| PR | Phases | Tasks |
| --- | --- | --- |
| PR A | Setup, Foundational, US2, US4 | T001–T016 |
| PR B | US1, US3, Polish | T017–T036 |

---

## Phase 1: Setup

- [X] T001 Create the `web/` layout from plan.md:
  - `web/public/` with `index.html`, `privacy/`, `support/`, `account/`, `css/`, `js/` and `images/`
  - `web/tests/`
- [X] T002 [P] Add a `web` key to `.github/scripts/ci-scope.sh`:
  - `web/*` selects `web`.
  - `specs/016-account-deletion/data-retention.md` selects `web`. This rule goes before the `*.md` documentation rule.
  - Update the header comment's dependency map.

  Add cases for both paths to `.github/scripts/ci-scope.test.sh`, and run `bash .github/scripts/ci-scope.test.sh`.
- [X] T003 [P] Add a `Run Website Tests` job to `.github/workflows/tests.yml`:
  - It runs when `needs.changes.outputs.web == 'true'`.
  - It sets up Deno v2.x, then runs `deno lint web/` and `deno test --allow-read=web,specs/016-account-deletion web/`.
  - Expose the `web` output from the `changes` job, and add the new job to Tests Gate's `needs` and its `jq` check.

## Phase 2: Foundational (shared by every page)

- [X] T004 Create `web/tests/pages.test.js`. It reads every `web/public/**/index.html` and asserts:
  - **Page basics:** `<html lang="en">`, a non-empty `<title>`, exactly one `<h1>`, and a viewport meta.
  - **Shared chrome:** a `<header>` and `<footer>` identical across all four pages, linking `/`, `/privacy/`, `/support/` and `/account/`, and containing "Palehorse, LLC" and `mailto:info@palehorse.net` (FR-001, FR-004).
  - **Internal links:** every `href` and `src` starting with `/` resolves to a file in `web/public/`.
  - **No scripts where none are needed:** Home, Privacy and Support contain no `<script>` (FR-002).
  - **Images:** every `<img>` has `alt`.
- [X] T005 [P] Create `web/tests/headers.test.js`, asserting that `web/public/_headers` sets:
  - **For `/*`:** `Content-Security-Policy` with `default-src 'self'`, `script-src 'self'`, `frame-ancestors 'none'` and `base-uri 'self'`, plus `connect-src` listing the Supabase URL and the Api base URL from `config.js`. Also `Referrer-Policy: strict-origin-when-cross-origin`, `X-Content-Type-Options: nosniff` and `Permissions-Policy` turning off camera, microphone and geolocation.
  - **For `/account/*`:** `Cache-Control: no-store`.

  Also assert that `web/public/_redirects` sends `/delete-account` to `/account/` with a 301.
- [X] T006 Create `web/public/css/site.css`:
  - Readable widths, system fonts, light and dark through `prefers-color-scheme`, and visible focus outlines.
  - Text contrast of at least 4.5:1 in both themes (FR-003).
  - No horizontal scroll at 320 px.
- [X] T007 Create `web/public/_headers` and `web/public/_redirects` to satisfy T005. `connect-src` uses the real Supabase and Api hosts from T008.
- [X] T008 Create `web/public/js/config.js` from contracts/site.md, with the real Supabase URL, publishable key and Api base URL, and `appleWebClientId: ""`. All of these are public values.
- [X] T009 Write the shared header and footer, and apply them to all four `index.html` files. Add the app icon to `web/public/images/`. `account/index.html` is a placeholder at this point: "Account management is coming soon. To delete your account now, use Profile → Delete Account in the app, or email `info@palehorse.net`."

**Checkpoint**: `deno test` passes for pages and headers.

## Phase 3: User Story 2, an accurate privacy policy (P1)

- [X] T010 [US2] Create `web/tests/privacy-policy.test.js`. It reads `web/public/privacy/index.html` and asserts:
  - **Categories:** every FR-014 category is present: account, profile photo, groups, invitations with the invitee's email, meetups with places, approximate location "not stored", place-search text, push-notification identifiers and delivery records, the Sign in with Apple token, and operational logs.
  - **Processors:** every FR-015 company is named: Supabase, DigitalOcean, Google, Apple, OneSignal, Firebase Cloud Messaging and Cloudflare.
  - **Retention matches `data-retention.md`:** the test reads that file and checks that "30 days", "24 hours" and the 7-day Supabase backups and logs appear in both documents.
  - **Statements:** no sale, no advertising, and no third-party analytics or ad SDKs (FR-017). Deletion in the app and on the web, plus `info@palehorse.net` (FR-018).
  - **Dates and audience:** a "Last updated" date in ISO or long form, and the under-13 statement.
- [X] T011 [US2] Write `web/public/privacy/index.html`:
  - The 10 sections from plan.md, in plain language, with each section's heading linkable by `id`.
  - The retention section follows `specs/016-account-deletion/data-retention.md` value for value.
- [X] T012 [P] [US2] Write `web/data-safety.md`, mapping each policy section to its Play Data safety category and answer:
  - collected or shared
  - optional or required
  - purpose
  - encrypted in transit
  - deletion request available

  Approximate location is marked as processed ephemerally, not collected.

## Phase 4: User Story 4, Home and Support (P3)

- [X] T013 [US4] Extend `web/tests/pages.test.js`:
  - **Home:** links to the Google Play listing for `io.loopmeet.app`, and has no App Store link until 013.
  - **Support:** mentions "Profile → Delete Account", links to `/account/`, mentions the 30 days, and has `mailto:info@palehorse.net`.
- [X] T014 [P] [US4] Write `web/public/index.html`: what LoopMeet is (groups, meetups, invitations, reminders), the Google Play badge link, and links into Privacy and Support.
- [X] T015 [P] [US4] Write `web/public/support/index.html`. It's an FAQ covering:
  - signing in
  - invitations
  - notifications
  - deleting your account in the app or on the web
  - what happens during the 30 days and how to restore
  - contact
- [ ] T016 Run `deno lint web/` and `deno test --allow-read=web,specs/016-account-deletion web/`. Then do the quickstart's PR A setup and checks 1–6 on the preview.
  - [X] Lint and the 34 website tests pass. Every page loads from a local server, with the largest at 12 KB. Screenshots at 1280, 390 and 320 px, light and dark, show no sideways scroll.
  - [ ] Cloudflare setup and quickstart checks 1–6 on the preview (you).

**Checkpoint: PR A.** Commit, push and open the PR. Once it's merged, Play can use the privacy URL.

## Phase 5: User Story 1, delete my account from the web (P1)

### Backend: CORS

- [ ] T017 [P] [US1] Create `tests/LoopMeet.Api.Tests/Cors/SiteCorsTests.cs`, using `WebApplicationFactory<Program>` with `Cors:AllowedOrigins` set to `https://site.test` and `https://*.preview.test`:
  - **Allowed preflight:** an `OPTIONS /users/account-deletion` from `https://site.test` with `Access-Control-Request-Method: POST` returns 204, with `Access-Control-Allow-Origin: https://site.test`, and `authorization` and `content-type` allowed.
  - **Wildcard previews:** `https://abc.preview.test` is allowed.
  - **Disallowed origin:** `https://evil.test` gets no `Access-Control-Allow-Origin`.
  - **401s:** an unauthenticated `GET /users/account-deletion` from an allowed origin returns 401 with `Access-Control-Allow-Origin`.
  - **No credentials:** no response includes `Access-Control-Allow-Credentials`.
  - **No `Origin` header:** a request without one behaves as today.
- [ ] T018 [US1] Implement T017:
  - In `src/LoopMeet.Api/Program.cs`, add `AddCors` with the `Site` policy, and `app.UseCors("Site")` before `UseAuthentication`.
  - In `src/LoopMeet.Api/appsettings.json`, set `Cors:AllowedOrigins` to `["https://loopmeet.pages.dev", "https://*.loopmeet.pages.dev"]`.
  - In `appsettings.Development.json`, set `["http://localhost:4507"]`.
- [ ] T019 [P] [US1] Create `supabase/functions/apple-authorization/cors.test.ts`, testing `parseOrigins` and `corsHeadersFor`:
  - exact match
  - a `*.` wildcard match on one label, which rejects the bare parent and a two-label prefix
  - a disallowed origin gives `null`
  - an empty list allows nothing
  - the allowed headers include `apikey` and `x-client-info`

  Add a wrapper test showing that:
  - an `OPTIONS` from an allowed origin gives 204 with the headers
  - a `POST` from an allowed origin gets the handler's response plus the headers
  - a request without `Origin` gets the handler's response unchanged
- [ ] T020 [US1] Implement T019:
  - Create `supabase/functions/apple-authorization/cors.ts`, exporting `parseOrigins`, `corsHeadersFor` and `withCors(handler, allowed)`.
  - In `index.ts`, wrap `handleAppleAuthorization` with `withCors(…, parseOrigins(Deno.env.get("SITE_ORIGINS") ?? ""))`.

  Run `deno lint supabase/functions/` and `deno test --allow-read=supabase/functions supabase/functions/`.
- [ ] T021 [P] [US1] In `supabase/config.toml`, add `http://localhost:4507/account/` to `additional_redirect_urls`.

### Site: Account page

- [ ] T022 [P] [US1] Create `web/tests/account-state.test.js` for `web/public/js/account-state.js`:
  - **`viewFor`:** gives `signed-out`, `loading`, `active`, `scheduled`, `purging` and `error` for each session and status combination in contracts/site.md.
  - **`canConfirm`:** `"DELETE"` is true, while `"delete"`, `" DELETE"`, `"DELETE "` and `""` are false.
  - **`messageFor`:** maps a network failure, a 401, an invalid login, a cancelled provider window and an unknown error to the contract's fixed messages, and never includes the raw error text.
  - **`showApple(config)`:** false for `""`, true for a Services ID.
  - **`formatDeletionDate`:** formats in the visitor's locale and time zone, from an ISO string.
- [ ] T023 [P] [US1] Create `web/tests/account-api.test.js` for `web/public/js/account-api.js`, using a fake `fetch` that records each call:
  - **Status, preview, request:** the exact URLs, methods, the `Authorization: Bearer` header, `Content-Type` on bodies, and the `{"confirmation":"DELETE"}` body.
  - **Results:** returns parsed JSON on 200 and 202. Throws a typed `AccountApiError` with `status` and `code` on 400, 401 and 409, and a `network` error when `fetch` rejects.
  - **`registerAppleToken`:** posts `{refreshToken, clientId}` to `{supabaseUrl}/functions/v1/apple-authorization` with the bearer and `apikey` headers. It resolves without throwing on any failure (FR-011).
- [ ] T024 [US1] Implement `web/public/js/account-state.js` and `web/public/js/account-api.js` to pass T022 and T023. They're pure ES modules with no DOM or global access; `fetch`, the config and the token are passed in.
- [ ] T025 [US1] Vendor the supabase-js UMD build:
  - Download the latest `@supabase/supabase-js` 2.x `dist/umd/supabase.js` from the npm registry tarball into `web/public/vendor/supabase.js`.
  - Record the version, source URL and SHA-256 in `web/vendor.lock`.
  - Create `web/tests/vendor.test.js`, which hashes the file with WebCrypto and compares it to the lock.
- [ ] T026 [US1] Write `web/public/account/index.html`:
  - **Sections:** one `<section>` per view (contracts/site.md), all hidden except the current one.
  - **Sign-in form:** email and password, with labels and `autocomplete` attributes, plus Google and Apple buttons.
  - **Confirm view:** the owned-groups list, the date, the DELETE field with a visible label and `autocomplete="off"`, and a disabled Delete button.
  - **Messages:** an `aria-live="polite"` region.
  - **`<noscript>`:** "This page needs JavaScript. To delete your account, use Profile → Delete Account in the app, or email `info@palehorse.net`."
  - **Scripts:** `vendor/supabase.js`, then `<script type="module" src="/js/account.js">`.

  Update `pages.test.js` so the Account page alone may have scripts, and must have the `<noscript>`.
- [ ] T027 [US1] Implement `web/public/js/account.js`:
  - **Client:** create it per research D4, with PKCE, `sessionStorage` and `detectSessionInUrl`.
  - **Sign-in:** email and password with `signInWithPassword`. Google and Apple with `signInWithOAuth`, redirecting to `location.origin + "/account/"`.
  - **Rendering:**
    - `onAuthStateChange` loads the status and renders `viewFor`.
    - Rendering moves focus to the shown section's heading.
    - The Delete button's state follows `canConfirm` on each input.
  - **Delete flow:** Delete loads the preview and shows the confirm view. Confirm posts the request and shows the scheduled view.
  - **Apple:** on `SIGNED_IN` with provider `apple` and a `provider_refresh_token`, call `registerAppleToken` without awaiting it.
  - **Errors:** everything goes through `messageFor`. A 401 calls `signOut()` first.
  - **Sign-out:** calls `auth.signOut()`.
- [ ] T028 [US1] Run the site, Api and edge-function suites, then `dotnet build` for the Api.

## Phase 6: User Story 3, change my mind from the web (P2)

- [ ] T029 [US3] Extend `account-state.test.js` and `account-api.test.js`:
  - **Restore:** `restore` calls `DELETE /users/account-deletion`, a 200 response gives the `active` view, and a 409 `deletion_in_progress` gives `purging`.
  - **Scheduled view:** offers restore and sign-out actions.
  - **Purging view:** offers sign-out only.
- [ ] T030 [US3] Implement T029 in `account-state.js`, `account-api.js` and `account.js`: wire Restore account, then re-read the status.

## Phase 7: Polish and go-live

- [ ] T031 [P] Check every page with the browser's accessibility tools (Lighthouse or axe) on mobile and desktop, and fix any contrast, label or heading issues they report.
- [ ] T032 [P] Check page weights. Home, Privacy and Support should be under 50 KB each, uncompressed, without the images (SC-004).
- [ ] T033 Update `CLAUDE.md` Known gotchas:
  - The site's origin list lives in three places: `Cors:AllowedOrigins`, the `SITE_ORIGINS` secret, and Supabase's Redirect URLs. Change them together.
  - The privacy policy and `web/data-safety.md` must change with `data-retention.md`.
  - The Supabase Apple Secret Key for the web expires after at most 6 months.
- [ ] T034 Run the full suites CI selects for this branch locally: site, Api tests, and edge functions.
- [ ] T035 Do the quickstart's PR B backend setup, then checks 7–21, on a preview with test accounts. Checks 22–24 run once the Apple web setup is done.
- [ ] T036 **Checkpoint: PR B.** Commit, push and open the PR. The custom domain (quickstart checks 25–27) and pointing Play at its URLs follow after merge.

## Dependencies

- **Setup (T001–T003)** comes before everything else.
- **Foundational (T004–T009)** comes before every story.
- **US2 (T010–T012) and US4 (T013–T015)** are independent of each other. Together they're PR A.
- **US1 backend (T017–T021) and US1 site (T022–T027)** can proceed in parallel. T027 needs T024 and T025.
- **US3 (T029–T030)** needs US1's Account page.

## Parallel examples

- **PR A:** T002 and T003, then T005 next to T004, then T012 next to T011, and T014 next to T015.
- **PR B:** T017, T019, T021, T022 and T023 together, then T018, T020 and T024.
