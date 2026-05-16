import { OneSignalClient } from "../_shared/onesignal-client.ts";
import { resolveAndroidChannelId } from "../_shared/notification-channels.ts";
import type { NotificationType } from "../_shared/notification-contract.ts";
import { buildPayload } from "./payload-builder.ts";

const oneSignalClient = new OneSignalClient(Deno.env.get("ONESIGNAL_REST_API_KEY") ?? "");

export interface DispatchInput {
  notificationType: NotificationType;
  targetId: string | null;
  eventId: string;
  recipients: string[];
  title: string;
  body: string;
}

export const dispatchNotification = async (input: DispatchInput) => {
  const payload = buildPayload(input.notificationType, input.targetId, input.eventId);
  const appId = Deno.env.get("ONESIGNAL_APP_ID") ?? "";
  const correlationId = crypto.randomUUID();

  console.log(JSON.stringify({
    level: "info",
    message: "notification_dispatch_start",
    correlation_id: correlationId,
    event_id: input.eventId,
    notification_type: input.notificationType,
    recipient_count: input.recipients.length,
  }));

  const androidChannelId = resolveAndroidChannelId(input.notificationType);
  const response = await oneSignalClient.send({
    app_id: appId,
    include_external_user_ids: input.recipients,
    headings: { en: input.title },
    contents: { en: input.body },
    data: payload as unknown as Record<string, unknown>,
    ...(androidChannelId ? { android_channel_id: androidChannelId } : {}),
  });

  return {
    event_id: input.eventId,
    correlation_id: correlationId,
    notification_type: input.notificationType,
    recipient_count: input.recipients.length,
    send_attempts: input.recipients.length,
    failed_attempts: response.errors ? input.recipients.length : 0,
    provider_message_id: response.id,
  };
};
