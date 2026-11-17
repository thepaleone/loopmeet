begin;

create extension if not exists pgtap with schema extensions;

select plan(12);

-- A owns "Book club" with members B, C and D. E is an outsider. D is pending deletion.
insert into auth.users (id, email) values
    ('00000000-0000-0000-0000-00000000000a', 'a@example.com'),
    ('00000000-0000-0000-0000-00000000000b', 'b@example.com'),
    ('00000000-0000-0000-0000-00000000000c', 'c@example.com'),
    ('00000000-0000-0000-0000-00000000000d', 'd@example.com'),
    ('00000000-0000-0000-0000-00000000000e', 'e@example.com');

insert into public.user_profiles (id, email, display_name) values
    ('00000000-0000-0000-0000-00000000000a', 'a@example.com', 'Ann'),
    ('00000000-0000-0000-0000-00000000000b', 'b@example.com', 'Ben'),
    ('00000000-0000-0000-0000-00000000000c', 'c@example.com', 'Cat'),
    ('00000000-0000-0000-0000-00000000000d', 'd@example.com', 'Dee'),
    ('00000000-0000-0000-0000-00000000000e', 'e@example.com', 'Eve');

insert into public.groups (id, owner_user_id, name) values
    ('00000000-0000-0000-0000-0000000000a1', '00000000-0000-0000-0000-00000000000a', 'Book club');

insert into public.memberships (group_id, member_user_id, role) values
    ('00000000-0000-0000-0000-0000000000a1', '00000000-0000-0000-0000-00000000000a', 'owner'),
    ('00000000-0000-0000-0000-0000000000a1', '00000000-0000-0000-0000-00000000000b', 'member'),
    ('00000000-0000-0000-0000-0000000000a1', '00000000-0000-0000-0000-00000000000c', 'member'),
    ('00000000-0000-0000-0000-0000000000a1', '00000000-0000-0000-0000-00000000000d', 'member');

-- B organised a meetup before leaving (FR-012: it stays with the group).
insert into public.meetups (id, group_id, created_by_user_id, title, scheduled_at) values
    ('00000000-0000-0000-0000-0000000000f1', '00000000-0000-0000-0000-0000000000a1',
     '00000000-0000-0000-0000-00000000000b', 'Next chapter', now() + interval '7 days');

insert into public.account_deletions (user_id, requested_at, purge_after, status) values
    ('00000000-0000-0000-0000-00000000000d', now(), now() + interval '30 days', 'pending');

select ok(
    exists (select 1 from public.active_memberships
            where group_id = '00000000-0000-0000-0000-0000000000a1'
              and member_user_id = '00000000-0000-0000-0000-00000000000b'),
    'before leaving, B is an active member'
);

-- B leaves (FR-010)

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000b","role":"authenticated"}', true);
set local role authenticated;

select lives_ok(
    $$ select public.leave_group('00000000-0000-0000-0000-0000000000a1') $$,
    'a member leaves the group'
);

select throws_ok(
    $$ select public.leave_group('00000000-0000-0000-0000-0000000000a1') $$,
    'P0001', 'not_group_member',
    'leaving twice is refused as not a member'
);

select is_empty(
    $$ select 1 from public.groups where id = '00000000-0000-0000-0000-0000000000a1' $$,
    'the leaver no longer sees the group'
);

-- The others

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000a","role":"authenticated"}', true);

select throws_ok(
    $$ select public.leave_group('00000000-0000-0000-0000-0000000000a1') $$,
    'P0001', 'owner_cannot_leave',
    'the owner cannot leave their own group'
);

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000e","role":"authenticated"}', true);

select throws_ok(
    $$ select public.leave_group('00000000-0000-0000-0000-0000000000a1') $$,
    'P0001', 'not_group_member',
    'an outsider cannot leave'
);

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000d","role":"authenticated"}', true);

select throws_ok(
    $$ select public.leave_group('00000000-0000-0000-0000-0000000000a1') $$,
    'P0001', 'account_inactive',
    'an account pending deletion cannot leave'
);

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000c","role":"authenticated"}', true);

select results_eq(
    $$ select title::text from public.meetups where id = '00000000-0000-0000-0000-0000000000f1' $$,
    $$ values ('Next chapter') $$,
    'a remaining member still sees the meetup the leaver organised'
);

reset role;

select results_eq(
    $$ select member_user_id from public.memberships
       where group_id = '00000000-0000-0000-0000-0000000000a1' order by member_user_id $$,
    $$ values ('00000000-0000-0000-0000-00000000000a'::uuid),
              ('00000000-0000-0000-0000-00000000000c'::uuid),
              ('00000000-0000-0000-0000-00000000000d'::uuid) $$,
    'only the leaver''s membership was deleted'
);

select ok(
    not exists (select 1 from public.active_memberships
                where member_user_id = '00000000-0000-0000-0000-00000000000b'),
    'the leaver drops out of active_memberships, so their notifications stop'
);

select ok(
    has_function_privilege('authenticated', 'public.leave_group(uuid)', 'execute'),
    'authenticated may call leave_group'
);

select ok(
    not has_function_privilege('anon', 'public.leave_group(uuid)', 'execute'),
    'anon may not call leave_group'
);

select * from finish();

rollback;
