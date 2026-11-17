-- 023 US2: a member leaves a group (specs/023-safety-controls/data-model.md).
-- Leaving deletes the caller's memberships row (research D2), so active_memberships, and with it
-- every notification recipient query, drops them with no change. Meetups they organised stay with
-- the group: meetups.created_by_user_id has no foreign key to memberships.

create or replace function public.leave_group(p_group_id uuid)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
    v_user_id uuid := auth.uid();
    v_role text;
    v_owner_user_id uuid;
begin
    if v_user_id is null then
        raise exception 'not_authenticated' using errcode = '28000';
    end if;

    if not private.account_is_active(v_user_id) then
        raise exception 'account_inactive' using errcode = 'P0001';
    end if;

    select memberships.role, groups.owner_user_id
      into v_role, v_owner_user_id
    from public.memberships
    join public.groups on groups.id = memberships.group_id
    where memberships.group_id = p_group_id
      and memberships.member_user_id = v_user_id
      and private.group_is_active(groups.id);

    if not found then
        raise exception 'not_group_member' using errcode = 'P0001';
    end if;

    if v_owner_user_id = v_user_id or v_role = 'owner' then
        raise exception 'owner_cannot_leave' using errcode = 'P0001';
    end if;

    delete from public.memberships
    where group_id = p_group_id
      and member_user_id = v_user_id;
end;
$$;

revoke execute on function public.leave_group(uuid) from public, anon;
grant execute on function public.leave_group(uuid) to authenticated;
