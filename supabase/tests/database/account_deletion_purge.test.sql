begin;

create extension if not exists pgtap with schema extensions;

select plan(40);

-- The visibility fixture: A owns G (member B) and is a member of H (owned by C), where A created
-- a meetup. D owns K, where B is a member. A is the user being purged.
insert into auth.users (id, email) values
    ('00000000-0000-0000-0000-00000000000a', 'a@example.com'),
    ('00000000-0000-0000-0000-00000000000b', 'b@example.com'),
    ('00000000-0000-0000-0000-00000000000c', 'c@example.com'),
    ('00000000-0000-0000-0000-00000000000d', 'd@example.com');

insert into public.user_profiles (id, email, display_name) values
    ('00000000-0000-0000-0000-00000000000a', 'a@example.com', 'A'),
    ('00000000-0000-0000-0000-00000000000b', 'b@example.com', 'B'),
    ('00000000-0000-0000-0000-00000000000c', 'c@example.com', 'C'),
    ('00000000-0000-0000-0000-00000000000d', 'd@example.com', 'D');

insert into public.groups (id, owner_user_id, name) values
    ('00000000-0000-0000-0000-0000000000a1', '00000000-0000-0000-0000-00000000000a', 'G'),
    ('00000000-0000-0000-0000-0000000000c1', '00000000-0000-0000-0000-00000000000c', 'H'),
    ('00000000-0000-0000-0000-0000000000d1', '00000000-0000-0000-0000-00000000000d', 'K');

insert into public.memberships (group_id, member_user_id, role) values
    ('00000000-0000-0000-0000-0000000000a1', '00000000-0000-0000-0000-00000000000a', 'owner'),
    ('00000000-0000-0000-0000-0000000000a1', '00000000-0000-0000-0000-00000000000b', 'member'),
    ('00000000-0000-0000-0000-0000000000c1', '00000000-0000-0000-0000-00000000000c', 'owner'),
    ('00000000-0000-0000-0000-0000000000c1', '00000000-0000-0000-0000-00000000000a', 'member'),
    ('00000000-0000-0000-0000-0000000000d1', '00000000-0000-0000-0000-00000000000d', 'owner'),
    ('00000000-0000-0000-0000-0000000000d1', '00000000-0000-0000-0000-00000000000b', 'member');

insert into public.meetups (id, group_id, created_by_user_id, title, scheduled_at) values
    ('00000000-0000-0000-0000-0000000001a1', '00000000-0000-0000-0000-0000000000a1', '00000000-0000-0000-0000-00000000000a', 'G meetup', now() + interval '3 days'),
    ('00000000-0000-0000-0000-0000000001ca', '00000000-0000-0000-0000-0000000000c1', '00000000-0000-0000-0000-00000000000a', 'H meetup by A', now() + interval '3 days'),
    ('00000000-0000-0000-0000-0000000001cc', '00000000-0000-0000-0000-0000000000c1', '00000000-0000-0000-0000-00000000000c', 'H meetup by C', now() + interval '3 days'),
    ('00000000-0000-0000-0000-0000000001d1', '00000000-0000-0000-0000-0000000000d1', '00000000-0000-0000-0000-00000000000d', 'K meetup', now() + interval '3 days');

-- A invites B to G, C invites A's email (no user id yet) to H, and D invites A by id to K.
-- D also invites C to K: unrelated to A, so it must survive.
insert into public.invitations (id, group_id, invited_email, invited_user_id, invited_by_user_id, status) values
    ('00000000-0000-0000-0000-0000000002ab', '00000000-0000-0000-0000-0000000000a1', 'b@example.com', '00000000-0000-0000-0000-00000000000b', '00000000-0000-0000-0000-00000000000a', 'pending'),
    ('00000000-0000-0000-0000-0000000002ca', '00000000-0000-0000-0000-0000000000c1', 'A@Example.com', null, '00000000-0000-0000-0000-00000000000c', 'pending'),
    ('00000000-0000-0000-0000-0000000002da', '00000000-0000-0000-0000-0000000000d1', 'other-a@example.com', '00000000-0000-0000-0000-00000000000a', '00000000-0000-0000-0000-00000000000d', 'pending'),
    ('00000000-0000-0000-0000-0000000002dc', '00000000-0000-0000-0000-0000000000d1', 'c@example.com', '00000000-0000-0000-0000-00000000000c', '00000000-0000-0000-0000-00000000000d', 'pending');

