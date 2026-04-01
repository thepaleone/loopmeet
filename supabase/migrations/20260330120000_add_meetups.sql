create table meetups (
    id uuid primary key default gen_random_uuid(),
    group_id uuid not null references groups(id) on delete cascade,
    created_by_user_id uuid not null,
    title varchar not null,
    scheduled_at timestamptz not null,
    place_name varchar null,
    place_address varchar null,
    latitude double precision null,
    longitude double precision null,
    place_id varchar null,
    created_at timestamptz default now(),
    updated_at timestamptz default now()
);

create index idx_meetups_group_scheduled on meetups (group_id, scheduled_at);
create index idx_meetups_scheduled_at on meetups (scheduled_at);

alter table meetups enable row level security;

create policy "meetups_select_group_member"
    on meetups
    for select
    using (
        exists (
            select 1 from memberships
            where memberships.group_id = meetups.group_id
              and memberships.member_user_id = auth.uid()
        )
    );

create policy "meetups_insert_group_member"
    on meetups
    for insert
    with check (
        exists (
            select 1 from memberships
            where memberships.group_id = meetups.group_id
              and memberships.member_user_id = auth.uid()
        )
    );

create policy "meetups_update_group_member"
    on meetups
    for update
    using (
        exists (
            select 1 from memberships
            where memberships.group_id = meetups.group_id
              and memberships.member_user_id = auth.uid()
        )
    );

create policy "meetups_delete_group_member"
    on meetups
    for delete
    using (
        exists (
            select 1 from memberships
            where memberships.group_id = meetups.group_id
              and memberships.member_user_id = auth.uid()
        )
    );
