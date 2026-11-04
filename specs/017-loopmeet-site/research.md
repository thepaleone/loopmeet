# Research: LoopMeet Website

**Feature**: 017-loopmeet-site · **Date**: 2026-11-04

Each decision lists what was chosen, why, and what was rejected.

## D1. One backend for the site and its previews

**Decision**: The site has one configuration. The live site and every Cloudflare preview use the backend the app uses: the one called staging until now, promoted to production.

**Rationale**: Only one backend exists. A second one would mean a second Supabase project, Api deployment, Apple and Google setup, and OneSignal app, all for previews of a four-page site. With one configuration the site needs no build step, so what's in `web/public/` is exactly what's served.

**Accepted risk**: a preview's Account page acts on real accounts. Every Account page action is the signed-in user acting on their own account behind the typed DELETE confirmation, and a request can be undone for 30 days. Previews are checked with test accounts only (quickstart).

**Rejected**: per-environment configuration chosen at build time. There is no second environment to point at.

## D2. Plain static files, no build step

**Decision**: Hand-written HTML, one stylesheet and ES modules in `web/public/`, which is the Cloudflare Pages output directory. Pages runs no build command. Each page is a folder with an `index.html` (`privacy/index.html`), so the same URLs work on Pages and on any local static server.

**Rationale**: It's what was chosen for 017: four pages, three of them static text. Nothing needs compiling, and there's no npm toolchain to maintain or audit.

**Rejected**: a static site generator or framework. Four pages don't justify one, and a shared header and footer repeated four times is cheaper than a template step. A source-inspection test keeps the four copies identical.

## D3. supabase-js is vendored, not loaded from a CDN

**Decision**: The pinned UMD build of `@supabase/supabase-js` 2.x is copied into `web/public/vendor/supabase.js`. A `web/vendor.lock` file records its version, source URL and SHA-256, and a test checks the hash.

**Rationale**:

- **Privacy:** loading it from jsDelivr would send every Account-page visitor's IP address to another company, which the policy would then have to name (FR-015). Vendoring keeps the processor list at Cloudflare.
- **Security:** the Content-Security-Policy can be `script-src 'self'`. Nothing outside our own origin can run on the page that holds a signed-in session.
- **Stability:** the page can't break because a CDN is down or a version changed.

**Rejected**: a jsDelivr or esm.sh `<script>` with Subresource Integrity. SRI covers integrity, but not the extra processor or the outage.

## D4. Sign-in uses Supabase Auth in the browser with PKCE

**Decision**: `createClient(url, publishableKey, { auth: { flowType: "pkce", storage: sessionStorage, persistSession: true, detectSessionInUrl: true } })`.

| Method | Call |
| --- | --- |
| Email and password | `auth.signInWithPassword({ email, password })` |
| Google | `auth.signInWithOAuth({ provider: "google", options: { redirectTo: origin + "/account/" } })` |
| Apple | `auth.signInWithOAuth({ provider: "apple", options: { redirectTo: origin + "/account/" } })` |

- **Same providers as the app.** The app already signs in with Google through Supabase's browser redirect (`AuthService.cs`), so the Google provider needs only new redirect URLs. The app's Apple sign-in is native (ID token). The web uses Supabase's Apple OAuth flow, which needs an Apple Services ID and a Supabase "Secret Key" (D7).
- **`redirectTo` is the page's own origin,** so previews return to themselves (FR-022). Supabase only honours URLs on its allow list (D8), so visitors can't redirect sign-in elsewhere.
- **`sessionStorage`, not `localStorage`:** the session ends when the tab closes, which keeps FR-012's "nothing beyond the sign-in session" as small as it can be. The PKCE code verifier also lives there, and the OAuth redirect returns to the same tab.
- **Sign out** calls `auth.signOut()`, which clears that storage.

**Rejected**: the implicit flow, which puts tokens in the URL fragment and browser history.

## D5. Apple's refresh token reaches 016's `apple-authorization`

