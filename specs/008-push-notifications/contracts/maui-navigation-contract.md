# Contract: MAUI Notification Navigation

Input: canonical notification payload from OneSignal `additional_data`.

Routing rules:
- `invitation.new` -> `//Invitations/Pending`
- `meetup.created` -> `//Groups/Detail?groupId={target_id}`
- `meetup.updated` -> `//Groups/Detail?groupId={target_id}`
- `meetup.canceled` -> `//Groups/Detail?groupId={target_id}`
- `meetup.today_reminder` -> `//Home`

Fallback behavior:
- If `target_id` missing for `group` target kinds, route to `//Home` and display friendly error message.
- If user is signed out, persist intent and resume route after successful sign-in.

Permission behavior:
- Request permission only after first sign-in or first notification-relevant action.
- If denied, expose `Enable notifications` action opening OS app notification settings.
