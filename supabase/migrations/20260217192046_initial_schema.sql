create table groups (
    id uuid primary key default gen_random_uuid(),
    owner_user_id uuid not null,
    name varchar not null,
    created_at timestamptz default now(),
    updated_at timestamptz default now()
);

create table memberships (
    id uuid primary key default gen_random_uuid(),
    group_id uuid not null references groups(id) on delete cascade,
    member_user_id uuid not null,
    role varchar not null,
    created_at timestamptz default now(),
    unique (group_id, member_user_id)
);

create table invitations (
    id uuid primary key default gen_random_uuid(),
    group_id uuid not null references groups(id) on delete cascade,
    invited_email varchar not null,
    invited_user_id uuid null,
    status varchar not null,
    created_at timestamptz default now(),
    accepted_at timestamptz null
);

