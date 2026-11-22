namespace PredictiveMaintenance.Models;

public static class IstClock
{
    private static readonly TimeZoneInfo IstZone = ResolveIstZone();

    public static DateTime Now => TimeZoneInfo.ConvertTime(DateTime.UtcNow, IstZone);

    private static TimeZoneInfo ResolveIstZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
        catch (TimeZoneNotFoundException) { }
        catch (InvalidTimeZoneException)  { }

        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
        catch (TimeZoneNotFoundException) { }
        catch (InvalidTimeZoneException)  { }

        return TimeZoneInfo.CreateCustomTimeZone(
            id: "IST-Fallback",
            baseUtcOffset: TimeSpan.FromMinutes(330),
            displayName: "(UTC+05:30) India Standard Time",
            standardDisplayName: "IST");
    }
}
