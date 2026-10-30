using System.Collections.Concurrent;

namespace LoopMeet.Core.Meetups;

/// <summary>
/// A meetup is past once the calendar date of "now" in its timezone is later than
/// the date it was scheduled on (i.e. after local midnight). Mirrored by
/// supabase/functions/_shared/meetup-lifecycle.ts; both are tested against
/// supabase/functions/_shared/meetup-lifecycle.cases.json.
/// </summary>
public static class MeetupLifecycle
{
    public const string DefaultTimezone = "America/Los_Angeles";

    /// <summary>
    /// A local day lasts at most 25 hours, so anything scheduled more than this long
    /// ago is past in every timezone; only newer rows need per-row classification.
    /// </summary>
    public static readonly TimeSpan ClassificationBand = TimeSpan.FromHours(26);

    private static readonly ConcurrentDictionary<string, TimeZoneInfo> Zones = new(StringComparer.Ordinal);

    public static bool IsPast(DateTimeOffset scheduledAt, string? timezone, DateTimeOffset now)
    {
        var zone = ResolveZone(timezone);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
        var scheduledDay = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(scheduledAt, zone).DateTime);
        return today > scheduledDay;
    }

    private static TimeZoneInfo ResolveZone(string? timezone)
    {
        var id = string.IsNullOrWhiteSpace(timezone) ? DefaultTimezone : timezone.Trim();
        return Zones.GetOrAdd(id, static key =>
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(key);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById(DefaultTimezone);
            }
        });
    }
}
