-- 016-account-deletion, part 3 of 3: permanent deletion (specs/016-account-deletion/data-model.md).
-- Apple refresh tokens kept for revocation, the service-role-only functions the account-purge
-- edge function runs, and the hourly schedule that triggers it.

-- Only service_role (edge functions) reads or writes this table: RLS is on with no policies.
create table public.apple_authorizations (
    user_id uuid not null references auth.users(id) on delete cascade,
    client_id text not null,
    refresh_token text not null,
    created_at timestamptz not null default now(),
    updated_at timestamptz not null default now(),
    primary key (user_id, client_id)
);

alter table public.apple_authorizations enable row level security;
revoke all on public.apple_authorizations from anon, authenticated;

-- Claims up to p_limit due rows whose lease is free (never claimed, or claimed over 15 minutes
-- ago by a run that died). skip locked keeps overlapping runs from claiming the same row.
create or replace function public.claim_due_account_purges(p_limit int)
returns setof public.account_deletions
language sql
security definer
set search_path = public
as $$
    with due as (
        select user_id
        from public.account_deletions
        where purge_after <= now()
          and (claimed_at is null or claimed_at < now() - interval '15 minutes')
        order by purge_after
        limit p_limit
        for update skip locked
    )
    update public.account_deletions d
    set status = 'purging',
        claimed_at = now(),
        attempts = d.attempts + 1,
        updated_at = now()
    from due
    where d.user_id = due.user_id
    returning d.*;
$$;

-- p_error null records the step as done. Otherwise p_error is an error code (never a provider
-- body), and the lease is released so the next hourly run resumes at this step.
create or replace function public.record_account_purge_step(p_user_id uuid, p_step text, p_error text)
returns void
language plpgsql
security definer
set search_path = public
as $$
begin
    if p_step is null or p_step not in ('onesignal', 'apple', 'storage', 'database', 'auth') then
        raise exception 'unknown purge step %', p_step using errcode = '22023';
    end if;

    if p_error is null then
        update public.account_deletions
        set completed_steps = case
                when p_step = any(completed_steps) then completed_steps
                else array_append(completed_steps, p_step)
            end,
            updated_at = now()
        where user_id = p_user_id;
    else
        update public.account_deletions
        set last_error = p_error,
            claimed_at = null,
            updated_at = now()
        where user_id = p_user_id;
    end if;
end;
$$;

-- The database step. Idempotent: a re-run finds nothing left to delete. The auth user, and with
-- it the account_deletions row, is deleted afterwards by the auth step.
create or replace function public.purge_account_data(p_user_id uuid)
returns void
language plpgsql
security definer
set search_path = public
as $$
declare
    v_email text;
begin
    select email into v_email from auth.users where id = p_user_id;

    delete from public.notification_open_events where user_id = p_user_id;
    delete from public.notification_delivery_attempts where user_id = p_user_id;
    delete from public.user_devices where user_id = p_user_id;
    delete from public.invitations
    where invited_by_user_id = p_user_id
       or invited_user_id = p_user_id
       or (v_email is not null and lower(invited_email) = lower(v_email));
    delete from public.meetups where created_by_user_id = p_user_id;
    -- Cascades to the owned groups' memberships, invitations and meetups.
    delete from public.groups where owner_user_id = p_user_id;
    delete from public.memberships where member_user_id = p_user_id;
    delete from public.apple_authorizations where user_id = p_user_id;
    delete from public.user_profiles where id = p_user_id;
end;
$$;

revoke execute on function public.claim_due_account_purges(int) from public, anon, authenticated;
revoke execute on function public.record_account_purge_step(uuid, text, text) from public, anon, authenticated;
revoke execute on function public.purge_account_data(uuid) from public, anon, authenticated;

grant execute on function public.claim_due_account_purges(int) to service_role;
grant execute on function public.record_account_purge_step(uuid, text, text) to service_role;
grant execute on function public.purge_account_data(uuid) to service_role;

-- Hourly at minute 7, clear of the 5-minute reminder runs. Same trigger as reminders-scheduler:
-- the publishable key, which is harmless because a run only processes rows that are already due.
create extension if not exists pg_cron;
create extension if not exists pg_net;

do $$
declare
    vault_project_url text := (
        select decrypted_secret
        from vault.decrypted_secrets
        where name = 'project_url'
        limit 1
    );
    vault_publishable_key text := (
        select decrypted_secret
        from vault.decrypted_secrets
        where name = 'publishable_key'
        limit 1
    );
begin
    -- Fresh local and CI databases have no vault secrets, so they get no cron job.
    if coalesce(vault_project_url, '') = '' or coalesce(vault_publishable_key, '') = '' then
        raise notice 'Vault secrets project_url/publishable_key missing; skipping cron schedule';
        return;
    end if;

    if exists (select 1 from cron.job where jobname = 'account-purge-hourly') then
        perform cron.unschedule('account-purge-hourly');
    end if;

    perform cron.schedule(
        'account-purge-hourly',
        '7 * * * *',
        $cron$
        select
            net.http_post(
                url := (select decrypted_secret from vault.decrypted_secrets where name = 'project_url') || '/functions/v1/account-purge',
                headers := jsonb_build_object(
                    'Content-Type', 'application/json',
                    'Authorization', 'Bearer ' || (select decrypted_secret from vault.decrypted_secrets where name = 'publishable_key')
                ),
                body := '{}'::jsonb
            ) as request_id;
        $cron$
    );
end
$$;