**Decision**: When the page sees the `SIGNED_IN` event from an Apple sign-in and `session.provider_refresh_token` is present, it posts `{ refreshToken, clientId: APPLE_WEB_CLIENT_ID }` to `/functions/v1/apple-authorization` with the user's access token. A failure is logged to the console and ignored (FR-011).

**Rationale**:

- **016 already accepts it:** `handler.ts` takes `{refreshToken, clientId}` as its 017 path.
- **The purge already handles it:** it revokes each stored row with that row's `client_id` (`purge-runner.ts`), and `createClientSecret` signs for any client id.
- **Only on `SIGNED_IN`:** Supabase includes `provider_refresh_token` only in the session from the code exchange. It isn't stored, so a page reload never resends it.

**Change needed**: the function must answer CORS (D6), because a browser sends a preflight `OPTIONS` first, which today returns 405.

**Rejected**: sending Apple's authorization code instead. Supabase has already used the code by the time the page sees the session.

## D6. CORS: the Api and `apple-authorization` accept only the site's origins

**Decision**:

- **Api**: a named ASP.NET Core CORS policy, `Site`, built from the `Cors:AllowedOrigins` configuration array. It's applied with `app.UseCors("Site")` after the correlation-id middleware and before `UseAuthentication`, so preflights and the 401 and 403 responses carry the headers.
  - **Methods:** `GET`, `POST`, `DELETE`.
  - **Headers:** `Authorization`, `Content-Type`.
  - **Credentials:** none (bearer tokens, no cookies).
  - **Wildcards:** `SetIsOriginAllowedToAllowWildcardSubdomains()` admits `https://*.loopmeet.pages.dev` for previews.
- **`apple-authorization`**: a small CORS helper in the function's folder, which only it uses. It reads the same list from the `SITE_ORIGINS` secret, comma-separated, where an entry may use one `*.` wildcard label.
  - **Matching origin:** `OPTIONS` returns 204 with the CORS headers, and every response from the function echoes the origin.
  - **Other origins:** get no CORS headers, and the function answers exactly as today.
  - **No `Origin` header:** the native app sends none, and its behaviour is unchanged.

| Origin | Why |
| --- | --- |
| `https://loopmeet.pages.dev` | the live site until the custom domain is set up |
| `https://*.loopmeet.pages.dev` | Pages previews (`<branch>.` and `<hash>.loopmeet.pages.dev`) |
| the custom domain, likely `https://account.loopmeet.io` | added when it's pointed at the project, after launch |
| `http://localhost:4507` | local development only (`appsettings.Development.json`, local `.env`) |

- **What CORS does here:** other websites can't call the Api from a visitor's browser (FR-021). It doesn't add authentication, which is still the bearer token.
- **Previews:** any branch pushed to this repo gets a preview origin. Only people who can push can create one.
- **Configuration:** the Pages project is named `loopmeet`. If that name is taken, the `pages.dev` host changes, and so does only this configuration.

**Rejected**: `AllowAnyOrigin`, which FR-021 forbids. Proxying the Api through Cloudflare to avoid CORS, which adds a moving part for no gain.

## D7. Apple web sign-in setup is manual and documented

**Decision**: The quickstart lists the one-time steps.

- **Apple Services ID:** create one, for example `io.loopmeet.web`, with Sign in with Apple enabled. Its primary App ID is `io.loopmeet.app`. Register the Supabase project host as a domain, and the Supabase callback `https://<project>.supabase.co/auth/v1/callback` as the return URL.
- **Supabase Apple provider:**
  - Add the Services ID to Client IDs, next to `io.loopmeet.app`.
  - Paste a client secret generated from the existing `.p8` key. It lasts at most 6 months, so the quickstart says to set a renewal reminder.
- **Site:** set `appleWebClientId` in `config.js`.

Until `appleWebClientId` is set, the Account page hides the Apple button, so the site can launch with email and Google.

**Rejected**: a script to generate the 6-month secret. Supabase's documentation already provides a generator, and the existing `createClientSecret` is fixed at 5 minutes for a different purpose.

