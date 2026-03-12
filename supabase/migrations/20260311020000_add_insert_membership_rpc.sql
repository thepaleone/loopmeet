-- Replace the RLS-based membership insert with a security definer RPC.
-- The Supabase.NET client's Authorization header doesn't reliably set
-- auth.uid() in PostgREST for INSERT operations (auth.uid() resolves to null).
-- Since SELECT and UPDATE operations work via invited_email matching,
-- we use the same pattern: validate via accepted invitation + email.
--
-- Security: the function verifies there is an accepted invitation for the
-- given group + member_user_id + email before inserting, preventing
-- arbitrary membership creation.

create or replace function create_membership_from_invitation(
    p_id uuid,
    p_group_id uuid,
    p_member_user_id uuid,
    p_role varchar,
    p_email text
)
returns void
language plpgsql
security definer
set search_path = public
as $$
begin
    if not exists (
        select 1
        from invitations
        where invitations.group_id = p_group_id
          and invitations.invited_user_id = p_member_user_id
          and invitations.status = 'accepted'
          and lower(invitations.invited_email) = lower(p_email)
    ) then
        raise exception 'No accepted invitation found for this membership (group_id=%, member_user_id=%, email=%)',
            p_group_id, p_member_user_id, p_email;
    end if;

    insert into memberships (id, group_id, member_user_id, role, created_at)
    values (p_id, p_group_id, p_member_user_id, p_role, now())
    on conflict (group_id, member_user_id) do nothing;
end;
$$;

-- Allow both authenticated and anon roles to call this (actual security is inside the function)
grant execute on function create_membership_from_invitation(uuid, uuid, uuid, varchar, text)
    to authenticated, anon;

-- The old INSERT policy is superseded by the RPC; keep it permissive to avoid
-- blocking any future direct inserts from service role.
drop policy if exists "memberships_insert_invited" on memberships;
drop policy if exists "memberships_insert_self" on memberships;

create policy "memberships_insert_service_or_self"
    on memberships
    for insert
    with check (
        current_setting('role', true) = 'service_role'
        or member_user_id = auth.uid()
    );
