create table user_profiles (
    id uuid primary key not null,
    email varchar not null,
    display_name varchar null,
    phone varchar null,
    created_at timestamptz default now(),
    updated_at timestamptz default now()
);