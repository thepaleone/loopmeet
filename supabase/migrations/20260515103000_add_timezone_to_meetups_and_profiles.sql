alter table public.meetups
    add column if not exists timezone text;

alter table public.user_profiles
    add column if not exists timezone text;

comment on column public.meetups.timezone is
    'IANA timezone for the meetup location (e.g., America/Los_Angeles). Nullable; reminder scheduler falls back to the recipient user_profiles.timezone, then America/Los_Angeles.';

comment on column public.user_profiles.timezone is
    'IANA timezone reported by the user''s primary device. Used as the reminder fallback when a meetup does not carry its own timezone.';
