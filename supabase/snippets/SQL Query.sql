do $$
DECLARE uid uuid := '0b939b06-1e79-4eb3-9af0-f10972b46efa';
BEGIN
  DELETE FROM auth.sessions        WHERE user_id = uid;
  DELETE FROM auth.identities      WHERE user_id = uid;
  DELETE FROM auth.mfa_factors     WHERE user_id = uid;
  DELETE FROM auth.refresh_tokens  WHERE user_id = uid;
  DELETE FROM auth.one_time_tokens WHERE user_id = uid;
  DELETE FROM auth.users           WHERE id      = uid; -- users.id might be uuid
END $$;