-- 023-safety-controls US1: reports (specs/023-safety-controls/data-model.md, research D1–D6, D10).
-- Users write a report only through submit_abuse_report(). The report-email edge function
-- (service_role) claims and marks rows, triggered on insert and by a 5-minute sweep.

-- No foreign keys on the ids: a report outlives the reporter's, the reported person's and the
-- group's deletion (FR-023, research D3). The names are snapshots taken when it's filed.
create table public.abuse_reports (
    id uuid primary key default gen_random_uuid(),
    reporter_user_id uuid not null,
    group_id uuid not null,
    reported_user_id uuid null,
    reason text not null check (reason in (
        'inappropriate_photo_or_name', 'harassment_or_threats', 'child_safety', 'spam', 'other'
    )),
    details text not null check (char_length(btrim(details)) between 1 and 1000),
    group_name text not null,
    reporter_display_name text not null,
    reported_display_name text null,
    received_at timestamptz not null default now(),
    email_sent_at timestamptz null,
    email_attempts int not null default 0,
    email_last_error text null,
    email_claimed_at timestamptz null,
    email_alerted_at timestamptz null,
    legal_hold_until timestamptz null
);

create index abuse_reports_reporter_received_idx on public.abuse_reports (reporter_user_id, received_at);
create index abuse_reports_unsent_idx on public.abuse_reports (received_at) where email_sent_at is null;

-- RLS on with no policies: users never read reports back (FR-008).
alter table public.abuse_reports enable row level security;
revoke all on public.abuse_reports from anon, authenticated;

-- The reasons, length and limit here are authoritative. The Api's AbuseReportValidator and the
-- App's ReportDraft mirror them for early feedback.
create or replace function public.submit_abuse_report(
    p_group_id uuid,
    p_reported_user_id uuid,
    p_reason text,
    p_details text
)
returns table (id uuid, received_at timestamptz)
language plpgsql
security definer
set search_path = public
as $$
declare
    v_user_id uuid := auth.uid();
    v_details text := btrim(coalesce(p_details, ''));
    v_group_name text;
    v_reporter_name text;
    v_reported_name text;
begin
    if v_user_id is null then
        raise exception 'not_authenticated' using errcode = '28000';
    end if;

    if not private.account_is_active(v_user_id) then
        raise exception 'account_inactive' using errcode = 'P0001';
    end if;

    if p_reason is null
       or p_reason not in ('inappropriate_photo_or_name', 'harassment_or_threats', 'child_safety', 'spam', 'other')
       or char_length(v_details) not between 1 and 1000 then
        raise exception 'invalid_report' using errcode = 'P0001';
    end if;

    select groups.name into v_group_name
    from public.groups
    join public.memberships on memberships.group_id = groups.id
    where groups.id = p_group_id
      and memberships.member_user_id = v_user_id
      and private.group_is_active(groups.id);

    if v_group_name is null then
        raise exception 'not_group_member' using errcode = 'P0001';
    end if;

    if p_reported_user_id is not null then
        if p_reported_user_id = v_user_id
           or not private.account_is_active(p_reported_user_id)
           or not exists (
               select 1
               from public.memberships
               where memberships.group_id = p_group_id
                 and memberships.member_user_id = p_reported_user_id
           ) then
            raise exception 'invalid_reported_user' using errcode = 'P0001';
        end if;

        select coalesce(user_profiles.display_name, '') into v_reported_name
        from public.user_profiles
        where user_profiles.id = p_reported_user_id;
        v_reported_name := coalesce(v_reported_name, '');
    end if;

    -- Serialises one reporter's submissions so two at once can't both pass the count.
    perform pg_advisory_xact_lock(hashtextextended('abuse_reports:' || v_user_id::text, 0));

    if (
        select count(*)
        from public.abuse_reports
        where abuse_reports.reporter_user_id = v_user_id
          and abuse_reports.received_at > now() - interval '24 hours'
    ) >= 10 then
        raise exception 'report_limit_reached' using errcode = 'P0001';
    end if;

    select coalesce(user_profiles.display_name, '') into v_reporter_name
    from public.user_profiles
    where user_profiles.id = v_user_id;

    return query
    insert into public.abuse_reports as r (
        reporter_user_id, group_id, reported_user_id, reason, details,
        group_name, reporter_display_name, reported_display_name
    )
    values (
        v_user_id, p_group_id, p_reported_user_id, p_reason, v_details,
        v_group_name, coalesce(v_reporter_name, ''), v_reported_name
    )
    returning r.id, r.received_at;
end;
$$;

-- Claims one report (p_report_id) or the oldest p_limit unsent ones whose 10-minute lease is free,
-- so the insert trigger's call and a sweep never send the same report twice.
create or replace function public.claim_report_emails(p_report_id uuid, p_limit int)
returns setof public.abuse_reports
language sql
security definer
set search_path = public
as $$
    with due as (
        select id
        from public.abuse_reports
        where email_sent_at is null
          and (email_claimed_at is null or email_claimed_at < now() - interval '10 minutes')
          and (p_report_id is null or id = p_report_id)
        order by received_at
        limit p_limit
        for update skip locked
    )
    update public.abuse_reports r
    set email_claimed_at = now(),
        email_attempts = r.email_attempts + 1
    from due
    where r.id = due.id
    returning r.*;
