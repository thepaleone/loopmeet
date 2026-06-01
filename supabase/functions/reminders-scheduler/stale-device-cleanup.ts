import { createClient } from "@supabase/supabase-js";

const supabase = createClient(
  Deno.env.get("SUPABASE_URL") ?? "",
  Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "",
);

export const cleanupStaleDevices = async () => {
  const staleThreshold = new Date(Date.now() - 1000 * 60 * 60 * 24 * 90).toISOString();

  const { data, error } = await supabase
    .from("user_devices")
    .update({ invalidated_at: new Date().toISOString() })
    .is("invalidated_at", null)
    .lt("last_seen_at", staleThreshold)
    .select("id");

  if (error) {
    throw new Error(`stale_device_cleanup_failed:${error.message}`);
  }

  return { cleaned_count: (data ?? []).length };
};
