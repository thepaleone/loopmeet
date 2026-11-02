import type { NotificationType } from "../_shared/notification-contract.ts";
import { isMeetupPast } from "../_shared/meetup-lifecycle.ts";

export interface WebhookPayload {
  type: "INSERT" | "UPDATE" | "DELETE";
  table: string;
  record: Record<string, unknown>;
  old_record?: Record<string, unknown> | null;
}

export type SuppressionReason = "past_meetup";

const readMeetupPast = (row: Record<string, unknown>, now: Date): boolean => {
  const scheduledAt = row.scheduled_at;
  if (typeof scheduledAt !== "string") return false;
  const timezone = typeof row.timezone === "string" ? row.timezone : null;
  return isMeetupPast(scheduledAt, timezone, now);
};

// A change to a meetup whose day had already ended, and that is still past after
// the change, is record-keeping: the group is not notified. Moving a past meetup
// into the future falls through and notifies as a normal update.
export const resolveSuppression = (
  payload: WebhookPayload,
  now: Date,
): SuppressionReason | null => {
  if (payload.table !== "meetups" || payload.type !== "UPDATE" || !payload.old_record) {
    return null;
  }

  return readMeetupPast(payload.old_record, now) && readMeetupPast(payload.record, now)
    ? "past_meetup"
    : null;
};

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
