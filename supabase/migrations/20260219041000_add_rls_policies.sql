alter table groups enable row level security;
alter table memberships enable row level security;
alter table invitations enable row level security;
alter table user_profiles enable row level security;

create or replace function is_group_owner(group_id uuid)
returns boolean
language sql
security definer
set search_path = public
as $$
    select exists (
        select 1
        from groups
        where groups.id = group_id
          and groups.owner_user_id = auth.uid()
    );
$$;

create or replace function is_group_invited_recipient(group_id uuid)
returns boolean
language sql
security definer
set search_path = public
as $$
    select exists (
        select 1
        from invitations
        where invitations.group_id = group_id
          and (
              invitations.invited_user_id = auth.uid()
              or invitations.invited_email = (auth.jwt() ->> 'email')
          )
    );
$$;

create policy "groups_select_owner_or_member"
    on groups
    for select
    using (
        owner_user_id = auth.uid()
        or exists (
            select 1
            from memberships
            where memberships.group_id = groups.id
              and memberships.member_user_id = auth.uid()
        )
        or is_group_invited_recipient(groups.id)
    );

create policy "groups_insert_owner"
    on groups
    for insert
    with check (owner_user_id = auth.uid());

create policy "groups_update_owner"
    on groups
    for update
    using (owner_user_id = auth.uid());

create policy "memberships_select_self_or_owner"
    on memberships
    for select
    using (
        member_user_id = auth.uid()
        or is_group_owner(memberships.group_id)
    );

create policy "memberships_insert_self"
    on memberships
    for insert
    with check (member_user_id = auth.uid());

create policy "invitations_select_recipient_or_owner"
    on invitations
    for select
    using (
        invited_user_id = auth.uid()
        or invited_email = (auth.jwt() ->> 'email')
        or is_group_owner(invitations.group_id)
    );

create policy "invitations_insert_owner"
    on invitations
    for insert
    with check (
        exists (
            select 1
            from groups
            where groups.id = invitations.group_id
              and groups.owner_user_id = auth.uid()
        )
    );

create policy "invitations_update_recipient"
    on invitations
    for update
    using (
        invited_user_id = auth.uid()
        or invited_email = (auth.jwt() ->> 'email')
    );

create policy "user_profiles_select_authenticated"
    on user_profiles
    for select
    using (id = auth.uid());

create policy "user_profiles_insert_self"
    on user_profiles
    for insert
    with check (id = auth.uid());

create policy "user_profiles_update_self"
    on user_profiles
    for update
    using (id = auth.uid())
    with check (id = auth.uid());