$$;

-- p_error null marks the report sent. Otherwise p_error is an error code (never a provider body),
-- and the lease is freed for the next sweep.
create or replace function public.mark_report_email(p_report_id uuid, p_error text)
returns void
language plpgsql
security definer
set search_path = public
as $$
begin
    if p_error is null then
        update public.abuse_reports
        set email_sent_at = now(),
            email_last_error = null,
            email_claimed_at = null
        where id = p_report_id;
    else
        update public.abuse_reports
        set email_last_error = p_error,
            email_claimed_at = null
        where id = p_report_id;
    end if;
end;
$$;

-- Reports still unsent 24 hours after they arrived, each returned once so the alert is logged once.
create or replace function public.stuck_report_emails()
returns setof public.abuse_reports
language sql
security definer
set search_path = public
as $$
    update public.abuse_reports
    set email_alerted_at = now()
    where email_sent_at is null
      and email_alerted_at is null
      and received_at < now() - interval '24 hours'
    returning *;
$$;

-- 1 year from receipt, unless an operator has placed a legal hold (FR-024).
create or replace function public.delete_expired_abuse_reports()
returns int
language plpgsql
security definer
set search_path = public
as $$
declare
    v_deleted int;
begin
    delete from public.abuse_reports
    where received_at < now() - interval '1 year'
      and (legal_hold_until is null or legal_hold_until < now());
    get diagnostics v_deleted = row_count;
    return v_deleted;
end;
$$;

revoke execute on function public.submit_abuse_report(uuid, uuid, text, text) from public, anon;
revoke execute on function public.claim_report_emails(uuid, int) from public, anon, authenticated;
revoke execute on function public.mark_report_email(uuid, text) from public, anon, authenticated;
revoke execute on function public.stuck_report_emails() from public, anon, authenticated;
revoke execute on function public.delete_expired_abuse_reports() from public, anon, authenticated;

grant execute on function public.submit_abuse_report(uuid, uuid, text, text) to authenticated;
grant execute on function public.claim_report_emails(uuid, int) to service_role;
grant execute on function public.mark_report_email(uuid, text) to service_role;
grant execute on function public.stuck_report_emails() to service_role;

-- Sends each new report at once. Fresh local and CI databases have no Vault secrets, so the
-- trigger does nothing there, as the purge schedule does (research D4).
create extension if not exists pg_cron;
create extension if not exists pg_net;

create or replace function private.send_new_abuse_report()
returns trigger
language plpgsql
security definer
set search_path = public
as $$
declare
    v_project_url text := (
        select decrypted_secret from vault.decrypted_secrets where name = 'project_url' limit 1
    );
    v_publishable_key text := (
        select decrypted_secret from vault.decrypted_secrets where name = 'publishable_key' limit 1
    );
begin
    if coalesce(v_project_url, '') <> '' and coalesce(v_publishable_key, '') <> '' then
        perform net.http_post(
            url := v_project_url || '/functions/v1/report-email',
            headers := jsonb_build_object(
                'Content-Type', 'application/json',
                'Authorization', 'Bearer ' || v_publishable_key
            ),
            body := jsonb_build_object('report_id', new.id)
        );
    end if;

    return new;
end;
$$;

revoke execute on function private.send_new_abuse_report() from public, anon, authenticated;

create trigger abuse_reports_send_email
    after insert on public.abuse_reports
    for each row execute function private.send_new_abuse_report();

do $$
declare
    vault_project_url text := (
        select decrypted_secret from vault.decrypted_secrets where name = 'project_url' limit 1
    );
    vault_publishable_key text := (
        select decrypted_secret from vault.decrypted_secrets where name = 'publishable_key' limit 1
    );
begin
    -- Retention needs no HTTP call, so it is scheduled everywhere.
    if exists (select 1 from cron.job where jobname = 'abuse-reports-retention') then
        perform cron.unschedule('abuse-reports-retention');
    end if;
    perform cron.schedule('abuse-reports-retention', '17 3 * * *', 'select public.delete_expired_abuse_reports();');

    if coalesce(vault_project_url, '') = '' or coalesce(vault_publishable_key, '') = '' then
        raise notice 'Vault secrets project_url/publishable_key missing; skipping report-email-sweep schedule';
        return;
    end if;

    if exists (select 1 from cron.job where jobname = 'report-email-sweep') then
        perform cron.unschedule('report-email-sweep');
    end if;

    perform cron.schedule(
        'report-email-sweep',
        '*/5 * * * *',
        $cron$
        select
            net.http_post(
                url := (select decrypted_secret from vault.decrypted_secrets where name = 'project_url') || '/functions/v1/report-email',
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
