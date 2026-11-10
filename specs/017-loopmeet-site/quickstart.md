# Quickstart: LoopMeet Website

There's one backend, the one the app uses (research D1). Every Account-page check uses a **test account**, never a real user's.

## One-time setup

### Cloudflare (PR A)

The site is a Worker, `loopmeet-account`, serving `web/public` as static assets (research D11).

| Setting | Value | Checked |
| --- | --- | --- |
| Worker address | `https://loopmeet-account.joelgray.workers.dev` | ✓ |
| Custom domain | `https://account.loopmeet.io` | ✓ |
| Production branch | `main` | ✓ |
| Builds for non-production branches | on, so PR B's branch gets `https://<branch>-loopmeet-account.joelgray.workers.dev` | ✓ |
| Preview URLs (**Settings → Domains & Routes**) | enabled | ✓ |
| Build watch paths | include `web/**` | ✓ |

### Backend (PR B)

1. **Supabase → Authentication → URL Configuration → Redirect URLs**: add `https://account.loopmeet.io/account/`, `https://loopmeet-account.joelgray.workers.dev/account/` and `https://*-loopmeet-account.joelgray.workers.dev/account/`. Leave the Site URL as it is.
2. **Supabase secrets**: `supabase secrets set SITE_ORIGINS="https://account.loopmeet.io,https://loopmeet-account.joelgray.workers.dev,https://*-loopmeet-account.joelgray.workers.dev"`, then `supabase functions deploy apple-authorization`.
3. **The Api**: deploy a build that includes PR B (`staging/**` tag). `Cors:AllowedOrigins` comes from `appsettings.json`, so it needs no new environment variable unless a host changes.
4. **`web/public/js/config.js`**: fill in the Supabase URL, the publishable key and the Api base URL, which is the same one the app's builds use.

### Apple on the web (PR B, or later)

1. In the Apple Developer portal, under **Identifiers**, add a **Services ID**, for example `io.loopmeet.web`. Give it its own identifier. The portal accepts a Services ID named the same as the App ID, `io.loopmeet.app`, but Apple's sign-in page then rejects it with `invalid_request` "Invalid client id or web redirect url".
   - Enable **Sign in with Apple**, with primary App ID `io.loopmeet.app`.
   - **Domains:** the Supabase project host `<project>.supabase.co`.
   - **Return URL:** `https://<project>.supabase.co/auth/v1/callback`.
   - Click **Done**, then **Continue** and **Save**. Done alone doesn't save the domain and Return URL.
2. Generate the Supabase Apple **Secret Key** from your `.p8` file, Team ID, Key ID and the Services ID:
   ```bash
   deno run --allow-read=<path to AuthKey_XXXXXXXXXX.p8> supabase/functions/_tools/apple-client-secret.ts \
     --team-id <TEAM_ID> --key-id <KEY_ID> --client-id <SERVICES_ID> --key-file <path to AuthKey_XXXXXXXXXX.p8>
   ```
   It prints the secret, and its expiry on stderr. Apple allows at most 6 months, so **set a calendar reminder to renew it** a week or so before that date, by rerunning the same command.
3. In **Supabase → Authentication → Providers → Apple**:
   - Set **Client IDs** to `io.loopmeet.web,io.loopmeet.app`. The Services ID must come first: the web flow sends the first entry to Apple as `client_id`. Keep `io.loopmeet.app`, which the iOS app's ID tokens are checked against.
   - Paste the Secret Key.
4. In `config.js`, set `appleWebClientId: "io.loopmeet.web"`. The Apple button appears once this is set.

| Setting | Value | Checked |
| --- | --- | --- |
| Services ID | | |
| Secret Key renewal due | | |

## Local run

```bash
deno run --allow-net --allow-read jsr:@std/http@1/file-server web/public --port 4507
deno lint --ignore=web/public/vendor web/ && deno test --allow-read=web,specs/016-account-deletion web/
```

Pointing the local site at local Supabase means editing `config.js` locally. Don't commit that change.

## Checks

### PR A: static pages

