using api.Common.Helpers;
using api.Entities;
using api.Entities.Enums;

namespace api.Services;

// Decides when a maintenance record stops a vehicle from running a trip. It has no database access, so it is easy to test.
public static class VehicleMaintenanceRules
{
    public static bool BlocksTrip(MaintenanceRecord record, DateTime departureUtc, int durationMinutes)
    {
        // Work in progress blocks the vehicle until it is finished. Completed or cancelled work never blocks it.
        if (record.Status == MaintenanceStatus.InProgress) return true;
        if (record.Status != MaintenanceStatus.Scheduled) return false;

        // Scheduled work takes the vehicle out for the whole Sri Lanka service day it falls on.
        var scheduledUtc = record.ScheduledFor.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(record.ScheduledFor, DateTimeKind.Utc)
            : record.ScheduledFor.ToUniversalTime();
        var serviceDate = DateOnly.FromDateTime(SriLankaTime.FromUtc(scheduledUtc));
        var start = SriLankaTime.DayStartUtc(serviceDate);
        var end = SriLankaTime.DayStartUtc(serviceDate.AddDays(1));
        return departureUtc < end && departureUtc.AddMinutes(Math.Max(durationMinutes, 1)) > start;
    }
}
