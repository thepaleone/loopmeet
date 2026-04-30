import type { FallbackRoute, NotificationTargetKind, NotificationType } from "../_shared/notification-contract.ts";
import { notificationMappingRegistry } from "../_shared/notification-mapping-registry.ts";

export interface DestinationDefinition {
  targetKind: NotificationTargetKind;
  fallbackRoute: FallbackRoute;
}

export const destinationMap: Record<NotificationType, DestinationDefinition> = {
  "invitation.new": notificationMappingRegistry["invitation.new"],
  "meetup.created": notificationMappingRegistry["meetup.created"],
  "meetup.updated": notificationMappingRegistry["meetup.updated"],
  "meetup.canceled": notificationMappingRegistry["meetup.canceled"],
  "meetup.today_reminder": notificationMappingRegistry["meetup.today_reminder"],
};
