namespace api.Common.Helpers;

// The database stores UTC, but service days and timetables follow Sri Lanka time.
public static class SriLankaTime
{
    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");

    public static DateOnly Today() => DateOnly.FromDateTime(FromUtc(DateTime.UtcNow));

    public static DateTime FromUtc(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    public static DateTime ToUtc(DateTime localTime) => TimeZoneInfo.ConvertTimeToUtc(localTime, Zone);

    // The UTC time at which the given Sri Lanka date begins.
    public static DateTime DayStartUtc(DateOnly date) =>
        ToUtc(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified));
}
