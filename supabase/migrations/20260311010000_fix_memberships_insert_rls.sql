-- The memberships_insert_self policy relies solely on auth.uid(), but the
-- Supabase PostgREST client configuration used by the API resolves auth.uid()
-- inconsistently. Other policies (e.g. invitations_update_recipient) work
-- correctly because they also accept invited_email = (auth.jwt() ->> 'email')
-- as a fallback. Add the same fallback here via a security definer function.
--
-- The insert is allowed when:
--   1. member_user_id = auth.uid()  (original condition), OR
--   2. There is an accepted invitation for this group whose invited_user_id
--      matches member_user_id AND whose invited_email matches the caller's
--      JWT email claim (prevents arbitrary users from self-inserting)

create or replace function can_insert_membership(p_group_id uuid, p_member_user_id uuid)
returns boolean
language sql
security definer
set search_path = public
as $$
    select
        p_member_user_id = auth.uid()
        or exists (
            select 1
            from invitations
            where invitations.group_id = p_group_id
              and invitations.invited_user_id = p_member_user_id
              and invitations.status = 'accepted'
              and invitations.invited_email = (auth.jwt() ->> 'email')
        );
$$;

drop policy if exists "memberships_insert_self" on memberships;

create policy "memberships_insert_invited"
    on memberships
    for insert
    with check (can_insert_membership(memberships.group_id, memberships.member_user_id));
