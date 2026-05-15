namespace LoopMeet.App.Services;

public static class TimezoneHelper
{
    private const string DefaultIanaTimezone = "America/Los_Angeles";

    public static string GetCurrentDeviceTimezoneId()
    {
        try
        {
            var id = TimeZoneInfo.Local.Id;
            // iOS/Android .NET surfaces IANA identifiers (e.g. "America/Los_Angeles").
            // Windows surfaces Windows IDs (e.g. "Pacific Standard Time"); convert.
            if (id.Contains('/', StringComparison.Ordinal))
            {
                return id;
            }

            if (TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out var iana) && !string.IsNullOrWhiteSpace(iana))
            {
                return iana;
            }

            return DefaultIanaTimezone;
        }
        catch
        {
            return DefaultIanaTimezone;
        }
    }
}
