import { createClient } from "@supabase/supabase-js";
import { cleanupStaleDevices } from "./stale-device-cleanup.ts";

const supabase = createClient(
  Deno.env.get("SUPABASE_URL") ?? "",
  Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "",
);

const notificationsDispatchUrl = Deno.env.get("NOTIFICATIONS_DISPATCH_URL") ?? "";

const isWithinLocalMorningWindow = () => {
  const now = new Date();
  const hour = now.getHours();
  return hour >= 8 && hour < 10;
};

Deno.serve(async () => {
  await cleanupStaleDevices();

  if (!isWithinLocalMorningWindow()) {
    return Response.json({ skipped: true, reason: "outside_window" });
  }

  const today = new Date().toISOString().slice(0, 10);
  const { data, error } = await supabase
    .from("meetups")
    .select("id,group_id")
    .gte("scheduled_at", `${today}T00:00:00Z`)
    .lt("scheduled_at", `${today}T23:59:59Z`);

  if (error) {
    return Response.json({ error: error.message }, { status: 500 });
  }

  for (const meetup of data ?? []) {
    await fetch(notificationsDispatchUrl, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        type: "INSERT",
        table: "meetups",
        record: { id: meetup.id, group_id: meetup.group_id, status: "scheduled" },
        old_record: null,
      }),
    });
  }

  return Response.json({ queued: (data ?? []).length });
});
