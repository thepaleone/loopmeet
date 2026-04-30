# Contract: Supabase Webhook -> `notifications-dispatch` Edge Function

Endpoint input (normalized):

```json
{
  "type": "INSERT",
  "table": "meetups",
  "schema": "public",
  "record": {
    "id": "6f8b0a52-2ddf-465f-90aa-7a77de8f89bd",
    "group_id": "2df0dd26-8b2f-43da-b9f4-91ae8ffb8f5c",
    "starts_at": "2026-05-01T16:00:00Z",
    "status": "scheduled"
  },
  "old_record": null,
  "timestamp": "2026-04-30T14:50:00Z"
}
```

Trigger matrix:
- `invitations` `INSERT` -> `invitation.new`
- `meetups` `INSERT` -> `meetup.created`
- `meetups` `UPDATE` with meaningful data change and not canceled -> `meetup.updated`
- `meetups` `UPDATE` where status transitions to canceled/deleted marker -> `meetup.canceled`

Edge response contract:

```json
{
  "event_id": "meetup:created:6f8b0a52",
  "notification_type": "meetup.created",
  "recipient_count": 12,
  "send_attempts": 12,
  "failed_attempts": 0
}
```

Failure contract:
- 4xx for invalid payload/schema
- 5xx for downstream dispatch failure
- Idempotent reprocessing by `event_id`
