create extension if not exists pg_cron;
create extension if not exists pg_net;
-create extension if not exists vault;

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
    if vault_project_url is null or vault_project_url = '' then
        raise exception 'Missing vault secret: project_url';
    end if;

    if vault_publishable_key is null or vault_publishable_key = '' then
        raise exception 'Missing vault secret: publishable_key';
    end if;

    if exists (select 1 from cron.job where jobname = 'reminders-scheduler-every-15-minutes') then
        perform cron.unschedule('reminders-scheduler-every-15-minutes');
    end if;

    perform cron.schedule(
        'reminders-scheduler-every-15-minutes',
        '*/15 * * * *',
        $$
        select
            net.http_post(
                url := (select decrypted_secret from vault.decrypted_secrets where name = 'project_url') || '/functions/v1/reminders-scheduler',
                headers := jsonb_build_object(
                    'Content-Type', 'application/json',
                    'Authorization', 'Bearer ' || (select decrypted_secret from vault.decrypted_secrets where name = 'publishable_key')
                ),
                body := '{}'::jsonb
            ) as request_id;
        $$
    );
end
$$;
