# Contract: LoopMeet Website

## Site configuration: `web/public/js/config.js`

Every value here is public. There's one configuration for the live site and every preview (research D1).

```js
export const config = Object.freeze({
  supabaseUrl: "https://<project>.supabase.co",
  supabasePublishableKey: "<anon or publishable key>", // public by design; RLS protects data
  apiBaseUrl: "https://<api host>",                    // the app's LOOPMEET_API_BASE_URL
  appleWebClientId: "",                                // the Services ID; "" hides the Apple button
});
```

Visitors can't override any of it. There are no query parameters and nothing read from storage (FR-020).

## Calls the Account page makes

All calls send the signed-in user's `Authorization: Bearer <access token>`. The bodies and responses are exactly [016's Api contract](../../016-account-deletion/contracts/api-account-deletion.md).

| Purpose | Request | Success | Failures the page handles |
| --- | --- | --- | --- |
| Status | `GET {apiBaseUrl}/users/account-deletion` | 200 `{status, requestedAt?, deletesAt?}` | 401 |
| Preview | `GET {apiBaseUrl}/users/account-deletion/preview` | 200 `{deletesAt, ownedGroups[]}` | 401 |
| Request | `POST {apiBaseUrl}/users/account-deletion` `{"confirmation":"DELETE"}` | 202 pending | 400 `invalid_confirmation`, 409 `deletion_in_progress`, 401 |
| Restore | `DELETE {apiBaseUrl}/users/account-deletion` | 200 `{status:"none"}` | 409 `deletion_in_progress`, 401 |
| Apple token | `POST {supabaseUrl}/functions/v1/apple-authorization` `{refreshToken, clientId}` | 204 | any failure is ignored (FR-011) |

The deletion endpoints are exempt from the Api's pending-deletion gate. The site therefore never receives `403 account_pending_deletion`.

## Account page views (`account-state.js`)

`viewFor(session, statusResponse)` returns one of:

| View | When | Shows |
| --- | --- | --- |
| `signed-out` | no session | the sign-in form: email and password, Google, and Apple only if `appleWebClientId` is set |
| `loading` | a session exists and the status isn't loaded yet | a progress line |
| `active` | `status: "none"` | the signed-in email, Delete account, Sign out |
| `confirm` | the user chose Delete account and the preview has loaded | owned groups with member counts, the deletion date, the DELETE field, a delete button enabled only when `canConfirm(text)` |
| `scheduled` | `status: "pending"` | the date, Restore account, Sign out |
| `purging` | `status: "purging"`, or a 409 `deletion_in_progress` | "being deleted, can no longer be restored", Sign out only |
| `error` | the status couldn't be loaded | `messageFor(error)` and Try again |

- **`canConfirm(text)`** is `text === "DELETE"`. It compares exactly, with no trimming and no case folding, as the server does.
- **`messageFor(error)`** maps failures to fixed messages. A raw error never reaches the page (FR-013).

| Failure | Message |
| --- | --- |
| Network | "Couldn't reach LoopMeet. Check your connection and try again." |
| 401 | Signs out, then "Your session ended. Sign in again." |
| 409 | the `purging` view |
| Invalid email or password | "That email and password don't match a LoopMeet account." |
| Provider window cancelled | "Sign-in was cancelled." |
| Anything else | "Something went wrong. Try again, or email `info@palehorse.net`." |

## CORS

### Api: `Cors:AllowedOrigins`

`appsettings.json`:

```json
"Cors": {
  "AllowedOrigins": "https://account.loopmeet.io, https://loopmeet-account.joelgray.workers.dev, https://*-loopmeet-account.joelgray.workers.dev"
}
```

An entry may hold one `*` in its first host label, matching one or more of `a-z`, `0-9` and `-`, never a dot (research D6). Matching ignores case and is otherwise exact: scheme, host and port. `supabase/functions/apple-authorization/site-origins.cases.json` pins the rule for both implementations. The value is one comma-separated string, the same format as `SITE_ORIGINS`, so an override replaces the whole list. (A JSON array would merge by index, and an override of entry 0 would quietly keep the rest.) `appsettings.Development.json` sets `"http://localhost:4507"`, and DO App Platform can override the list with the `Cors__AllowedOrigins` environment variable.

| Request | Response |
| --- | --- |
| Preflight from an allowed origin | 204 with `Access-Control-Allow-Origin: <origin>`, methods `GET, POST, DELETE`, headers `authorization, content-type` |
| Any response to an allowed origin, including 401 | `Access-Control-Allow-Origin: <origin>` |
| A disallowed origin | no `Access-Control-Allow-*` headers |
| No `Origin` header (the app) | unchanged |
| Credentials | never allowed (no `Access-Control-Allow-Credentials`) |

### `apple-authorization`: the `SITE_ORIGINS` secret

The secret holds the same list, comma-separated, with the same `*` rule as the Api.

| Request | Response |
| --- | --- |
| `OPTIONS` from an allowed origin | 204 with `Access-Control-Allow-Origin: <origin>`, `Access-Control-Allow-Methods: POST`, `Access-Control-Allow-Headers: authorization, content-type, apikey, x-client-info`, `Vary: Origin` |
| Any other response to an allowed origin | `Access-Control-Allow-Origin: <origin>` and `Vary: Origin` |
| `OPTIONS` from a disallowed origin | 405, as today, with no CORS headers |
| No `Origin` header | unchanged from 016 |
