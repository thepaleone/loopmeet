begin;

create extension if not exists pgtap with schema extensions;

select plan(38);

-- A requests deletion. B is in A's group G. C owns H, where A is a member and created a meetup.
-- D is unrelated and owns K, where B is a member: the control that must stay visible throughout.
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

-- A invites B to G, and C invites A's email (no user id yet) to H.
insert into public.invitations (id, group_id, invited_email, invited_user_id, invited_by_user_id, status) values
    ('00000000-0000-0000-0000-0000000002ab', '00000000-0000-0000-0000-0000000000a1', 'b@example.com', '00000000-0000-0000-0000-00000000000b', '00000000-0000-0000-0000-00000000000a', 'pending'),
    ('00000000-0000-0000-0000-0000000002ca', '00000000-0000-0000-0000-0000000000c1', 'a@example.com', null, '00000000-0000-0000-0000-00000000000c', 'pending');

-- Everything the current role can see, for comparing a viewer's view before and after.
-- Created inside this transaction, so it's rolled back with the fixture.
create function public.test_visible_rows()
returns table (kind text, row_data jsonb)
language sql
stable
as $$
    select 'groups', to_jsonb(g) from public.groups g
    union all select 'memberships', to_jsonb(m) from public.memberships m
    union all select 'invitations', to_jsonb(i) from public.invitations i
    union all select 'meetups', to_jsonb(e) from public.meetups e
    union all select 'user_profiles', to_jsonb(p) from public.user_profiles p
$$;
grant execute on function public.test_visible_rows() to authenticated;

create table public.test_visibility_snapshot (viewer text, kind text, row_data jsonb);
grant select, insert on public.test_visibility_snapshot to authenticated;

set local role authenticated;

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000b","email":"b@example.com","role":"authenticated"}', true);
insert into public.test_visibility_snapshot select 'b', * from public.test_visible_rows();

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000c","email":"c@example.com","role":"authenticated"}', true);
insert into public.test_visibility_snapshot select 'c', * from public.test_visible_rows();

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000d","email":"d@example.com","role":"authenticated"}', true);
insert into public.test_visibility_snapshot select 'd', * from public.test_visible_rows();

-- Sanity: before the request, B sees G and the invitation, and C sees A in H.
select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000b","email":"b@example.com","role":"authenticated"}', true);
select is(
    (select count(*)::int from public.groups where id = '00000000-0000-0000-0000-0000000000a1'),
    1,
    'before the request B sees G'
);

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000c","email":"c@example.com","role":"authenticated"}', true);
select is(
    (select count(*)::int from public.memberships where group_id = '00000000-0000-0000-0000-0000000000c1'),
    2,
    'before the request C sees both memberships of H'
);

-- A requests deletion.
select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000a","email":"a@example.com","role":"authenticated"}', true);
select lives_ok('select public.request_account_deletion()', 'A requests deletion');

-- Hidden from B (FR-006)

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000b","email":"b@example.com","role":"authenticated"}', true);

select is_empty(
    $$select 1 from public.groups where id = '00000000-0000-0000-0000-0000000000a1'$$,
    'B no longer sees G'
);
select is_empty(
    $$select 1 from public.meetups where group_id = '00000000-0000-0000-0000-0000000000a1'$$,
    'B no longer sees G''s meetups'
);
select is_empty(
    $$select 1 from public.memberships where group_id = '00000000-0000-0000-0000-0000000000a1'$$,
    'B no longer sees G''s memberships, including its own'
);
select is_empty(
    $$select 1 from public.invitations where id = '00000000-0000-0000-0000-0000000002ab'$$,
    'B no longer sees A''s invitation'
);
select is_empty(
    $$select 1 from public.user_profiles where id = '00000000-0000-0000-0000-00000000000a'$$,
    'B no longer sees A''s profile'
);

