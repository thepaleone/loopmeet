import type { FallbackRoute, NotificationTargetKind, NotificationType } from "./notification-contract.ts";

export interface NotificationMappingEntry {
  targetKind: NotificationTargetKind;
  fallbackRoute: FallbackRoute;
  routeId: string;
}

export const NOTIFICATION_MAPPING_VERSION = "v1";

export const notificationMappingRegistry: Record<NotificationType, NotificationMappingEntry> = {
  "invitation.new": {
    targetKind: "invitations",
    fallbackRoute: "pending_invitations",
    routeId: "PendingInvitations",
  },
  "meetup.created": {
    targetKind: "group",
    fallbackRoute: "home",
    routeId: "GroupDetail",
  },
  "meetup.updated": {
    targetKind: "group",
    fallbackRoute: "home",
    routeId: "GroupDetail",
  },
  "meetup.canceled": {
    targetKind: "group",
    fallbackRoute: "home",
    routeId: "GroupDetail",
  },
  "meetup.today_reminder": {
    targetKind: "home",
    fallbackRoute: "home",
    routeId: "Home",
  },
};
