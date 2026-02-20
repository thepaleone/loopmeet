alter table groups enable row level security;
alter table memberships enable row level security;
alter table invitations enable row level security;
alter table user_profiles enable row level security;

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
        or exists (
            select 1
            from groups
            where groups.id = memberships.group_id
              and groups.owner_user_id = auth.uid()
        )
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
        or exists (
            select 1
            from groups
            where groups.id = invitations.group_id
              and groups.owner_user_id = auth.uid()
        )
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
    using (auth.role() = 'authenticated');
