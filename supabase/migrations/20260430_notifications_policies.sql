alter table public.user_devices enable row level security;
alter table public.notification_events enable row level security;
alter table public.notification_delivery_attempts enable row level security;
alter table public.notification_open_events enable row level security;

create policy if not exists "user_devices_select_own"
on public.user_devices
for select
to authenticated
using (auth.uid() = user_id);

create policy if not exists "user_devices_update_own"
on public.user_devices
for update
to authenticated
using (auth.uid() = user_id)
with check (auth.uid() = user_id);

create policy if not exists "user_devices_insert_own"
on public.user_devices
for insert
to authenticated
with check (auth.uid() = user_id);

create policy if not exists "notification_events_service_role"
on public.notification_events
for all
to service_role
using (true)
with check (true);

create policy if not exists "notification_delivery_attempts_service_role"
on public.notification_delivery_attempts
for all
to service_role
using (true)
with check (true);

create policy if not exists "notification_open_events_service_role"
on public.notification_open_events
for all
to service_role
using (true)
with check (true);

create unique index if not exists user_devices_active_unique_idx
on public.user_devices(user_id, onesignal_player_id)
where invalidated_at is null;

create unique index if not exists notification_attempt_once_idx
on public.notification_delivery_attempts(notification_event_id, user_id);

create index if not exists user_devices_last_seen_idx
on public.user_devices(last_seen_at);
