import { OneSignalClient } from "../_shared/onesignal-client.ts";
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

  const response = await oneSignalClient.send({
    app_id: appId,
    include_external_user_ids: input.recipients,
    headings: { en: input.title },
    contents: { en: input.body },
    data: payload,
  });

  return {
    event_id: input.eventId,
    notification_type: input.notificationType,
    recipient_count: input.recipients.length,
    send_attempts: input.recipients.length,
    failed_attempts: response.errors ? input.recipients.length : 0,
    provider_message_id: response.id,
  };
};
