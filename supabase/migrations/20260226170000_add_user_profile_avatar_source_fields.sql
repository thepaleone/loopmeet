alter table user_profiles
    add column if not exists social_avatar_url varchar null,
    add column if not exists avatar_override_url varchar null;
