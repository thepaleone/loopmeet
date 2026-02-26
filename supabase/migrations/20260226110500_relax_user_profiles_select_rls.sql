drop policy if exists "user_profiles_select_authenticated"
    on user_profiles;

create policy "user_profiles_select_authenticated"
    on user_profiles
    for select
    using (auth.uid() is not null);
