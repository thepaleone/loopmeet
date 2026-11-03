# Quickstart: Account Deletion

## Run the database tests locally

```bash
# With podman instead of Docker Desktop, point the Supabase CLI at podman's socket first:
export DOCKER_HOST="unix://$(podman machine inspect --format '{{.ConnectionInfo.PodmanSocket.Path}}')"
supabase start
supabase db reset          # applies every migration, including <timestamp>_account_deletion.sql
supabase test db           # runs supabase/tests/database/*.sql (pgTAP)
supabase stop
```

## Deploy to staging

1. Apply the migration: `supabase db push` (or the existing migration path for staging).
2. Set the new edge secrets: `supabase secrets set APPLE_TEAM_ID=… APPLE_KEY_ID=… APPLE_PRIVATE_KEY="$(cat AuthKey_XXXX.p8)" APPLE_CLIENT_ID=io.loopmeet.app AVATAR_BUCKET_NAME=<same as the Api's Supabase:AvatarBucketName>`.
3. Deploy the functions: `supabase functions deploy account-purge apple-authorization notifications-dispatch reminders-scheduler`.
4. Confirm the cron job exists: `select jobname, schedule from cron.job where jobname = 'account-purge-hourly';`.
5. Deploy the Api (`staging/**` tag) and install an app build that includes 016.

| Setting | Staging value | Checked |
| --- | --- | --- |
| Api `Supabase:AvatarBucketName` | | |
| Edge `AVATAR_BUCKET_NAME` | | |

## Speed up the 30 days for testing

On staging only, move a test user's date into the past, then trigger a run instead of waiting for the hourly job:

```sql
update account_deletions set purge_after = now() - interval '1 minute' where user_id = '<test user id>';
```

```bash
curl -X POST "$SUPABASE_URL/functions/v1/account-purge" -H "Authorization: Bearer $PUBLISHABLE_KEY" -d '{}'
```

## Device checks

Use three test accounts:
- **A** owns group G with member B, and is a member of group H owned by C.
- **B**
- **C**

| # | Check | Expected | Result |
| --- | --- | --- | --- |
| 1 | A: Profile → Delete Account | Screen lists G with "2 members" and the date 30 days out. Delete is disabled. Time from Profile to confirmed request is under 1 minute (SC-001). | |
| 2 | Type `delete`, then `DELETE` followed by a space | Delete stays disabled | |
| 3 | Type `DELETE`, tap Delete | The date is shown, and after acknowledging, A is on the sign-in screen | |
| 4 | B: refresh Home and Groups (wait 30 s) | G and its meetups are gone. A's invitation to B is gone. | |
| 5 | C: open H | A isn't in H's members and the count dropped by 1 | |
| 6 | A was also signed in on a second device: open the app there | The scheduled-deletion screen with the date. Nothing else is reachable. | |
| 7 | A signs in again → Sign out | Signed out, and the deletion is still scheduled (row unchanged) | |
| 8 | A signs in again → Restore account | Home. G, meetups, memberships and invitations are all back for A, B and C. | |
| 9 | Request again, then force the date and run the purge | `account_deletions` row gone, A can't sign in, B no longer has G, A's avatar folder is empty, and OneSignal has no user with external id A | |
| 10 | Run the purge again | `{"claimed":0}` and no errors | |
| 11 | Sign up again with A's email | A new, empty account | |
| 12 | Push: create a meetup in H while A is pending | B and C get it, A doesn't | |
| 13 | iOS only (after 013): Sign in with Apple, then request deletion and force the purge | An `apple_authorizations` row exists after sign-in. After the purge, LoopMeet is gone from Settings → Apple ID → Sign in with Apple. | |
