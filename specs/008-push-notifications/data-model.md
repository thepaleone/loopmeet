# Data Model: Push Notification Flows

## 1) user_devices
Tracks push-capable installations and user preference state.

Fields:
- `id` (uuid, PK)
- `user_id` (uuid, FK -> `auth.users.id`, required)
- `onesignal_player_id` (text, unique nullable while pending registration)
- `onesignal_external_id` (text, required; canonical string of `user_id`)
- `platform` (text, required; `ios` | `android`)
- `device_locale` (text, nullable)
- `device_timezone` (text, nullable IANA)
- `notifications_enabled` (boolean, required, default `true`)
- `permission_state` (text, required; `unknown` | `granted` | `denied`)
- `last_seen_at` (timestamptz, required)
- `invalidated_at` (timestamptz, nullable)
- `created_at` (timestamptz, required)
- `updated_at` (timestamptz, required)

Constraints:
- Unique active registration per `(user_id, onesignal_player_id)` when `invalidated_at is null`
- `onesignal_external_id = user_id::text`
- `permission_state = denied` implies `notifications_enabled = false`

## 2) notification_events
Canonical event ledger for outbound notifications.

Fields:
- `id` (uuid, PK)
- `event_id` (text, unique; idempotency key source)
- `notification_type` (text, required)
- `target_kind` (text, required; `group` | `meetup` | `home` | `invitations`)
- `target_id` (uuid/text nullable for `home`)
- `fallback_route` (text, required)
- `source_table` (text, required)
- `source_row_id` (uuid/text, required)
- `occurred_at` (timestamptz, required)
- `payload_json` (jsonb, required)
- `created_at` (timestamptz, required)

State transitions:
- `created` -> `dispatched` -> (`delivered` when known) -> (`opened` when app reports)
- Terminal error path: `dispatch_failed`

## 3) notification_delivery_attempts
Per-recipient send attempts and provider responses.

Fields:
- `id` (uuid, PK)
- `notification_event_id` (uuid, FK -> `notification_events.id`)
- `user_id` (uuid, FK -> `auth.users.id`)
- `device_id` (uuid, FK -> `user_devices.id`, nullable if user-level send)
- `onesignal_message_id` (text, nullable)
- `status` (text, required; `queued` | `sent` | `failed`)
- `provider_status_code` (int, nullable)
- `provider_response` (jsonb, nullable)
- `error_code` (text, nullable)
- `attempted_at` (timestamptz, required)

Constraints:
- Unique on `(notification_event_id, user_id)` for non-repeatable event types

## 4) notification_open_events
Records app-side open/navigation outcomes for observability.

Fields:
- `id` (uuid, PK)
- `event_id` (text, required)
- `user_id` (uuid, FK -> `auth.users.id`, nullable for pre-auth open)
- `navigation_result` (text, required; `resolved` | `fallback` | `deferred_auth` | `failed`)
- `resolved_route` (text, nullable)
- `opened_at` (timestamptz, required)

## 5) Notification Destination Mapping (code contract)
Canonical map keyed by `notification_type`:
- `invitation.new` -> `PendingInvitations`
- `meetup.created` -> `GroupDetail`
- `meetup.updated` -> `GroupDetail`
- `meetup.canceled` -> `GroupDetail`
- `meetup.today_reminder` -> `Home`

Validation rules:
- Each mapped type must define `target_kind`, destination route id, and fallback route id.
- App and Edge Function maps must pass contract parity tests.
