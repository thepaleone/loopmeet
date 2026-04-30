# Contract: Notification Payload (`additional_data`)

Required keys for all push types:

```json
{
  "notification_type": "meetup.created",
  "target_kind": "group",
  "target_id": "2df0dd26-8b2f-43da-b9f4-91ae8ffb8f5c",
  "fallback_route": "home",
  "event_id": "meetup:created:6f8b0a52",
  "sent_at": "2026-04-30T14:52:00Z"
}
```

Rules:
- `notification_type`: enum (`invitation.new`, `meetup.created`, `meetup.updated`, `meetup.canceled`, `meetup.today_reminder`)
- `target_kind`: enum (`invitations`, `group`, `home`)
- `target_id`: required for `group`, optional/null for `home` and `invitations`
- `fallback_route`: enum (`home`, `pending_invitations`)
- `event_id`: globally unique per outbound event
- `sent_at`: RFC3339 UTC timestamp

Compatibility:
- New notification types must preserve these keys.
- New optional keys are additive only.
