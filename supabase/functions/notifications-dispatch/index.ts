import { createClient } from "@supabase/supabase-js";
import type { NotificationType } from "../_shared/notification-contract.ts";
import { dispatchNotification } from "./dispatch-service.ts";
import { resolveRecipients } from "./recipient-resolver.ts";
import { resolveNotificationType, type WebhookPayload } from "./webhook-router.ts";

const supabase = createClient(
  Deno.env.get("SUPABASE_URL") ?? "",
  Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "",
);

interface NotificationCopy {
  title: string;
  body: string;
}

const invitationCopy = async (record: Record<string, unknown>): Promise<NotificationCopy> => {
  const groupId = typeof record.group_id === "string" ? record.group_id : null;
  const invitedByUserId = typeof record.invited_by_user_id === "string" ? record.invited_by_user_id : null;

  let groupName = "your";
  if (groupId) {
    const { data } = await supabase
      .from("groups")
      .select("name")
      .eq("id", groupId)
      .maybeSingle();
    if (typeof data?.name === "string" && data.name.trim().length > 0) {
      groupName = data.name.trim();
    }
  }

  let ownerName = "the group owner";
  if (invitedByUserId) {
    const { data } = await supabase
      .from("user_profiles")
      .select("display_name,email")
      .eq("id", invitedByUserId)
      .maybeSingle();

    const displayName = typeof data?.display_name === "string" ? data.display_name.trim() : "";
    const email = typeof data?.email === "string" ? data.email.trim() : "";
    if (displayName.length > 0) {
      ownerName = displayName;
    } else if (email.length > 0) {
      ownerName = email;
    }
  }

  return {
    title: "LoopMeet - New Invitation!",
    body: `You have been invited to the ${groupName} group by ${ownerName}`,
  };
};

const buildNotificationCopy = async (
  notificationType: NotificationType,
  record: Record<string, unknown>,
): Promise<NotificationCopy> => {
  const groupId = typeof record.group_id === "string" ? record.group_id : null;
  let groupName = "one of your groups";
  if (groupId) {
    const { data } = await supabase
      .from("groups")
      .select("name")
      .eq("id", groupId)
      .maybeSingle();
    if (typeof data?.name === "string" && data.name.trim().length > 0) {
      groupName = data.name.trim();
    }
  }

  switch (notificationType) {
    case "invitation.new":
      return invitationCopy(record);
    case "meetup.created":
      return { title: "LoopMeet - New Meetup", body: `A new meetup has been scheduled in ${groupName}.` };
    case "meetup.updated":
      return { title: "LoopMeet - Meetup Updated", body: `A meetup in ${groupName} has new details.` };
    case "meetup.canceled":
      return { title: "LoopMeet - Meetup Canceled", body: `A meetup in ${groupName} was canceled.` };
    case "meetup.today_reminder":
      return { title: "LoopMeet - Reminder", body: `You have a meetup happening today in ${groupName}.` };
  }
};

const buildEventId = (payload: WebhookPayload, notificationType: string) => {
  const id = payload.record.id;
  const rowId = typeof id === "string" ? id : crypto.randomUUID();
  return `${payload.table}:${notificationType}:${rowId}`;
};

Deno.serve(async (request) => {
  if (request.method !== "POST") {
    return Response.json({ error: "method_not_allowed" }, { status: 405 });
  }

  const payload = (await request.json()) as WebhookPayload;
  const notificationType = resolveNotificationType(payload);

  if (!notificationType) {
    return Response.json({ skipped: true, reason: "unsupported_event" });
  }

  const recipients = await resolveRecipients({ table: payload.table, record: payload.record });
  if (recipients.length === 0) {
    return Response.json({ skipped: true, reason: "no_recipients" });
  }

  const groupId = payload.record.group_id;
  const targetId = typeof groupId === "string" ? groupId : null;
  const copy = await buildNotificationCopy(notificationType, payload.record);

  const result = await dispatchNotification({
    notificationType,
    targetId,
    eventId: buildEventId(payload, notificationType),
    recipients,
    title: copy.title,
    body: copy.body,
  });

  return Response.json(result);
});
