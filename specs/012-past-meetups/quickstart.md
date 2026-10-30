# Quickstart: Past Meetups — Device Validation

**Feature**: 012-past-meetups | **Date**: 2026-10-30

Automated tests cover classification, paging, validation and push-suppression logic. This checklist covers what only a real device and the real notification pipeline can show: the toggle look, scrolling to load more, pull-to-refresh, and whether a push actually arrives. Run it on staging (Api and edge functions deployed from this branch) on **one iOS device and one Android device**.

## Setup

1. Two accounts:
   - **Owner (A)**: signs in on device 1.
   - **Member (B)**: signs in on device 2, with notifications **allowed**.
2. A owns group **"012 QA"**, and B is a member.
3. Seed past meetups directly in Supabase (the app can't create past meetups). Use the SQL editor, logged in as the project owner:

   ```sql
   -- 45 past meetups, one every 2 days, newest 3 days ago; plus one tied pair
   insert into meetups (group_id, created_by_user_id, title, scheduled_at, timezone)
   select '<group-id>', '<owner-id>', 'Past #' || g, now() - (g * interval '2 days') - interval '1 day', 'America/Los_Angeles'
   from generate_series(1, 45) g;
   insert into meetups (group_id, created_by_user_id, title, scheduled_at, timezone)
   select '<group-id>', '<owner-id>', 'Tie ' || g, date_trunc('hour', now()) - interval '10 days', 'America/Los_Angeles'
   from generate_series(1, 2) g;
   ```

4. Using the app as A, create one meetup **later today** (call it "Tonight") and one **next week** ("Next week").
5. Using the SQL editor, create one meetup that **started 1 hour ago** today in `America/Los_Angeles` ("Started"). Confirm that the current LA time is after 01:00, so it really is still today there.

## Matrix

Mark each row ✅/❌ for iOS and Android.

| # | Steps | Expected | iOS | Android |
| --- | --- | --- | --- | --- |
| 1 | B opens "012 QA" | Toggle is visible with **Upcoming Meetups** selected. List shows Started, Tonight, Next week in that order | | |
| 2 | B taps **Past Meetups** | Members and group name don't flicker or reload. Past #1 is first, newest first. "Started" is **not** in this list | | |
| 3 | B scrolls to the bottom repeatedly | More meetups load automatically until Past #45 is shown. The two "Tie" meetups each appear exactly once. No duplicates anywhere | | |
| 4 | Turn on airplane mode, scroll to the end of a partially loaded list | Loaded items stay, and a "tap to retry" row appears. Turn airplane mode off and tap it: loading continues | | |
| 5 | B pulls down to refresh while on **Past** | Past list restarts from Past #1. Upcoming isn't reloaded (check the Api logs: only the `/meetups/past` request) | | |
| 6 | B taps a past meetup | Detail opens with full information and **no edit control** (B isn't the owner) | | |
| 7 | B goes back, leaves the group page, reopens it | Toggle is back on **Upcoming** | | |
| 8 | A opens a past meetup → edit → change the **location only** → save | Save succeeds with no "must be in the future" error. A returns to the page with **Past** still selected and the new location shown. **B receives no push** within 5 minutes | | |
| 9 | A edits a past meetup → change the date to **another past date** → save | Save succeeds, and the meetup moves to its new place in the past list. **B receives no push** | | |
| 10 | A edits a past meetup → change the date to **tomorrow** → save | Save succeeds. The meetup leaves Past and shows in Upcoming on Group Detail and on B's Home tab (within 30 s). **B receives "LoopMeet - Meetup Updated"** | | |
| 11 | A swipes to delete a past meetup | It disappears from the past list. **B receives no push** | | |
| 12 | A edits "Started" (title only) → save | Save succeeds. **B receives the "Meetup Updated" push** (it's still upcoming) | | |
| 13 | A edits "Started" → change the time to 30 minutes ago → save | Rejected with "Scheduled time must be in the future." | | |
| 14 | After LA midnight (or by changing "Started"'s `scheduled_at` to yesterday in SQL), refresh both lists | "Started" is only in **Past**, and no longer on B's Home tab | | |
| 15 | New group with no meetups. Open it and tap both toggle options | Toggle stays visible. Each option shows its own empty message | | |

## Log check (push suppression)

For rows 8, 9 and 11, open the Supabase dashboard → Edge Functions → `notifications-dispatch` → Logs:

- Rows 8 and 9: one `notification_suppressed` entry with `reason: "past_meetup"` and the meetup id.
- Row 11: a response with `reason: "unsupported_event"` (deletes never notify). No `notification_suppressed` line is expected.
- Rows 10 and 12: no suppression entry, and a normal dispatch result.

## Cleanup

```sql
delete from meetups where group_id = '<group-id>' and (title like 'Past #%' or title like 'Tie %');
```
