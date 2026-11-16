# Contract: `report-email` Edge Function

**Location:** `supabase/functions/report-email/`. It holds:

- `index.ts`, the HTTP entry;
- `report-mailer.ts`, the pure logic: claim, render, send and mark, through injected ports, as `account-purge/purge-runner.ts` does;
- `report-mailer.test.ts`;
- `deno.json`.

The tests import `jsr:` and `npm:` specifiers directly (CLAUDE.md gotcha).

**Config:** `supabase/config.toml` gets `[functions.report-email] verify_jwt = false` (research D4). It uses the following secrets:

- `SUPABASE_URL`, `SUPABASE_SERVICE_ROLE_KEY` (built in);
- `SENDGRID_API_KEY`;
- `REPORT_EMAIL_FROM`, which is `noreply@loopmeet.io`;
- `REPORT_EMAIL_TO`, which is `abuse@loopmeet.io`.

## Input

`POST /functions/v1/report-email`

| Body | Caller | Meaning |
| --- | --- | --- |
| `{ "report_id": "uuid" }` | the `abuse_reports` insert trigger | send this report if it is still unsent and unclaimed |
| `{}` | the `report-email-sweep` cron job, every 5 minutes | send up to 25 unsent, unclaimed reports, oldest first, then log any stuck ones |

Any other body returns `400`. The response is `200 { "sent": n, "failed": n, "stuck": n }`. The function returns `200` even when a send fails, because the failure is recorded on the row and retried by the sweep.

## Steps

1. Claim the rows with `claim_report_emails(report_id, 25)`. A row already sent or leased within 10 minutes isn't returned.
2. For each row, read `user_profiles.email` for `reporter_user_id`. If there's none, the email goes without a `reply_to`.
3. Render the email (below), then `POST https://api.sendgrid.com/v3/mail/send` with `Authorization: Bearer ${SENDGRID_API_KEY}`.
4. On `202`, call `mark_report_email(id, null)`. On any other status or a network error, call `mark_report_email(id, "<code>")`. The code is `http_<status>` or `network`.
5. For a sweep (`{}`) only, call `stuck_report_emails()` and log `report_email_stuck { report_id, received_at }` for each row.

## Email

- **From:** `noreply@loopmeet.io` (`REPORT_EMAIL_FROM`).
- **To:** `abuse@loopmeet.io` (`REPORT_EMAIL_TO`).
- **Reply-To:** the reporter's account email, when available.
- **Subject:** `LoopMeet report <first 8 characters of id>: <reason label>`. It contains no user-written text.
- **Body (`text/plain`):**

```text
Report <id>
Received: <received_at, ISO 8601 UTC>
Reason: <reason label>

Group: <group_name> (<group_id>)
Reported: <reported_display_name> (<reported_user_id>) | The group itself
Reporter: <reporter_display_name> (<reporter_user_id>), <reporter email or "email unavailable">

Details:
<details>
```

The email holds no tokens, links that sign anyone in, or internal URLs.

## Logs

The function logs only these events, as structured JSON:

- `report_email_sent { report_id }`;
- `report_email_failed { report_id, error_code, attempts }`;
- `report_email_stuck { report_id, received_at }`.

It never logs emails, names, details or the SendGrid response body.

## Tests (`report-mailer.test.ts`, Deno)

- **Rendering:** the body has every field above. The subject has no group name or details. A null reported user renders "The group itself", and a missing reporter email drops `reply_to`.
- **Sending:** a `202` marks the row sent. Any other status, or a network error, marks the failure code, and the rows after it still go.
- **Request body:** a `report_id` body claims only that row. `{}` claims a batch and checks for stuck rows. Any other body gives `400`.
- **Logs:** no log call receives an email, a name or the details.
