export const notificationTypes = [
  "invitation.new",
  "meetup.created",
  "meetup.updated",
  "meetup.canceled",
  "meetup.today_reminder",
] as const;

export type NotificationType = (typeof notificationTypes)[number];
export type NotificationTargetKind = "invitations" | "group" | "home";
export type FallbackRoute = "home" | "pending_invitations";

export interface NotificationPayload {
  notification_type: NotificationType;
  target_kind: NotificationTargetKind;
  target_id: string | null;
  fallback_route: FallbackRoute;
  event_id: string;
  sent_at: string;
}

export const isNotificationType = (value: string): value is NotificationType => {
  return (notificationTypes as readonly string[]).includes(value);
};
