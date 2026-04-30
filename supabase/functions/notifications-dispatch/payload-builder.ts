import type { NotificationPayload, NotificationType } from "../_shared/notification-contract.ts";
import { destinationMap } from "./destination-map.ts";

export const buildPayload = (
  notificationType: NotificationType,
  targetId: string | null,
  eventId: string,
  sentAt = new Date().toISOString(),
): NotificationPayload => {
  const destination = destinationMap[notificationType];
  return {
    notification_type: notificationType,
    target_kind: destination.targetKind,
    target_id: targetId,
    fallback_route: destination.fallbackRoute,
    event_id: eventId,
    sent_at: sentAt,
  };
};
