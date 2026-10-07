namespace api.Services;

public static class ScheduleTimes
{
    /// <summary>
    /// The departure times of a timetable: first, first + headway, ... up to and including the last departure.
    /// Time is counted in plain minutes. <see cref="TimeOnly.AddMinutes(double)"/> wraps at midnight, so a timetable
    /// ending at 23:30 with a 30 minute headway would jump to 00:00, still look "before" 23:30 and never finish.
    /// </summary>
    public static IEnumerable<TimeOnly> Departures(TimeOnly first, TimeOnly last, int headwayMinutes)
    {
        if (headwayMinutes <= 0) yield break;
        var lastMinute = last.Hour * 60 + last.Minute;
        for (var minute = first.Hour * 60 + first.Minute; minute <= lastMinute; minute += headwayMinutes)
            yield return new TimeOnly(minute / 60, minute % 60);
    }
}
