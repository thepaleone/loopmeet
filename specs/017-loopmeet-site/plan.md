# Implementation Plan: LoopMeet Website, Privacy Policy and Web Account Deletion

**Branch**: `017-loopmeet-site` | **Date**: 2026-11-04 | **Spec**: [spec.md](./spec.md)

## Summary

A four-page static site, Home, Privacy, Support and Account, in `web/public/`, served by Cloudflare Pages with no build step.

- **Account page:** signs in with Supabase Auth in the browser, then calls 016's existing account-deletion endpoints as the user. After an Apple sign-in, it hands Apple's refresh token to 016's `apple-authorization`.
- **Privacy policy:** rewritten from 016's `data-retention.md` and the app's real data use.
- **Backend changes:**
  - a CORS policy on the Api
  - CORS handling in `apple-authorization`
  - one local Supabase redirect URL

  There's no new endpoint and no schema change.

The site and its previews use the one existing backend, promoted to production (research D1).

## Technical Context

| | |
| --- | --- |
| **Site** | HTML5, one CSS file, ES-module JavaScript with no framework and no build. `@supabase/supabase-js` 2.x UMD, vendored and pinned (research D3). |
| **Api** | C# 13 / .NET 10 minimal API. The built-in ASP.NET Core CORS middleware, so no new packages. |
| **Edge function** | TypeScript on Deno 2.x. `apple-authorization` gets CORS handling. |
| **Hosting** | Cloudflare Pages, project `loopmeet`, output `web/public`, production branch `main`, previews per branch. |
| **Testing** | `deno test` and `deno lint` for the site (`web/tests/`). xUnit with `WebApplicationFactory` for the Api's CORS. `deno test` for the edge function's CORS. A quickstart for real sign-in on each provider and a delete-and-restore with a test account. |
| **Performance** | Static pages under 50 KB each, excluding the vendored library, which loads only on `/account/`. That meets SC-004's under-2-seconds-on-mobile goal. |
| **Constraints** | `script-src 'self'`, and no third-party requests from the browser other than Supabase and the Api. Home, Privacy and Support load no JavaScript. |

## Design

### Site layout

```text
web/
├── public/                         # Cloudflare Pages output directory, served as is
│   ├── index.html                  # Home
│   ├── privacy/index.html          # Privacy policy
│   ├── support/index.html          # Support and FAQ
│   ├── account/index.html          # Account page shell, <noscript> fallback
│   ├── css/site.css                # light and dark via prefers-color-scheme
│   ├── js/config.js                # public backend config (contracts/site.md)
│   ├── js/account-state.js         # pure: views, DELETE rule, messages
│   ├── js/account-api.js           # fetch wrapper for 016's endpoints and apple-authorization
│   ├── js/account.js               # DOM wiring: Supabase client, events, rendering
│   ├── vendor/supabase.js          # pinned UMD build
│   ├── images/                     # app icon, store badges
│   ├── _headers                    # CSP and security headers; no-store on /account/*
│   └── _redirects                  # /delete-account → /account/
├── tests/                          # deno test, not deployed
├── vendor.lock                     # supabase-js version, URL, SHA-256
└── data-safety.md                  # policy section → Play Data safety mapping (FR-019)
```

- **Shared chrome:** each page repeats the same header and footer, with links to Home, Privacy, Support and Account, plus "© Palehorse, LLC" and `info@palehorse.net`. `pages.test.js` checks that all four copies are identical, which replaces a template step (research D2).
- **Store links:** Home links to Google Play only until 013 ships the App Store listing.

### Account page

- **Wiring:** `account.js` creates the Supabase client (research D4) and subscribes to `onAuthStateChange`. On each session change, it fetches the status and renders `viewFor(...)` (contracts/site.md).
- **Delete flow:**
  1. Delete account loads the preview.
  2. The confirm view shows the owned groups and the date, and enables the button only when `canConfirm`.
  3. The request goes out, and the page shows the scheduled view with the date.
- **Restore** calls `DELETE`, then re-reads the status. A 409 shows the purging view.
- **Apple:** on `SIGNED_IN` with `app_metadata.provider === "apple"` and a `provider_refresh_token`, it calls `apple-authorization` without awaiting the result (research D5).
- **Logic stays testable:** all decisions live in `account-state.js`, and all requests go through `account-api.js`, which takes the `fetch` function and the token as arguments. `account.js` only reads the DOM, calls those two, and renders.

**Accessibility:**

- Each view is a `<section>` with a heading.
- Focus moves to the new view's heading.
- Errors go to an `aria-live="polite"` region.
- The DELETE field has a visible label and `autocomplete="off"`.

### Privacy policy content

The policy is written from:

| Source | Used for |
| --- | --- |
| FR-014 and FR-015 tables | what's collected, and the companies that process it |
| `specs/016-account-deletion/data-retention.md` | deletion and retention |
| FR-017 and FR-018 | no sale or ads, data rights, contact, last updated |

**Sections:**

1. Who we are
2. What we collect and why
3. Who processes it
4. Location
5. Push notifications
6. Retention and deletion
7. Your choices and rights
8. Children
9. Changes
10. Contact

Section 8 says LoopMeet isn't directed at children under 13, as the Play and App Store policies require.

`web/data-safety.md` maps each section to the Play Data safety categories it backs:

