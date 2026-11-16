begin;

create extension if not exists pgtap with schema extensions;

select plan(32);

-- A owns "Book club" with members B and D. C is an outsider. D is pending deletion.
insert into auth.users (id, email) values
    ('00000000-0000-0000-0000-00000000000a', 'a@example.com'),
    ('00000000-0000-0000-0000-00000000000b', 'b@example.com'),
    ('00000000-0000-0000-0000-00000000000c', 'c@example.com'),
    ('00000000-0000-0000-0000-00000000000d', 'd@example.com');

insert into public.user_profiles (id, email, display_name) values
    ('00000000-0000-0000-0000-00000000000a', 'a@example.com', 'Ann'),
    ('00000000-0000-0000-0000-00000000000b', 'b@example.com', 'Ben'),
    ('00000000-0000-0000-0000-00000000000c', 'c@example.com', 'Cat'),
    ('00000000-0000-0000-0000-00000000000d', 'd@example.com', 'Dee');

insert into public.groups (id, owner_user_id, name) values
    ('00000000-0000-0000-0000-0000000000a1', '00000000-0000-0000-0000-00000000000a', 'Book club');

insert into public.memberships (group_id, member_user_id, role) values
    ('00000000-0000-0000-0000-0000000000a1', '00000000-0000-0000-0000-00000000000a', 'owner'),
    ('00000000-0000-0000-0000-0000000000a1', '00000000-0000-0000-0000-00000000000b', 'member'),
    ('00000000-0000-0000-0000-0000000000a1', '00000000-0000-0000-0000-00000000000d', 'member');

insert into public.account_deletions (user_id, requested_at, purge_after, status) values
    ('00000000-0000-0000-0000-00000000000d', now(), now() + interval '30 days', 'pending');

-- Table shape and access (FR-004, FR-008)

select ok(
    (select relrowsecurity from pg_class where oid = 'public.abuse_reports'::regclass),
    'abuse_reports has row-level security on'
);

select has_trigger('public', 'abuse_reports', 'abuse_reports_send_email', 'a new report triggers the email');

select ok(
    exists (select 1 from cron.job where jobname = 'abuse-reports-retention' and schedule = '17 3 * * *'),
    'the daily retention job is scheduled'
);

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000b","role":"authenticated"}', true);
set local role authenticated;

select throws_ok(
    $$ select * from public.abuse_reports $$,
    '42501', null,
    'authenticated cannot read reports'
);
select throws_ok(
    $$ insert into public.abuse_reports (reporter_user_id, group_id, reason, details, group_name, reporter_display_name)
       values ('00000000-0000-0000-0000-00000000000b', '00000000-0000-0000-0000-0000000000a1', 'spam', 'x', 'g', 'n') $$,
    '42501', null,
    'authenticated cannot insert reports directly'
);

-- Submitting (FR-003, FR-004)

select lives_ok(
    $$ select * from public.submit_abuse_report('00000000-0000-0000-0000-0000000000a1', '00000000-0000-0000-0000-00000000000a', 'harassment_or_threats', '  He keeps sending threats.  ') $$,
    'a member reports another member'
);
select lives_ok(
    $$ select * from public.submit_abuse_report('00000000-0000-0000-0000-0000000000a1', null, 'inappropriate_photo_or_name', 'The group name is offensive.') $$,
    'a member reports the group itself'
);

select throws_ok(
    $$ select * from public.submit_abuse_report('00000000-0000-0000-0000-0000000000a1', '00000000-0000-0000-0000-00000000000b', 'spam', 'me') $$,
    'P0001', 'invalid_reported_user',
    'a member cannot report themselves'
);
select throws_ok(
    $$ select * from public.submit_abuse_report('00000000-0000-0000-0000-0000000000a1', '00000000-0000-0000-0000-00000000000c', 'spam', 'not here') $$,
    'P0001', 'invalid_reported_user',
    'a person outside the group cannot be reported'
);
select throws_ok(
    $$ select * from public.submit_abuse_report('00000000-0000-0000-0000-0000000000a1', '00000000-0000-0000-0000-00000000000d', 'spam', 'hidden') $$,
    'P0001', 'invalid_reported_user',
    'a member pending deletion cannot be reported'
);
select throws_ok(
    $$ select * from public.submit_abuse_report('00000000-0000-0000-0000-0000000000a1', null, 'rude', 'x') $$,
    'P0001', 'invalid_report',
    'an unknown reason is refused'
);
select throws_ok(
    $$ select * from public.submit_abuse_report('00000000-0000-0000-0000-0000000000a1', null, 'spam', '   ') $$,
    'P0001', 'invalid_report',
    'blank details are refused'
);
select throws_ok(
    format($$ select * from public.submit_abuse_report('00000000-0000-0000-0000-0000000000a1', null, 'spam', %L) $$, repeat('x', 1001)),
    'P0001', 'invalid_report',
    'details over 1,000 characters are refused'
);
select lives_ok(
    format($$ select * from public.submit_abuse_report('00000000-0000-0000-0000-0000000000a1', null, 'other', %L) $$, repeat('x', 1000)),
    'details of exactly 1,000 characters are accepted'
);

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000c","role":"authenticated"}', true);
select throws_ok(
    $$ select * from public.submit_abuse_report('00000000-0000-0000-0000-0000000000a1', null, 'spam', 'x') $$,
    'P0001', 'not_group_member',
    'a non-member cannot report a group'
);

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000d","role":"authenticated"}', true);
select throws_ok(
    $$ select * from public.submit_abuse_report('00000000-0000-0000-0000-0000000000a1', null, 'spam', 'x') $$,
    'P0001', 'account_inactive',
    'a user pending deletion cannot report'
);

