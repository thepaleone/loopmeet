begin;

create extension if not exists pgtap with schema extensions;

select plan(23);

-- Users A (requests deletion), B (also pending, to check row isolation) and C (stays active).
insert into auth.users (id, email) values
    ('00000000-0000-0000-0000-00000000000a', 'a@example.com'),
    ('00000000-0000-0000-0000-00000000000b', 'b@example.com'),
    ('00000000-0000-0000-0000-00000000000c', 'c@example.com');

insert into public.groups (id, owner_user_id, name) values
    ('00000000-0000-0000-0000-0000000000a1', '00000000-0000-0000-0000-00000000000a', 'A group'),
    ('00000000-0000-0000-0000-0000000000c1', '00000000-0000-0000-0000-00000000000c', 'C group');

-- Request (FR-003, FR-005)

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000a","role":"authenticated"}', true);
set local role authenticated;

select is(
    (select purge_after - requested_at from public.request_account_deletion()),
    interval '30 days',
    'request_account_deletion schedules purge exactly 30 days after the request'
);

select is(
    (select status from public.account_deletions where user_id = '00000000-0000-0000-0000-00000000000a'),
    'pending',
    'a new request is pending'
);

reset role;
update public.account_deletions
set requested_at = requested_at - interval '1 day', purge_after = purge_after - interval '1 day'
where user_id = '00000000-0000-0000-0000-00000000000a';
set local role authenticated;

select is(
    (select requested_at from public.request_account_deletion()),
    (now() - interval '1 day'),
    'a repeat request returns the original row unchanged'
);

-- Row access

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000b","role":"authenticated"}', true);
select lives_ok('select public.request_account_deletion()', 'B can request deletion');

select is(
    (select array_agg(user_id) from public.account_deletions),
    array['00000000-0000-0000-0000-00000000000b'::uuid],
    'a user reads only their own deletion row'
);

select throws_ok(
    $$insert into public.account_deletions (user_id, requested_at, purge_after, status)
      values ('00000000-0000-0000-0000-00000000000c', now(), now(), 'pending')$$,
    '42501',
    null,
    'authenticated cannot insert deletion rows directly'
);

select throws_ok(
    $$update public.account_deletions set purge_after = now() + interval '1 year'$$,
    '42501',
    null,
    'authenticated cannot update deletion rows directly'
);

select throws_ok(
    $$delete from public.account_deletions$$,
    '42501',
    null,
    'authenticated cannot delete deletion rows directly'
);

-- Active-account helpers, as an unrelated active user

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000c","role":"authenticated"}', true);

select is(private.account_is_active('00000000-0000-0000-0000-00000000000a'), false, 'account_is_active is false for a pending user');
select is(private.account_is_active('00000000-0000-0000-0000-00000000000c'), true, 'account_is_active is true for an active user');
select is(private.group_is_active('00000000-0000-0000-0000-0000000000a1'), false, 'group_is_active is false for a group owned by a pending user');
select is(private.group_is_active('00000000-0000-0000-0000-0000000000c1'), true, 'group_is_active is true for a group owned by an active user');
select is(private.email_is_active('A@Example.COM'), false, 'email_is_active is false for a pending user''s email, ignoring case');
select is(private.email_is_active('c@example.com'), true, 'email_is_active is true for an active user''s email');

-- Restore (FR-011, FR-012)

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000a","role":"authenticated"}', true);

select is(public.restore_account_deletion(), true, 'restore removes a pending request and returns true');
select is(private.account_is_active('00000000-0000-0000-0000-00000000000a'), true, 'the account is active again after restore');
select is(public.restore_account_deletion(), false, 'restore with nothing pending returns false');

select lives_ok('select public.request_account_deletion()', 'A requests deletion again');
reset role;
update public.account_deletions
set purge_after = now() - interval '1 second'
where user_id = '00000000-0000-0000-0000-00000000000a';
set local role authenticated;

select throws_ok(
    'select public.restore_account_deletion()',
    'P0001',
    'deletion_in_progress',
    'restore is refused once the deletion date has passed'
);

reset role;
update public.account_deletions
set purge_after = now() + interval '1 day', status = 'purging'
where user_id = '00000000-0000-0000-0000-00000000000a';
set local role authenticated;

select throws_ok(
    'select public.restore_account_deletion()',
    'P0001',
    'deletion_in_progress',
    'restore is refused while purging'
);

-- Grants and exposure

reset role;
set local role anon;

select throws_ok('select public.request_account_deletion()', '42501', null, 'anon cannot request deletion');
select throws_ok('select public.restore_account_deletion()', '42501', null, 'anon cannot restore');

reset role;

select is(
    (select array_agg(distinct pronamespace::regnamespace::text)
     from pg_proc
     where proname in ('account_is_active', 'group_is_active', 'email_is_active')),
    array['private'],
    'the active-account helpers live in the unexposed private schema'
);

select * from finish();

rollback;
