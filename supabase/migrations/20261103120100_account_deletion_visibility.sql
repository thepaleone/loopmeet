-- 016 account deletion: hide a pending user from everyone else (FR-006 to FR-009).
--
-- Every policy on groups, memberships, invitations, meetups and user_profiles is
-- recreated with its existing predicate unchanged AND the account-active conditions
-- from data-model.md. Restoring the account deletes the account_deletions row, so
-- everything becomes visible again without any data having changed (FR-012).
--
-- The recreated policies apply to `authenticated` only. They used to apply to every
-- role, but anon has no usage on the private schema, so an anon query would now fail
-- instead of returning nothing. Without a policy anon still sees and changes nothing.
-- service_role has bypassrls, so the old `current_setting('role') = 'service_role'`
-- branch on the memberships insert policy never decided anything and is dropped.

-- groups

drop policy if exists "groups_select_owner_or_member" on public.groups;
create policy "groups_select_owner_or_member"
    on public.groups
    for select
    to authenticated
    using (
        (
            owner_user_id = auth.uid()
            or exists (
                select 1
                from public.memberships
                where memberships.group_id = groups.id
                  and memberships.member_user_id = auth.uid()
            )
            or public.is_group_invited_recipient(groups.id)
        )
        and private.account_is_active(owner_user_id)
    );

drop policy if exists "groups_insert_owner" on public.groups;
create policy "groups_insert_owner"
    on public.groups
    for insert
    to authenticated
    with check (
        owner_user_id = auth.uid()
        and private.account_is_active((select auth.uid()))
    );

drop policy if exists "groups_update_owner" on public.groups;
create policy "groups_update_owner"
    on public.groups
    for update
    to authenticated
    using (
        owner_user_id = auth.uid()
        and private.account_is_active(owner_user_id)
    );

-- memberships

drop policy if exists "memberships_select_member_or_owner" on public.memberships;
create policy "memberships_select_member_or_owner"
    on public.memberships
    for select
    to authenticated
    using (
        (public.is_group_owner(group_id) or public.is_group_member(group_id))
        and private.group_is_active(group_id)
        and private.account_is_active(member_user_id)
    );

drop policy if exists "memberships_insert_service_or_self" on public.memberships;
create policy "memberships_insert_service_or_self"
    on public.memberships
    for insert
    to authenticated
    with check (
        member_user_id = auth.uid()
        and private.account_is_active((select auth.uid()))
    );

-- invitations

drop policy if exists "invitations_select_recipient_or_owner" on public.invitations;
create policy "invitations_select_recipient_or_owner"
    on public.invitations
    for select
    to authenticated
    using (
        (
            invited_user_id = auth.uid()
            or invited_email = (auth.jwt() ->> 'email')
            or public.is_group_owner(group_id)
        )
        and private.group_is_active(group_id)
        and private.account_is_active(invited_by_user_id)
        and (invited_user_id is null or private.account_is_active(invited_user_id))
        and private.email_is_active(invited_email)
    );

drop policy if exists "invitations_insert_owner" on public.invitations;
create policy "invitations_insert_owner"
    on public.invitations
    for insert
    to authenticated
    with check (
        exists (
            select 1
            from public.groups
            where groups.id = invitations.group_id
              and groups.owner_user_id = auth.uid()
        )
        and private.account_is_active((select auth.uid()))
    );

drop policy if exists "invitations_update_recipient" on public.invitations;
create policy "invitations_update_recipient"
    on public.invitations
    for update
    to authenticated
    using (
        (
            invited_user_id = auth.uid()
            or invited_email = (auth.jwt() ->> 'email')
        )
        and private.group_is_active(group_id)
        and private.account_is_active(invited_by_user_id)
        and (invited_user_id is null or private.account_is_active(invited_user_id))
        and private.email_is_active(invited_email)
    );

-- meetups

drop policy if exists "meetups_select_group_member" on public.meetups;
create policy "meetups_select_group_member"
    on public.meetups
    for select
    to authenticated
    using (
        exists (
            select 1
            from public.memberships
            where memberships.group_id = meetups.group_id
              and memberships.member_user_id = auth.uid()
        )
        and private.group_is_active(group_id)
        and private.account_is_active(created_by_user_id)
    );

drop policy if exists "meetups_insert_group_member" on public.meetups;
create policy "meetups_insert_group_member"
    on public.meetups
    for insert
    to authenticated
    with check (
        exists (
            select 1
            from public.memberships
            where memberships.group_id = meetups.group_id
              and memberships.member_user_id = auth.uid()
        )
        and private.account_is_active((select auth.uid()))
    );

drop policy if exists "meetups_update_group_member" on public.meetups;
create policy "meetups_update_group_member"
    on public.meetups
    for update
    to authenticated
    using (
        exists (
            select 1
            from public.memberships
            where memberships.group_id = meetups.group_id
              and memberships.member_user_id = auth.uid()
        )
        and private.group_is_active(group_id)
        and private.account_is_active(created_by_user_id)
    );

drop policy if exists "meetups_delete_group_member" on public.meetups;
create policy "meetups_delete_group_member"
    on public.meetups
    for delete
    to authenticated
    using (
        exists (
            select 1
            from public.memberships
            where memberships.group_id = meetups.group_id
              and memberships.member_user_id = auth.uid()
        )
        and private.group_is_active(group_id)
        and private.account_is_active(created_by_user_id)
    );

-- user_profiles

drop policy if exists "user_profiles_select_authenticated" on public.user_profiles;
create policy "user_profiles_select_authenticated"
    on public.user_profiles
    for select
    to authenticated
    using (
        auth.uid() is not null
        and private.account_is_active(id)
    );

drop policy if exists "user_profiles_insert_self" on public.user_profiles;
create policy "user_profiles_insert_self"
    on public.user_profiles
    for insert
    to authenticated
    with check (
        id = auth.uid()
        and private.account_is_active((select auth.uid()))
    );

drop policy if exists "user_profiles_update_self" on public.user_profiles;
create policy "user_profiles_update_self"
    on public.user_profiles
    for update
    to authenticated
    using (
        id = auth.uid()
        and private.account_is_active((select auth.uid()))
    )
    with check (
        id = auth.uid()
        and private.account_is_active((select auth.uid()))
    );

-- active_memberships: the memberships that may receive or cause notifications.
-- Edge functions read it with the service-role key, which bypasses RLS on memberships,
-- so the account-active filter has to be applied here instead (FR-008).

create view public.active_memberships
with (security_invoker = false)
as
select m.*
from public.memberships m
join public.groups g on g.id = m.group_id
where private.account_is_active(m.member_user_id)
  and private.account_is_active(g.owner_user_id);

revoke all on public.active_memberships from public, anon, authenticated;
grant select on public.active_memberships to service_role;