insert into public.user_devices (id, user_id, onesignal_external_id, platform) values
    ('00000000-0000-0000-0000-0000000003a0', '00000000-0000-0000-0000-00000000000a', '00000000-0000-0000-0000-00000000000a', 'android'),
    ('00000000-0000-0000-0000-0000000003b0', '00000000-0000-0000-0000-00000000000b', '00000000-0000-0000-0000-00000000000b', 'android');

insert into public.notification_events (id, event_id, notification_type, target_kind, target_id, fallback_route, source_table, source_row_id, occurred_at, payload_json) values
    ('00000000-0000-0000-0000-0000000004e0', 'meetups:meetup.created:x', 'meetup.created', 'group', '00000000-0000-0000-0000-0000000000d1', 'home', 'meetups', '00000000-0000-0000-0000-0000000001d1', now(), '{}');

insert into public.notification_delivery_attempts (notification_event_id, user_id, device_id, status) values
    ('00000000-0000-0000-0000-0000000004e0', '00000000-0000-0000-0000-00000000000a', '00000000-0000-0000-0000-0000000003a0', 'sent'),
    ('00000000-0000-0000-0000-0000000004e0', '00000000-0000-0000-0000-00000000000b', '00000000-0000-0000-0000-0000000003b0', 'sent');

insert into public.notification_open_events (event_id, user_id, navigation_result) values
    ('meetups:meetup.created:x', '00000000-0000-0000-0000-00000000000a', 'resolved'),
    ('meetups:meetup.created:x', '00000000-0000-0000-0000-00000000000b', 'resolved');

insert into public.apple_authorizations (user_id, client_id, refresh_token) values
    ('00000000-0000-0000-0000-00000000000a', 'io.loopmeet.app', 'r-a');

-- Deletion rows: A is due, B is due with a stale lease from an earlier failed run,
-- C isn't due yet, and D is due but another run holds its lease.
insert into public.account_deletions (user_id, requested_at, purge_after, status, attempts, claimed_at) values
    ('00000000-0000-0000-0000-00000000000a', now() - interval '31 days', now() - interval '1 day', 'pending', 0, null),
    ('00000000-0000-0000-0000-00000000000b', now() - interval '31 days', now() - interval '1 day', 'purging', 3, now() - interval '20 minutes'),
    ('00000000-0000-0000-0000-00000000000c', now() - interval '1 day', now() + interval '29 days', 'pending', 0, null),
    ('00000000-0000-0000-0000-00000000000d', now() - interval '31 days', now() - interval '1 day', 'purging', 1, now() - interval '5 minutes');

-- Privileged functions are closed to clients (FR-019).
set local role authenticated;
select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000a","role":"authenticated"}', true);

select throws_ok(
    $$ select * from public.claim_due_account_purges(10) $$,
    '42501', null,
    'authenticated cannot claim purges'
);
select throws_ok(
    $$ select public.record_account_purge_step('00000000-0000-0000-0000-00000000000a', 'onesignal', null) $$,
    '42501', null,
    'authenticated cannot record purge steps'
);
select throws_ok(
    $$ select public.purge_account_data('00000000-0000-0000-0000-00000000000b') $$,
    '42501', null,
    'authenticated cannot purge account data'
);
select throws_ok(
    $$ select * from public.apple_authorizations $$,
    '42501', null,
    'authenticated cannot read apple_authorizations'
);

set local role anon;

select throws_ok(
    $$ select public.purge_account_data('00000000-0000-0000-0000-00000000000b') $$,
    '42501', null,
    'anon cannot purge account data'
);
select throws_ok(
    $$ select * from public.apple_authorizations $$,
    '42501', null,
    'anon cannot read apple_authorizations'
);

-- Claiming.
set local role service_role;

select set_eq(
    $$ select user_id from public.claim_due_account_purges(10) $$,
    $$ values ('00000000-0000-0000-0000-00000000000a'::uuid), ('00000000-0000-0000-0000-00000000000b'::uuid) $$,
    'claim returns the due rows with a free lease only'
);

