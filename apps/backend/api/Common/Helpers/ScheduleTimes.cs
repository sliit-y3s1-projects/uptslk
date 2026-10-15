namespace api.Common.Helpers;

public static class ScheduleTimes
{
    // Departure times from first to last, every headway minutes. Counted in minutes because TimeOnly wraps at midnight.
    public static IEnumerable<TimeOnly> Departures(TimeOnly first, TimeOnly last, int headwayMinutes)
    {
        if (headwayMinutes <= 0) yield break;
        var lastMinute = last.Hour * 60 + last.Minute;
        for (var minute = first.Hour * 60 + first.Minute; minute <= lastMinute; minute += headwayMinutes)
            yield return new TimeOnly(minute / 60, minute % 60);
    }
}
