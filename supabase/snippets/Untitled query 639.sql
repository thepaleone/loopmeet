-- Example for auth.sessions.user_id
ALTER TABLE auth.refresh_tokens
  ALTER COLUMN user_id TYPE uuid USING user_id::uuid;