| # | Check | Expected | Result |
| --- | --- | --- | --- |
| 1 | Open the preview URL on a phone, then on a desktop. Visit Home, Privacy and Support. | Each reads well at both sizes, and the header and footer link to all four pages (FR-001, FR-003). | Pass |
| 2 | Turn off JavaScript in the browser and reload the three pages. | They read fully (FR-002). | Pass |
| 3 | On a phone over mobile data, load Home cold. | It's readable in under 2 seconds (SC-004). | Pass |
| 4 | Compare Privacy, side by side, with the Play Data safety form, using `web/data-safety.md`. | No category or processor is missing or contradicts the form (SC-003). | Pass |
| 5 | Switch the device to dark mode. | Pages stay readable with good contrast. | Pass |
| 6 | After merge, open `https://account.loopmeet.io/privacy/` and `https://account.loopmeet.io/delete-account`. | The policy loads, and `/delete-account` lands on `/account/`. | Pass |

### PR B: Account page (test accounts only)

| # | Check | Expected | Result |
| --- | --- | --- | --- |
| 7 | Sign in with the email and password of test account **T1**. | The page shows T1's email and "not scheduled" (US1-1). | Pass |
| 8 | Delete account. Type `delete`, then `DELETE`. | The button stays disabled for `delete` and enables only for `DELETE`. Owned groups show with member counts, and the date is 30 days out (US1-2). | Pass |
| 9 | Confirm. | The page shows the date. In the app, signing in as T1 shows the scheduled-deletion screen, and another member no longer sees T1's groups (US1-3, SC-002). | Pass |
| 10 | Reload the Account page. | It shows the scheduled view with the same date. Reloading didn't cancel it (US1-4). | Pass |
| 11 | Restore account. | The page shows active. In the app, T1 and their groups are back for everyone (US3-1). | Pass |
| 12 | Request deletion in the **app** as T1, then sign in on the web. | The scheduled view shows, with the app's date (US3, FR-010). | Pass |
| 13 | Sign out. | The page returns to sign-in. In DevTools → Application, `sessionStorage` has no Supabase keys, and `localStorage` has none (FR-012). | Pass |
| 14 | Sign in with a wrong password, then cancel a Google sign-in. | Short, specific messages, and nothing changes (US1-5, FR-013). | Pass |
| 15 | Sign in with **Google** as test account T2. | It returns to `/account/` on the same host, preview or live (FR-022), and shows T2's email. | Pass |
| 16 | On a preview URL, repeat check 15. | It returns to the preview's own `/account/`. | Pass |
| 17 | Turn off the network and choose Delete account. | "Couldn't reach LoopMeet…" shows, with no raw error. | Pass |
| 18 | In the browser console on another site, for example `example.com`, run `fetch("<api>/users/account-deletion")`. | The browser blocks it with a CORS error (FR-021, SC-006). | Pass |
| 19 | Turn off JavaScript and open `/account/`. | It says JavaScript is needed and gives `info@palehorse.net` for deletion requests. | Pass |
| 20 | Time checks 7–9 from opening the live site. | Under 2 minutes (SC-001). | Pass |
| 21 | Once the custom domain works (rows 25–27), set Play Console's account-deletion URL to `https://<domain>/account/` and the privacy policy URL to `https://<domain>/privacy/`. | Play accepts both (SC-005). | Pass |

### Apple on the web, once set up

| # | Check | Expected | Result |
| --- | --- | --- | --- |
| 22 | Sign in with **Apple** as test account T3. | It returns to `/account/` and shows T3's email, or Apple's relay address. | |
| 23 | Check the database: `select client_id from apple_authorizations where user_id = '<T3>';`. | There's a row with `client_id = io.loopmeet.web`, and the function logs show `apple_authorization_stored` (FR-011). | |
| 24 | Request deletion as T3, then force the purge (016 quickstart). | LoopMeet disappears from T3's Apple ID → Sign in with Apple list. | |

### Live site, after PR B merges

Checks 7–21 run on PR B's preview. These repeat the key ones on the live hosts.

| # | Check | Expected | Result |
| --- | --- | --- | --- |
| 25 | Open `https://account.loopmeet.io/account/` in a private window. | The sign-in options show, and the response has `Cache-Control: no-store`. | |
| 26 | Repeat checks 7, 15 and 22 on `https://account.loopmeet.io/account/`. | Sign-in returns to the domain, and the status loads, so CORS allows the domain. | |
| 27 | Repeat check 7 on `https://loopmeet-account.joelgray.workers.dev/account/`. | It works there too. | |
