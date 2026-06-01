import { createClient } from "@supabase/supabase-js";
import { NotificationDispatcher } from "../_shared/dispatch.ts";
import { cleanupStaleDevices } from "./stale-device-cleanup.ts";

const supabase = createClient(
  Deno.env.get("SUPABASE_URL") ?? "",
  Deno.env.get("SUPABASE_SERVICE_ROLE_KEY") ?? "",
);

const DEFAULT_TIMEZONE = "America/Los_Angeles";
const WINDOW_OPEN_HOUR = 8;   // 08:00 local
const WINDOW_CLOSE_HOUR = 22; // < 22:00 local (8AM-10PM)
const SOON_LEAD_MINUTES = 60;
const SOON_GRACE_MINUTES = 15; // tolerate cron jitter

interface MeetupRow {
  id: string;
  group_id: string;
  title: string;
  scheduled_at: string;
  timezone: string | null;
  groups?: { id: string; name: string } | null;
}

interface RecipientRow {
  group_id: string;
  member_user_id: string;
}

interface ProfileRow {
  id: string;
  timezone: string | null;
}

interface LocalNow {
  year: number;
  month: number;
  day: number;
  hour: number;
  minute: number;
  isoDate: string; // YYYY-MM-DD in target tz
  offsetMinutesFromUtc: number;
}

const partsToLocal = (parts: Intl.DateTimeFormatPart[]): LocalNow => {
  const lookup: Record<string, string> = {};
  for (const part of parts) {
    if (part.type !== "literal") lookup[part.type] = part.value;
  }
  const year = Number(lookup.year);
  const month = Number(lookup.month);
  const day = Number(lookup.day);
  const hour = Number(lookup.hour);
  const minute = Number(lookup.minute);
  return {
    year,
    month,
    day,
    hour,
    minute,
    isoDate: `${lookup.year}-${lookup.month}-${lookup.day}`,
    offsetMinutesFromUtc: 0,
  };
};

const localizeInstant = (instant: Date, timezone: string): LocalNow => {
  try {
    const formatter = new Intl.DateTimeFormat("en-US", {
      timeZone: timezone,
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
      hour12: false,
    });
    return partsToLocal(formatter.formatToParts(instant));
  } catch {
    const formatter = new Intl.DateTimeFormat("en-US", {
      timeZone: DEFAULT_TIMEZONE,
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
      hour12: false,
    });
    return partsToLocal(formatter.formatToParts(instant));
  }
};

const resolveTimezone = (
  meetupTz: string | null,
  userTz: string | null,
): string => {
  if (meetupTz && meetupTz.trim().length > 0) return meetupTz.trim();
  if (userTz && userTz.trim().length > 0) return userTz.trim();
  return DEFAULT_TIMEZONE;
};

const fetchTodayCandidateMeetups = async (now: Date): Promise<MeetupRow[]> => {
  // Wide UTC window covering ±25h so we capture meetups whose local-today
  // overlaps the current instant in any timezone on earth.
  const lower = new Date(now.getTime() - 25 * 60 * 60 * 1000).toISOString();
  const upper = new Date(now.getTime() + 25 * 60 * 60 * 1000).toISOString();

  const { data, error } = await supabase
    .from("meetups")
    .select("id,group_id,title,scheduled_at,timezone,groups(id,name)")
    .gte("scheduled_at", lower)
    .lt("scheduled_at", upper);

  if (error) throw new Error(`fetch_meetups_failed:${error.message}`);
  return (data ?? []) as MeetupRow[];
};

const fetchRecipients = async (groupIds: string[]): Promise<Map<string, string[]>> => {
  const map = new Map<string, string[]>();
  if (groupIds.length === 0) return map;

  const { data, error } = await supabase
    .from("memberships")
    .select("group_id,member_user_id")
    .in("group_id", groupIds);

  if (error) throw new Error(`fetch_recipients_failed:${error.message}`);

  for (const row of (data ?? []) as RecipientRow[]) {
    const existing = map.get(row.group_id) ?? [];
    existing.push(row.member_user_id);
    map.set(row.group_id, existing);
  }
  return map;
};

const fetchUserTimezones = async (userIds: string[]): Promise<Map<string, string | null>> => {
  const map = new Map<string, string | null>();
  if (userIds.length === 0) return map;

  const { data, error } = await supabase
    .from("user_profiles")
    .select("id,timezone")
    .in("id", userIds);

  if (error) throw new Error(`fetch_profiles_failed:${error.message}`);

  for (const row of (data ?? []) as ProfileRow[]) {
    map.set(row.id, row.timezone);
  }
  return map;
};

