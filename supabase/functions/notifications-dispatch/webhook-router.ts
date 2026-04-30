import type { NotificationType } from "../_shared/notification-contract.ts";

export interface WebhookPayload {
  type: "INSERT" | "UPDATE" | "DELETE";
  table: string;
  record: Record<string, unknown>;
  old_record?: Record<string, unknown> | null;
}

export const resolveNotificationType = (payload: WebhookPayload): NotificationType | null => {
  if (payload.table === "invitations" && payload.type === "INSERT") {
    return "invitation.new";
  }

  if (payload.table === "meetups" && payload.type === "INSERT") {
    return "meetup.created";
  }

  if (payload.table === "meetups" && payload.type === "UPDATE") {
    const nextStatus = payload.record.status;
    const previousStatus = payload.old_record?.status;
    if (nextStatus === "canceled" && previousStatus !== "canceled") {
      return "meetup.canceled";
    }
    return "meetup.updated";
  }

  return null;
};
