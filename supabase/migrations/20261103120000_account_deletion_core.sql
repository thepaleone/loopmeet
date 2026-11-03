-- 016-account-deletion, part 1 of 3: the deletion record, the "is this account active?"
-- helpers, and the caller-run request and restore functions (specs/016-account-deletion/data-model.md).

create table public.account_deletions (
    user_id uuid primary key references auth.users(id) on delete cascade,
    requested_at timestamptz not null,
    purge_after timestamptz not null,
    status text not null check (status in ('pending', 'purging')),
    completed_steps text[] not null default '{}',
    attempts int not null default 0,
    last_error text null,
    claimed_at timestamptz null,
    updated_at timestamptz not null default now()
);

alter table public.account_deletions enable row level security;

create policy "account_deletions_select_self"
    on public.account_deletions
    for select
    using (user_id = auth.uid());

-- Clients write only through request_account_deletion() and restore_account_deletion().
revoke all on public.account_deletions from anon, authenticated;
grant select on public.account_deletions to authenticated;

-- PostgREST exposes only public, so nobody can call these as RPCs to probe whether
-- another user is pending deletion. Policies can still execute them.
create schema if not exists private;
grant usage on schema private to authenticated, service_role;

create or replace function private.account_is_active(p_user_id uuid)
returns boolean
language sql
stable
security definer
set search_path = public
as $$
    select not exists (
        select 1
        from public.account_deletions
        where account_deletions.user_id = p_user_id
    );
$$;

create or replace function private.group_is_active(p_group_id uuid)
returns boolean
language sql
stable
security definer
set search_path = public
as $$
    select exists (
        select 1
        from public.groups
        where groups.id = p_group_id
          and private.account_is_active(groups.owner_user_id)
    );
$$;

create or replace function private.email_is_active(p_email text)
returns boolean
language sql
stable
security definer
set search_path = public
as $$
    select not exists (
        select 1
        from public.account_deletions
        join auth.users on users.id = account_deletions.user_id
        where lower(users.email) = lower(p_email)
    );
$$;

-- The 30-day grace period is authoritative here. AccountDeletionPolicy.GracePeriod mirrors it
-- for the preview screen, and tests pin both.
create or replace function public.request_account_deletion()
returns public.account_deletions
language plpgsql
security definer
set search_path = public
as $$
declare
    v_user_id uuid := auth.uid();
    v_row public.account_deletions;
begin
    if v_user_id is null then
        raise exception 'not_authenticated' using errcode = '28000';
    end if;

    insert into public.account_deletions (user_id, requested_at, purge_after, status)
    values (v_user_id, now(), now() + interval '30 days', 'pending')
    on conflict (user_id) do nothing;

    select * into v_row
    from public.account_deletions
    where user_id = v_user_id;

    return v_row;
end;
$$;

-- Returns true when a pending request was removed and false when there was none.
-- Raises deletion_in_progress once the deadline has passed or purging has started.
create or replace function public.restore_account_deletion()
returns boolean
language plpgsql
security definer
set search_path = public
as $$
declare
    v_user_id uuid := auth.uid();
begin
    if v_user_id is null then
        raise exception 'not_authenticated' using errcode = '28000';
    end if;

    delete from public.account_deletions
    where user_id = v_user_id
      and status = 'pending'
      and purge_after > now();

    if found then
        return true;
    end if;

    if exists (select 1 from public.account_deletions where user_id = v_user_id) then
        raise exception 'deletion_in_progress' using errcode = 'P0001';
    end if;

    return false;
end;
$$;

revoke execute on function private.account_is_active(uuid) from public, anon;
revoke execute on function private.group_is_active(uuid) from public, anon;
revoke execute on function private.email_is_active(text) from public, anon;
revoke execute on function public.request_account_deletion() from public, anon;
revoke execute on function public.restore_account_deletion() from public, anon;

grant execute on function private.account_is_active(uuid) to authenticated, service_role;
grant execute on function private.group_is_active(uuid) to authenticated, service_role;
grant execute on function private.email_is_active(text) to authenticated, service_role;
grant execute on function public.request_account_deletion() to authenticated, service_role;
grant execute on function public.restore_account_deletion() to authenticated, service_role;
