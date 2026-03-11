-- Allow any group member to see all other members of that group.
--
-- The existing policy only allowed users to see their own membership row
-- (or all rows if they were the group owner), which caused member counts
-- and member lists to show only the requesting user's own entry.
--
-- is_group_member uses SECURITY DEFINER so it queries the memberships table
-- as the function owner (bypassing RLS), avoiding infinite recursion.

create or replace function is_group_member(p_group_id uuid)
returns boolean
language sql
security definer
set search_path = public
as $$
    select exists (
        select 1 from memberships
        where memberships.group_id = p_group_id
          and memberships.member_user_id = auth.uid()
    );
$$;

drop policy "memberships_select_self_or_owner" on memberships;

create policy "memberships_select_member_or_owner"
    on memberships
    for select
    using (
        is_group_owner(memberships.group_id)
        or is_group_member(memberships.group_id)
    );