-- Writes to hidden rows affect nothing (FR-007).
with changed as (
    update public.meetups set title = 'changed'
    where id = '00000000-0000-0000-0000-0000000001a1'
    returning 1
)
select is(count(*)::int, 0, 'B''s update of G''s meetup affects no rows') from changed;
with removed as (
    delete from public.meetups
    where id = '00000000-0000-0000-0000-0000000001a1'
    returning 1
)
select is(count(*)::int, 0, 'B''s delete of G''s meetup affects no rows') from removed;
with accepted as (
    update public.invitations set status = 'accepted'
    where id = '00000000-0000-0000-0000-0000000002ab'
    returning 1
)
select is(count(*)::int, 0, 'B can''t accept A''s invitation') from accepted;

-- The control: B still sees D's group K, its meetup and its memberships.
select is(
    (select count(*)::int from public.groups where id = '00000000-0000-0000-0000-0000000000d1'),
    1,
    'B still sees the unrelated group K'
);
select is(
    (select count(*)::int from public.meetups where group_id = '00000000-0000-0000-0000-0000000000d1'),
    1,
    'B still sees K''s meetup'
);
select is(
    (select count(*)::int from public.memberships where group_id = '00000000-0000-0000-0000-0000000000d1'),
    2,
    'B still sees K''s memberships'
);

-- Hidden from C (FR-006)

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000c","email":"c@example.com","role":"authenticated"}', true);

select is_empty(
    $$select 1 from public.groups where id = '00000000-0000-0000-0000-0000000000a1'$$,
    'C doesn''t see G'
);
select results_eq(
    $$select member_user_id from public.memberships where group_id = '00000000-0000-0000-0000-0000000000c1'$$,
    $$values ('00000000-0000-0000-0000-00000000000c'::uuid)$$,
    'C no longer sees A''s membership in H'
);
select results_eq(
    $$select id from public.meetups where group_id = '00000000-0000-0000-0000-0000000000c1'$$,
    $$values ('00000000-0000-0000-0000-0000000001cc'::uuid)$$,
    'C sees its own meetup in H but not A''s'
);
select is_empty(
    $$select 1 from public.invitations where id = '00000000-0000-0000-0000-0000000002ca'$$,
    'C no longer sees its invitation to A''s email'
);
select is_empty(
    $$select 1 from public.user_profiles where id = '00000000-0000-0000-0000-00000000000a'$$,
    'C no longer sees A''s profile'
);
with changed as (
    update public.meetups set title = 'changed'
    where id = '00000000-0000-0000-0000-0000000001ca'
    returning 1
)
select is(count(*)::int, 0, 'C''s update of A''s meetup in H affects no rows') from changed;

-- D sees exactly what it saw before, apart from A's profile.
select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000d","email":"d@example.com","role":"authenticated"}', true);
select set_eq(
    'select kind, row_data from public.test_visible_rows()',
    $$select kind, row_data from public.test_visibility_snapshot
      where viewer = 'd'
        and not (kind = 'user_profiles' and row_data->>'id' = '00000000-0000-0000-0000-00000000000a')$$,
    'the unrelated user D loses sight of A''s profile and nothing else'
);

-- A can't create anything while pending (FR-009)

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000a","email":"a@example.com","role":"authenticated"}', true);

select throws_ok(
    $$insert into public.groups (owner_user_id, name) values ('00000000-0000-0000-0000-00000000000a', 'New')$$,
    '42501',
    null,
    'A can''t create a group'
);
select throws_ok(
    $$insert into public.meetups (group_id, created_by_user_id, title, scheduled_at)
      values ('00000000-0000-0000-0000-0000000000c1', '00000000-0000-0000-0000-00000000000a', 'New', now() + interval '1 day')$$,
    '42501',
    null,
    'A can''t create a meetup in H'
);
select throws_ok(
    $$insert into public.invitations (group_id, invited_email, invited_by_user_id, status)
      values ('00000000-0000-0000-0000-0000000000a1', 'new@example.com', '00000000-0000-0000-0000-00000000000a', 'pending')$$,
    '42501',
    null,
    'A can''t invite anyone to G'
);
select throws_ok(
    $$insert into public.memberships (group_id, member_user_id, role)
      values ('00000000-0000-0000-0000-0000000000d1', '00000000-0000-0000-0000-00000000000a', 'member')$$,
    '42501',
    null,
    'A can''t add a membership'
);
select throws_ok(
    $$insert into public.user_profiles (id, email) values ('00000000-0000-0000-0000-00000000000a', 'a@example.com')$$,
    '42501',
    null,
    'A can''t create a profile'
);
with changed as (
    update public.user_profiles set display_name = 'changed'
    where id = '00000000-0000-0000-0000-00000000000a'
    returning 1
)
select is(count(*)::int, 0, 'A can''t update its profile') from changed;

