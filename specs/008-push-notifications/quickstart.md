# Quickstart: Push Notification Implementation

## Phase A: Database

1. Add migration for `user_devices`, `notification_events`, `notification_delivery_attempts`, `notification_open_events`.
2. Add indexes/constraints for idempotency and active-device lookup.
3. Add RLS policies:
   - Users can read/update only their own `user_devices` preference state.
   - Service-role only writes for event/delivery audit tables.

Core SQL sketch:

```sql
create table public.user_devices (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references auth.users(id) on delete cascade,
  onesignal_player_id text,
  onesignal_external_id text not null,
  platform text not null,
  notifications_enabled boolean not null default true,
  permission_state text not null default 'unknown',
  device_timezone text,
  last_seen_at timestamptz not null default now(),
  invalidated_at timestamptz,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  constraint user_devices_external_id_ck check (onesignal_external_id = user_id::text)
);
```

## Phase B: Backend (Edge + Webhooks)

1. Create `supabase/functions/notifications-dispatch`.
2. Implement webhook payload parser and `notification_type` resolver.
3. Resolve recipients using membership + invitation ownership rules.
4. Build canonical `additional_data` payload and send to OneSignal REST.
5. Persist send attempts and provider responses.
6. Add `supabase/functions/reminders-scheduler` invoked every 15 minutes.

Core TypeScript sketch:

```ts
const destinationMap = {
  "invitation.new": { targetKind: "invitations", fallbackRoute: "pending_invitations" },
  "meetup.created": { targetKind: "group", fallbackRoute: "home" },
  "meetup.updated": { targetKind: "group", fallbackRoute: "home" },
  "meetup.canceled": { targetKind: "group", fallbackRoute: "home" },
  "meetup.today_reminder": { targetKind: "home", fallbackRoute: "home" }
} as const;

function buildAdditionalData(type: keyof typeof destinationMap, targetId: string | null, eventId: string) {
  const map = destinationMap[type];
  return {
    notification_type: type,
    target_kind: map.targetKind,
    target_id: targetId,
    fallback_route: map.fallbackRoute,
    event_id: eventId,
    sent_at: new Date().toISOString()
  };
}
```

Webhook conditions to configure:
- `public.invitations`: `INSERT`
- `public.meetups`: `INSERT`
- `public.meetups`: `UPDATE` (send `meetup.updated` when significant fields changed)
- `public.meetups`: `UPDATE` (send `meetup.canceled` when status enters canceled/deleted)

## Phase C: Frontend (MAUI)

1. Add OneSignal SDK wiring and login binding after Supabase auth.
2. Implement `NotificationService`:
   - Permission request timing per FR-014.
   - `NotificationOpened` handler parsing canonical payload.
   - Signed-out intent persistence for post-login redirect.
3. Route mappings:
   - `invitation.new` -> Pending Invitations
   - `meetup.created|updated|canceled` -> Group Detail
   - `meetup.today_reminder` -> Home

Core C# sketch:

```csharp
public async Task OnAuthenticatedAsync(Guid userId)
{
    OneSignal.Default.Login(userId.ToString());
    await _deviceRegistryService.UpsertCurrentDeviceAsync(userId);
}

private async void HandleNotificationOpened(INotification notification)
{
    var data = notification.AdditionalData;
    var payload = NotificationPayload.FromDictionary(data);

    if (!_authState.IsSignedIn)
    {
        await _pendingIntentStore.SaveAsync(payload);
        await _navigation.GoToSignInAsync();
        return;
    }

    await _notificationNavigator.NavigateAsync(payload);
}
```

Permission UX:
- Trigger prompt after successful sign-in or first notification-relevant action.
- If denied, show in-app "Enable notifications" action that opens OS settings.
- Never block core app usage.

## Phase D: QA

1. Contract tests for webhook payload parsing and canonical `additional_data` keys.
2. Integration tests for each of the five notification types.
3. Client tests for:
   - Permission states (`unknown`, `granted`, `denied`)
   - Signed-out notification tap -> post-login redirect
   - Invalid/missing target -> fallback route + user message
4. Reminder window verification across at least 3 timezones.