select is(
    (select row(status, claimed_at is not null, attempts)::text from public.account_deletions where user_id = '00000000-0000-0000-0000-00000000000a'),
    row('purging', true, 1)::text,
    'claiming A sets purging, the lease and attempts 1'
);
select is(
    (select row(status, claimed_at > now() - interval '1 minute', attempts)::text from public.account_deletions where user_id = '00000000-0000-0000-0000-00000000000b'),
    row('purging', true, 4)::text,
    'claiming B takes over the stale lease and increments attempts'
);
select is(
    (select row(status, attempts)::text from public.account_deletions where user_id = '00000000-0000-0000-0000-00000000000c'),
    row('pending', 0)::text,
    'C, not yet due, is untouched'
);
select is(
    (select attempts from public.account_deletions where user_id = '00000000-0000-0000-0000-00000000000d'),
    1,
    'D, whose lease is held, is untouched'
);
select is(
    (select count(*)::int from public.claim_due_account_purges(10)),
    0,
    'a second claim finds nothing while the leases are fresh'
);

-- Recording steps.
select public.record_account_purge_step('00000000-0000-0000-0000-00000000000a', 'onesignal', null);
select public.record_account_purge_step('00000000-0000-0000-0000-00000000000a', 'apple', null);
select public.record_account_purge_step('00000000-0000-0000-0000-00000000000a', 'apple', null);

select is(
    (select completed_steps from public.account_deletions where user_id = '00000000-0000-0000-0000-00000000000a'),
    array['onesignal', 'apple'],
    'completed steps are appended in order, once each'
);

select public.record_account_purge_step('00000000-0000-0000-0000-00000000000a', 'storage', 'storage_503');

select is(
    (select row(completed_steps, last_error, claimed_at is null)::text from public.account_deletions where user_id = '00000000-0000-0000-0000-00000000000a'),
    row(array['onesignal', 'apple'], 'storage_503', true)::text,
    'a failure records last_error, releases the lease and keeps the completed steps'
);

select throws_ok(
    $$ select public.record_account_purge_step('00000000-0000-0000-0000-00000000000a', 'bogus', null) $$,
    '22023', null,
    'an unknown step is rejected'
);

-- Purging A's data.
select lives_ok(
    $$ select public.purge_account_data('00000000-0000-0000-0000-00000000000a') $$,
    'purge_account_data runs for A'
);

select is((select count(*)::int from public.user_profiles where id = '00000000-0000-0000-0000-00000000000a'), 0, 'A''s profile is gone');
select is((select count(*)::int from public.groups where owner_user_id = '00000000-0000-0000-0000-00000000000a'), 0, 'A''s owned groups are gone');
select is((select count(*)::int from public.memberships where group_id = '00000000-0000-0000-0000-0000000000a1'), 0, 'G''s memberships are gone, B''s included');
select is((select count(*)::int from public.memberships where member_user_id = '00000000-0000-0000-0000-00000000000a'), 0, 'A''s memberships elsewhere are gone');
select is((select count(*)::int from public.meetups where group_id = '00000000-0000-0000-0000-0000000000a1' or created_by_user_id = '00000000-0000-0000-0000-00000000000a'), 0, 'G''s meetups and the meetups A created are gone');
select is(
    (select count(*)::int from public.invitations
     where group_id = '00000000-0000-0000-0000-0000000000a1'
        or invited_by_user_id = '00000000-0000-0000-0000-00000000000a'
        or invited_user_id = '00000000-0000-0000-0000-00000000000a'
        or lower(invited_email) = 'a@example.com'),
    0,
    'invitations A sent or received, by id or by email in any case, are gone'
);
select is((select count(*)::int from public.user_devices where user_id = '00000000-0000-0000-0000-00000000000a'), 0, 'A''s devices are gone');
select is((select count(*)::int from public.notification_delivery_attempts where user_id = '00000000-0000-0000-0000-00000000000a'), 0, 'A''s delivery attempts are gone');
select is((select count(*)::int from public.notification_open_events where user_id = '00000000-0000-0000-0000-00000000000a'), 0, 'A''s open events are gone');
select is((select count(*)::int from public.apple_authorizations where user_id = '00000000-0000-0000-0000-00000000000a'), 0, 'A''s Apple authorizations are gone');

