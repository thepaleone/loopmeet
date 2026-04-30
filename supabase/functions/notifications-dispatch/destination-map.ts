import type { FallbackRoute, NotificationTargetKind, NotificationType } from "../_shared/notification-contract.ts";

export interface DestinationDefinition {
  targetKind: NotificationTargetKind;
  fallbackRoute: FallbackRoute;
}

export const destinationMap: Record<NotificationType, DestinationDefinition> = {
  "invitation.new": { targetKind: "invitations", fallbackRoute: "pending_invitations" },
  "meetup.created": { targetKind: "group", fallbackRoute: "home" },
  "meetup.updated": { targetKind: "group", fallbackRoute: "home" },
  "meetup.canceled": { targetKind: "group", fallbackRoute: "home" },
  "meetup.today_reminder": { targetKind: "home", fallbackRoute: "home" },
};
