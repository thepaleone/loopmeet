import type { NotificationType } from "./notification-contract.ts";

/**
 * OneSignal Android channel ID per notification type. Channel IDs are created
 * in the OneSignal dashboard (Settings -> Messaging -> Android Notification
 * Categories) and are forwarded to the OneSignal API via `android_channel_id`.
 * Recipients can independently opt out of each channel at the Android system
 * level, so prefer one channel per user-meaningful category.
 */
export const NOTIFICATION_ANDROID_CHANNELS: Record<NotificationType, string> = {
  // "New or Changed Meetup" — meetup CRUD and group invitations share this
  // channel so a single Android Settings toggle covers all "something about
  // your groups changed" pings.
  "invitation.new": "bfa2d7cf-0abc-46b3-b3fc-1bd22b482c80",
  "meetup.created": "bfa2d7cf-0abc-46b3-b3fc-1bd22b482c80",
  "meetup.updated": "bfa2d7cf-0abc-46b3-b3fc-1bd22b482c80",
  "meetup.canceled": "bfa2d7cf-0abc-46b3-b3fc-1bd22b482c80",

  // "Meetup Reminders" — both the morning "today" reminder and the
  // 1-hour-out reminder route here.
  "meetup.today_reminder": "76cd1d30-aa25-4b99-9384-e807a9848aa6",
};

export const resolveAndroidChannelId = (notificationType: NotificationType): string | undefined =>
  NOTIFICATION_ANDROID_CHANNELS[notificationType];