reset role;

select is(
    (select row(reporter_display_name, reported_display_name, group_name, details)::text
     from public.abuse_reports where reason = 'harassment_or_threats'),
    row('Ben', 'Ann', 'Book club', 'He keeps sending threats.')::text,
    'the report stores trimmed details and snapshots of the names'
);
select is(
    (select reported_display_name from public.abuse_reports where reason = 'inappropriate_photo_or_name'),
    null,
    'a report about the group itself has no reported person'
);

-- Rate limit (FR-006): B has 3 reports; 6 more recent ones and one 25 hours old are added.
insert into public.abuse_reports (reporter_user_id, group_id, reason, details, group_name, reporter_display_name, received_at)
select '00000000-0000-0000-0000-00000000000b', '00000000-0000-0000-0000-0000000000a1', 'spam', 'filler', 'Book club', 'Ben', now() - interval '1 hour'
from generate_series(1, 6);
insert into public.abuse_reports (reporter_user_id, group_id, reason, details, group_name, reporter_display_name, received_at)
values ('00000000-0000-0000-0000-00000000000b', '00000000-0000-0000-0000-0000000000a1', 'spam', 'old', 'Book club', 'Ben', now() - interval '25 hours');

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000b","role":"authenticated"}', true);
set local role authenticated;

select lives_ok(
    $$ select * from public.submit_abuse_report('00000000-0000-0000-0000-0000000000a1', null, 'spam', 'tenth') $$,
    'the tenth report in 24 hours is accepted, because the 25-hour-old one does not count'
);
select throws_ok(
    $$ select * from public.submit_abuse_report('00000000-0000-0000-0000-0000000000a1', null, 'spam', 'eleventh') $$,
    'P0001', 'report_limit_reached',
    'the eleventh report in 24 hours is refused'
);

-- Service-only functions are closed to clients.
select throws_ok($$ select * from public.claim_report_emails(null, 25) $$, '42501', null, 'authenticated cannot claim report emails');
select throws_ok($$ select public.delete_expired_abuse_reports() $$, '42501', null, 'authenticated cannot delete reports');

set local role anon;
select throws_ok(
    $$ select * from public.submit_abuse_report('00000000-0000-0000-0000-0000000000a1', null, 'spam', 'x') $$,
    '42501', null,
    'anon cannot submit reports'
);

-- Email delivery (FR-005)
reset role;
create temporary table target as
select id from public.abuse_reports where reason = 'harassment_or_threats';
grant select on target to service_role;
set local role service_role;

select is(
    (select count(*)::int from public.claim_report_emails((select id from target), 25)),
    1,
    'claiming one report returns it'
);
select is(
    (select count(*)::int from public.claim_report_emails((select id from target), 25)),
    0,
    'a report inside its lease is not claimed again'
);

select public.mark_report_email((select id from target), 'http_500');
select is(
    (select row(email_sent_at is null, email_claimed_at is null, email_last_error, email_attempts)::text
     from public.abuse_reports where id = (select id from target)),
    row(true, true, 'http_500', 1)::text,
    'a failure records the error code and frees the lease'
);

select is(
    (select count(*)::int from public.claim_report_emails(null, 25)),
    11,
    'a sweep claims every unsent report with a free lease'
);

select public.mark_report_email((select id from target), null);
reset role;
update public.abuse_reports set email_claimed_at = null where id = (select id from target);
set local role service_role;
select is(
    (select count(*)::int from public.claim_report_emails((select id from target), 25)),
    0,
    'a sent report is never claimed again'
);

-- Stuck alert: logged once for a report unsent 24 hours after it arrived.
select is(
    (select array_agg(details) from public.stuck_report_emails()),
    array['old'],
    'stuck_report_emails returns the report unsent for over 24 hours'
);
select is(
    (select count(*)::int from public.stuck_report_emails()),
    0,
    'each stuck report is returned only once'
);

-- Retention (FR-024)
reset role;
update public.abuse_reports set received_at = now() - interval '13 months' where details in ('old', 'tenth');
update public.abuse_reports set legal_hold_until = now() + interval '1 year' where details = 'tenth';

select is(public.delete_expired_abuse_reports(), 1, 'one expired report is deleted');
select is(
    (select array_agg(details) from public.abuse_reports where received_at < now() - interval '1 year'),
    array['tenth'],
    'a report under a legal hold is kept past a year'
);

select * from finish();
rollback;
