import { dispatchNotification } from "./dispatch-service.ts";
import { resolveRecipients } from "./recipient-resolver.ts";
import { resolveNotificationType, type WebhookPayload } from "./webhook-router.ts";

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

  const result = await dispatchNotification({
    notificationType,
    targetId,
    eventId: buildEventId(payload, notificationType),
    recipients,
    title: "LoopMeet update",
    body: "You have a new group update",
  });

  return Response.json(result);
});