-- Everyone else's data stays. Reads are limited to the fixture's ids so rows already in a
-- local database don't affect the result.
select set_eq(
    $$ select id from public.groups where id::text like '00000000-0000-0000-0000-%' $$,
    $$ values ('00000000-0000-0000-0000-0000000000c1'::uuid), ('00000000-0000-0000-0000-0000000000d1'::uuid) $$,
    'H and K remain'
);
select set_eq(
    $$ select group_id, member_user_id from public.memberships where group_id::text like '00000000-0000-0000-0000-%' $$,
    $$ values
        ('00000000-0000-0000-0000-0000000000c1'::uuid, '00000000-0000-0000-0000-00000000000c'::uuid),
        ('00000000-0000-0000-0000-0000000000d1'::uuid, '00000000-0000-0000-0000-00000000000d'::uuid),
        ('00000000-0000-0000-0000-0000000000d1'::uuid, '00000000-0000-0000-0000-00000000000b'::uuid) $$,
    'the other memberships remain'
);
select set_eq(
    $$ select id from public.meetups where id::text like '00000000-0000-0000-0000-%' $$,
    $$ values ('00000000-0000-0000-0000-0000000001cc'::uuid), ('00000000-0000-0000-0000-0000000001d1'::uuid) $$,
    'C''s and D''s meetups remain'
);
select set_eq(
    $$ select id from public.invitations where id::text like '00000000-0000-0000-0000-%' $$,
    $$ values ('00000000-0000-0000-0000-0000000002dc'::uuid) $$,
    'D''s invitation to C remains'
);
select is((select count(*)::int from public.user_profiles where id::text like '00000000-0000-0000-0000-%'), 3, 'the other profiles remain');
select is((select count(*)::int from public.user_devices where user_id = '00000000-0000-0000-0000-00000000000b'), 1, 'B''s device remains');
select is((select count(*)::int from public.notification_delivery_attempts where user_id = '00000000-0000-0000-0000-00000000000b'), 1, 'B''s delivery attempt remains');
select is((select count(*)::int from public.notification_open_events where user_id = '00000000-0000-0000-0000-00000000000b'), 1, 'B''s open event remains');
select is((select count(*)::int from public.notification_events where id::text like '00000000-0000-0000-0000-%'), 1, 'notification events, which aren''t keyed by user, remain');
select is((select count(*)::int from public.account_deletions where user_id = '00000000-0000-0000-0000-00000000000a'), 1, 'the deletion row survives until the auth step');

-- Idempotent: a second run succeeds and changes nothing.
create temporary table purge_counts as
    select 'groups' as kind, count(*) as n from public.groups
    union all select 'memberships', count(*) from public.memberships
    union all select 'meetups', count(*) from public.meetups
    union all select 'invitations', count(*) from public.invitations
    union all select 'user_profiles', count(*) from public.user_profiles
    union all select 'user_devices', count(*) from public.user_devices
    union all select 'notification_delivery_attempts', count(*) from public.notification_delivery_attempts
    union all select 'notification_open_events', count(*) from public.notification_open_events;

select lives_ok(
    $$ select public.purge_account_data('00000000-0000-0000-0000-00000000000a') $$,
    'a second purge_account_data run succeeds'
);
select set_eq(
    $$ select 'groups' as kind, count(*) as n from public.groups
       union all select 'memberships', count(*) from public.memberships
       union all select 'meetups', count(*) from public.meetups
       union all select 'invitations', count(*) from public.invitations
       union all select 'user_profiles', count(*) from public.user_profiles
       union all select 'user_devices', count(*) from public.user_devices
       union all select 'notification_delivery_attempts', count(*) from public.notification_delivery_attempts
       union all select 'notification_open_events', count(*) from public.notification_open_events $$,
    $$ select kind, n from purge_counts $$,
    'the second run changes nothing'
);

-- The auth step: deleting the auth user cascades away the deletion row and any Apple
-- authorization stored after the database step.
reset role;
insert into public.apple_authorizations (user_id, client_id, refresh_token) values
    ('00000000-0000-0000-0000-00000000000a', 'io.loopmeet.web', 'r-a-web');
delete from auth.users where id = '00000000-0000-0000-0000-00000000000a';

select is((select count(*)::int from public.account_deletions where user_id = '00000000-0000-0000-0000-00000000000a'), 0, 'deleting the auth user removes the deletion row');
select is((select count(*)::int from public.apple_authorizations where user_id = '00000000-0000-0000-0000-00000000000a'), 0, 'deleting the auth user removes Apple authorizations');

select * from finish();
rollback;
