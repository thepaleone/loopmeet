// A meetup is past once the calendar date of "now" in its timezone is later than
// the date it was scheduled on (i.e. after local midnight). Mirrors
// src/LoopMeet.Core/Meetups/MeetupLifecycle.cs; both are tested against
// meetup-lifecycle.cases.json.

export const DEFAULT_MEETUP_TIMEZONE = "America/Los_Angeles";

const dateFormatters = new Map<string, Intl.DateTimeFormat>();

const buildFormatter = (timeZone: string): Intl.DateTimeFormat =>
  new Intl.DateTimeFormat("en-CA", {
    timeZone,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  });

const formatterFor = (
  timezone: string | null | undefined,
): Intl.DateTimeFormat => {
  const id = timezone && timezone.trim().length > 0
    ? timezone.trim()
    : DEFAULT_MEETUP_TIMEZONE;
  const cached = dateFormatters.get(id);
  if (cached) return cached;

  let formatter: Intl.DateTimeFormat;
  try {
    formatter = buildFormatter(id);
  } catch (err) {
    if (!(err instanceof RangeError)) throw err;
    formatter = buildFormatter(DEFAULT_MEETUP_TIMEZONE);
  }
  dateFormatters.set(id, formatter);
  return formatter;
};

export const isMeetupPast = (
  scheduledAt: string | Date,
  timezone: string | null | undefined,
  now: Date,
): boolean => {
  const formatter = formatterFor(timezone);
  const scheduled = scheduledAt instanceof Date
    ? scheduledAt
    : new Date(scheduledAt);
  // en-CA formats as YYYY-MM-DD, so lexical order is date order.
  return formatter.format(now) > formatter.format(scheduled);
};