-- active_memberships (FR-008)

reset role;

select set_eq(
    $$select group_id, member_user_id from public.active_memberships
      where group_id in ('00000000-0000-0000-0000-0000000000a1', '00000000-0000-0000-0000-0000000000c1', '00000000-0000-0000-0000-0000000000d1')$$,
    $$values
        ('00000000-0000-0000-0000-0000000000c1'::uuid, '00000000-0000-0000-0000-00000000000c'::uuid),
        ('00000000-0000-0000-0000-0000000000d1'::uuid, '00000000-0000-0000-0000-00000000000d'::uuid),
        ('00000000-0000-0000-0000-0000000000d1'::uuid, '00000000-0000-0000-0000-00000000000b'::uuid)$$,
    'active_memberships excludes A''s memberships and every membership of G while A is pending'
);

set local role service_role;
select lives_ok('select 1 from public.active_memberships', 'service_role can read active_memberships');
reset role;

set local role authenticated;
select throws_ok('select 1 from public.active_memberships', '42501', null, 'authenticated can''t read active_memberships');
reset role;

set local role anon;
select throws_ok('select 1 from public.active_memberships', '42501', null, 'anon can''t read active_memberships');
select is(
    (select count(*)::int from public.groups),
    0,
    'anon still sees no groups, without an error from the private helpers'
);
reset role;

-- Restore brings everything back unchanged (FR-012)

set local role authenticated;
select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000a","email":"a@example.com","role":"authenticated"}', true);
select is(public.restore_account_deletion(), true, 'A restores the account');

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000b","email":"b@example.com","role":"authenticated"}', true);
select set_eq(
    'select kind, row_data from public.test_visible_rows()',
    $$select kind, row_data from public.test_visibility_snapshot where viewer = 'b'$$,
    'after restore B sees exactly the rows and values it saw before'
);

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000c","email":"c@example.com","role":"authenticated"}', true);
select set_eq(
    'select kind, row_data from public.test_visible_rows()',
    $$select kind, row_data from public.test_visibility_snapshot where viewer = 'c'$$,
    'after restore C sees exactly the rows and values it saw before'
);

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000d","email":"d@example.com","role":"authenticated"}', true);
select set_eq(
    'select kind, row_data from public.test_visible_rows()',
    $$select kind, row_data from public.test_visibility_snapshot where viewer = 'd'$$,
    'after restore D sees exactly the rows and values it saw before'
);

select set_config('request.jwt.claims', '{"sub":"00000000-0000-0000-0000-00000000000a","email":"a@example.com","role":"authenticated"}', true);
select lives_ok(
    $$insert into public.groups (owner_user_id, name) values ('00000000-0000-0000-0000-00000000000a', 'After restore')$$,
    'after restore A can create a group again'
);

reset role;

select set_eq(
    $$select group_id, member_user_id from public.active_memberships
      where group_id in ('00000000-0000-0000-0000-0000000000a1', '00000000-0000-0000-0000-0000000000c1', '00000000-0000-0000-0000-0000000000d1')$$,
    $$select group_id, member_user_id from public.memberships
      where group_id in ('00000000-0000-0000-0000-0000000000a1', '00000000-0000-0000-0000-0000000000c1', '00000000-0000-0000-0000-0000000000d1')$$,
    'after restore active_memberships includes A''s memberships and G again'
);

select * from finish();

rollback;