const buildEventId = (
  meetupId: string,
  variant: "first" | "soon",
  localDate: string,
): string => `meetup:today_reminder:${variant}:${meetupId}:${localDate}`;

interface PlannedSend {
  variant: "first" | "soon";
  meetup: MeetupRow;
  recipientUserId: string;
  groupName: string;
  localDate: string;
  eventId: string;
}

Deno.serve(async () => {
  try {
    await cleanupStaleDevices();
  } catch (err) {
    console.log(JSON.stringify({
      level: "warn",
      message: "stale_device_cleanup_failed",
      error: err instanceof Error ? err.message : String(err),
    }));
  }

  const now = new Date();
  const meetups = await fetchTodayCandidateMeetups(now);
  if (meetups.length === 0) {
    return Response.json({ ok: true, queued: 0, reason: "no_meetups_in_window" });
  }

  const recipientsByGroup = await fetchRecipients(
    Array.from(new Set(meetups.map((m) => m.group_id))),
  );

  const userIds = Array.from(new Set(
    Array.from(recipientsByGroup.values()).flat(),
  ));
  const timezoneByUser = await fetchUserTimezones(userIds);

  const plan: PlannedSend[] = [];
  for (const meetup of meetups) {
    const recipients = recipientsByGroup.get(meetup.group_id) ?? [];
    if (recipients.length === 0) continue;

    const scheduledAt = new Date(meetup.scheduled_at);
    const groupName = meetup.groups?.name?.trim() || "your group";

    for (const userId of recipients) {
      const timezone = resolveTimezone(meetup.timezone, timezoneByUser.get(userId) ?? null);
      const localNow = localizeInstant(now, timezone);
      const localScheduled = localizeInstant(scheduledAt, timezone);

      // Only fire reminders when the meetup's local date matches the recipient's local "today".
      if (localScheduled.isoDate !== localNow.isoDate) continue;

      // First reminder: as early as possible within 08:00..22:00 local on the meetup day.
      const inWindow = localNow.hour >= WINDOW_OPEN_HOUR && localNow.hour < WINDOW_CLOSE_HOUR;
      if (inWindow) {
        plan.push({
          variant: "first",
          meetup,
          recipientUserId: userId,
          groupName,
          localDate: localNow.isoDate,
          eventId: buildEventId(meetup.id, "first", localNow.isoDate),
        });
      }

      // Soon reminder: within ~1h before scheduled_at (with grace for cron jitter).
      const minutesUntilStart = (scheduledAt.getTime() - now.getTime()) / 60000;
      if (
        minutesUntilStart <= SOON_LEAD_MINUTES &&
        minutesUntilStart >= -SOON_GRACE_MINUTES
      ) {
        plan.push({
          variant: "soon",
          meetup,
          recipientUserId: userId,
          groupName,
          localDate: localNow.isoDate,
          eventId: buildEventId(meetup.id, "soon", localNow.isoDate),
        });
      }
    }
  }

  if (plan.length === 0) {
    return Response.json({ ok: true, queued: 0, reason: "no_reminders_due" });
  }

  const dispatcher = new NotificationDispatcher();
  let sent = 0;
  let skipped = 0;
  let failed = 0;

  for (const item of plan) {
    const title = item.variant === "first"
      ? `You have a Meetup today for ${item.groupName}`
      : `Your ${item.groupName} meetup is starting soon!`;
    const body = item.variant === "first"
      ? `Heads up — your ${item.groupName} meetup is on today's calendar.`
      : `Your ${item.groupName} meetup starts within the hour.`;

    const result = await dispatcher.sendForRecipient({
      notificationType: "meetup.today_reminder",
      eventId: item.eventId,
      externalUserId: item.recipientUserId,
      title,
      body,
      targetId: item.meetup.group_id,
      sourceTable: "meetups",
      sourceRowId: item.meetup.id,
      occurredAt: new Date().toISOString(),
    });

    if (result.status === "sent") sent += 1;
    else if (result.status === "skipped") skipped += 1;
    else failed += 1;
  }

  return Response.json({
    ok: true,
    queued: plan.length,
    sent,
    skipped,
    failed,
  });
});
