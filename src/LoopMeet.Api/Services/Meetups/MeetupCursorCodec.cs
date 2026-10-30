using System.Globalization;
using System.Text;
using LoopMeet.Core.Meetups;
using Microsoft.AspNetCore.WebUtilities;

namespace LoopMeet.Api.Services.Meetups;

/// <summary>Opaque, URL-safe wire form of a <see cref="MeetupPageCursor"/>.</summary>
internal static class MeetupCursorCodec
{
    public static string Encode(MeetupPageCursor cursor)
    {
        var raw = $"{cursor.ScheduledAt.UtcTicks.ToString(CultureInfo.InvariantCulture)}:{cursor.Id:N}";
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(raw));
    }

    public static bool TryDecode(string? value, out MeetupPageCursor cursor)
    {
        cursor = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string raw;
        try
        {
            raw = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(value));
        }
        catch (FormatException)
        {
            return false;
        }

        var parts = raw.Split(':');
        if (parts.Length != 2
            || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks)
            || ticks < DateTimeOffset.MinValue.UtcTicks
            || ticks > DateTimeOffset.MaxValue.UtcTicks
            || !Guid.TryParseExact(parts[1], "N", out var id))
        {
            return false;
        }

        cursor = new MeetupPageCursor(new DateTimeOffset(ticks, TimeSpan.Zero), id);
        return true;
    }
}