## D8. Supabase redirect URLs

**Decision**: Add these to the Supabase project's Auth → URL Configuration → Redirect URLs:

- `https://loopmeet.pages.dev/account/`
- the custom domain's `/account/`, once it exists
- `https://*.loopmeet.pages.dev/account/`

Add `http://localhost:4507/account/` to `additional_redirect_urls` in `supabase/config.toml`.

The **Site URL** is left alone, because the app's email confirmation links depend on it.

## D9. Tests run under Deno, which CI already has

**Decision**: The site's tests are `deno test` files in `web/tests/`, linted with `deno lint web/`. They import with `jsr:` specifiers, as the edge-function tests do.

| Test file | What it checks |
| --- | --- |
| `account-state.test.js` | The Account page's logic as pure functions:<br>• status responses to the view to show<br>• the DELETE rule<br>• error responses to user messages (FR-013)<br>• when to show Apple |
| `account-api.test.js` | The request wrapper, using a fake `fetch`:<br>• paths, methods, headers and bodies against 016's contract<br>• the `apple-authorization` call |
| `pages.test.js` | Every page:<br>• has `lang`, a `<title>` and one `<h1>`<br>• has the shared header and footer with the four links and Palehorse, LLC / `info@palehorse.net` (FR-001, FR-004)<br>• has internal links that resolve to files in `web/public/`<br>• loads no scripts on Home, Privacy and Support (FR-002)<br>• the Account page has a `<noscript>` with the email address |
| `privacy-policy.test.js` | The policy names every FR-014 category and FR-015 processor. It states 30 days, 24 hours and the 7-day Supabase backups and logs, matching `specs/016-account-deletion/data-retention.md`, which the test also reads. It has a "Last updated" date. |
| `vendor.test.js` | The vendored file's SHA-256 matches `web/vendor.lock`. |
| `headers.test.js` | `_headers` sets the CSP (`script-src 'self'`), `frame-ancestors 'none'`, `Referrer-Policy` and `X-Content-Type-Options`. |

**Rationale**: Deno already runs the edge-function tests in CI, so this adds no new toolchain or npm dependencies. Keeping the logic in DOM-free modules (`account-state.js`, `account-api.js`), with `account.js` as a thin DOM layer, puts the behaviour under unit test.

- **Not automated:** OAuth redirects and real sign-in, which the quickstart covers.
- **The Api:** CORS gets a `WebApplicationFactory` test in `LoopMeet.Api.Tests`.
- **The edge function:** its CORS gets a Deno test alongside the existing handler tests.

**Rejected**: Playwright or a headless browser. It adds a heavy toolchain to CI to test four small pages, most of which the source checks already cover.

## D10. CI scope

**Decision**: A new `web` key in `.github/scripts/ci-scope.sh`, with a `Run Website Tests` job (`deno lint web/` then `deno test --allow-read=web,specs/016-account-deletion web/`). The Tests Gate checks it too.

| Paths | Selects |
| --- | --- |
| `web/*` | `web` |
| `specs/016-account-deletion/data-retention.md` | `web` |

The data-retention rule has to come before the `*.md` documentation rule, because the policy test reads that file (the CLAUDE.md gotcha about cross-tree reads). Cases for both rules go into `ci-scope.test.sh`.

## D11. Cloudflare Pages configuration

**Decision**: Connect the GitHub repo to a Pages project named `loopmeet`.

| Setting | Value |
| --- | --- |
| Production branch | `main` |
| Build command | none |
| Output directory | `web/public` |
| Build watch paths | include `web/*` |
| Custom domain | added after launch, likely `account.loopmeet.io` |

- **Build watch paths** keep app-only branches from deploying previews.
- **`web/public/_headers`** sets security headers on every page, plus `Cache-Control: no-store` on `/account/*`.
- **`web/public/_redirects`** sends `/delete-account` to `/account/`, a short URL for store forms.

All of this is in the quickstart, since it's dashboard work.