- Personal info: name, email, phone
- Photos
- Location: approximate, not collected (processed in transit only)
- App activity: in-app actions
- App info and performance
- Device or other IDs

### Api CORS (`src/LoopMeet.Api/Program.cs`)

```csharp
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Site", policy => policy
    .WithOrigins(allowedOrigins)
    .SetIsOriginAllowedToAllowWildcardSubdomains()
    .WithMethods("GET", "POST", "DELETE")
    .WithHeaders("Authorization", "Content-Type")));
// …
app.UseCors("Site");   // after the correlation-id middleware, before UseAuthentication
```

`AccountDeletionGateMiddleware` and authentication run after CORS, so a 401 still carries the CORS headers and the page can tell a lost session from a network failure. Origins come from configuration (contracts/site.md), and DO App Platform can override them with `Cors__AllowedOrigins__N`.

### `apple-authorization` CORS

- **Where:** `supabase/functions/apple-authorization/cors.ts` exports `corsHeadersFor(origin, allowed)` and `parseOrigins(csv)`.
- **How:** `index.ts` wraps the handler. It answers an allowed `OPTIONS` with 204 and adds the headers to every response for an allowed origin.
- **Unchanged:** `handler.ts` stays as it is, and requests with no `Origin` header, like the app's, behave exactly as before.
- **Why it isn't shared:** it lives in the function folder and not `_shared/` because only this function is called from a browser (Constitution IV).

### Observability

- **Server side:** the deletion endpoints already log `AccountDeletionRequested`, `AccountDeletionRestored` and so on, keyed by user id. Calls from the site appear in those logs, identical to calls from the app.
- **Edge function:** logs `apple_authorization_stored` with the client id, which is how the quickstart tells a web token from an app token.
- **Site:** has no server of its own and adds no analytics, as the policy promises.

## Constitution Check

*GATE: evaluated against Meetloop Constitution v0.1.0.*

| Gate | Principle | Status | Notes |
| ------ | ----------- | -------- | ------- |
| G1 | I. Code Quality | PASS | Small modules with one job each: state, Api calls, DOM. No dead code or TODOs. The vendored library is pinned and hash-checked, not edited. |
| G2 | II. Tests Required | PASS | Unit tests cover the Account logic and request wrapper. Page and policy checks cover the content. Integration tests cover the Api and edge-function CORS. The quickstart covers real sign-in. Everything automated runs in CI, through the new `web` key. |
| G3 | III. UX First | PASS | The spec's user stories and acceptance scenarios drive the views table. Messages are fixed and actionable (FR-013), the page works with the keyboard and a screen reader, and three pages need no JavaScript. |
| G4 | IV. Simplicity | PASS | No build, framework or npm. CORS uses ASP.NET Core's built-in middleware. The edge function's CORS helper isn't shared, because it has a single use. |
| G5 | V. Modularity | PASS | `web/` is a new, self-contained module that talks to the backend only through the published contracts (016's Api contract and contracts/site.md). The Api and edge function changes are additive. |
| G6 | VI. Contract-First | PASS | `contracts/site.md` defines the config, the views, the calls and the CORS behaviour before any code. No existing contract changes. |
| G7 | VII. Observability | PASS | The existing structured logs cover every server-side action from the site. No new server flows. |
| G8 | Privacy & retention constraints | PASS | This feature publishes the privacy and retention rules. The site stores only the session, in `sessionStorage`, and sends nothing to third parties (vendored library, CSP). |
| G9 | Quality gates | PASS | The new `Run Website Tests` job is gated by Tests Gate. `deno lint` runs on `web/`. The ci-scope rules and their test cases cover `web/` and `data-retention.md`. |

## Project Structure

```text
web/                                                        # new, see "Site layout"
src/LoopMeet.Api/Program.cs                                 # AddCors + UseCors("Site")
src/LoopMeet.Api/appsettings.json                           # Cors:AllowedOrigins
src/LoopMeet.Api/appsettings.Development.json               # localhost:4507
tests/LoopMeet.Api.Tests/Cors/SiteCorsTests.cs              # new
supabase/functions/apple-authorization/cors.ts              # new
supabase/functions/apple-authorization/cors.test.ts         # new
supabase/functions/apple-authorization/index.ts             # wraps the handler
supabase/config.toml                                        # localhost:4507/account/ redirect
.github/scripts/ci-scope.sh, ci-scope.test.sh               # web key and rules
.github/workflows/tests.yml                                 # Run Website Tests job, gate
specs/017-loopmeet-site/quickstart.md
```

## Delivery

| PR | Contents | Mergeable on its own because |
| --- | --- | --- |
| A | US2 and US4: Home, Privacy, Support, shared chrome, CSS, `_headers`, `_redirects`, `data-safety.md`, page and policy tests, CI `web` key | Three static pages can go live and give Play a privacy URL right away. The Account page link shows a "coming soon" note until PR B. |
| B | US1 and US3: the Account page, vendored supabase-js, Api CORS, `apple-authorization` CORS, the redirect URL, their tests | It completes the web deletion URL Play needs. It needs the Cloudflare project and the Supabase redirect URLs set up first. |

## Out of scope

- Terms of Service.
- The palehorse.net redirect.
- Updating the store listings.
- Renaming the app's `Staging` build configuration and `staging/**` deploy tags now that the backend is production.
- A separate staging backend.
