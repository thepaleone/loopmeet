// The full npm specifier, not the "@supabase/supabase-js" import-map alias: `deno test` run from
// the repo root ignores the per-function deno.json files, and the resolver tests import this module.
import type { SupabaseClient } from "npm:@supabase/supabase-js@2";

// An account is active unless it has an account_deletions row (pending or purging).
export const isAccountActive = async (client: SupabaseClient, userId: string): Promise<boolean> => {
  const { data, error } = await client
    .from("account_deletions")
    .select("user_id")
    .eq("user_id", userId)
    .maybeSingle();

  if (error) {
    throw new Error(`account_status_failed:${error.message}`);
  }

  return data === null;
};
