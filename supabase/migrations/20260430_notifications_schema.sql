create table if not exists public.user_devices (
  id uuid primary key default gen_random_uuid(),
  user_id uuid not null references auth.users(id) on delete cascade,
  onesignal_player_id text,
  onesignal_external_id text not null,
  platform text not null,
  device_locale text,
  device_timezone text,
  notifications_enabled boolean not null default true,
  permission_state text not null default 'unknown',
  last_seen_at timestamptz not null default now(),
  invalidated_at timestamptz,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  constraint user_devices_external_id_ck check (onesignal_external_id = user_id::text),
  constraint user_devices_permission_ck check (permission_state in ('unknown', 'granted', 'denied'))
);

create table if not exists public.notification_events (
  id uuid primary key default gen_random_uuid(),
  event_id text not null unique,
  notification_type text not null,
  target_kind text not null,
  target_id text,
  fallback_route text not null,
  source_table text not null,
  source_row_id text not null,
  occurred_at timestamptz not null,
  payload_json jsonb not null,
  created_at timestamptz not null default now()
);

create table if not exists public.notification_delivery_attempts (
  id uuid primary key default gen_random_uuid(),
  notification_event_id uuid not null references public.notification_events(id) on delete cascade,
  user_id uuid not null references auth.users(id) on delete cascade,
  device_id uuid references public.user_devices(id) on delete set null,
  onesignal_message_id text,
  status text not null,
  provider_status_code int,
  provider_response jsonb,
  error_code text,
  attempted_at timestamptz not null default now(),
  constraint notification_delivery_attempts_status_ck check (status in ('queued', 'sent', 'failed'))
);

create table if not exists public.notification_open_events (
  id uuid primary key default gen_random_uuid(),
  event_id text not null,
  user_id uuid references auth.users(id) on delete set null,
  navigation_result text not null,
  resolved_route text,
  opened_at timestamptz not null default now(),
  constraint notification_open_events_result_ck check (navigation_result in ('resolved', 'fallback', 'deferred_auth', 'failed'))
);
