using api.Common.Helpers;
using api.Data;
using api.Entities.Enums;
using api.Entities;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public static class VehicleMaintenanceRules
{
    public static bool BlocksTrip(MaintenanceRecord record, DateTime departureUtc, int durationMinutes)
    {
        if (record.Status == MaintenanceStatus.InProgress) return true;
        if (record.Status != MaintenanceStatus.Scheduled) return false;

        var scheduledUtc = record.ScheduledFor.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(record.ScheduledFor, DateTimeKind.Utc)
            : record.ScheduledFor.ToUniversalTime();
        var serviceDate = DateOnly.FromDateTime(SriLankaTime.FromUtc(scheduledUtc));
        var start = SriLankaTime.DayStartUtc(serviceDate);
        var end = SriLankaTime.DayStartUtc(serviceDate.AddDays(1));
        return departureUtc < end && departureUtc.AddMinutes(Math.Max(durationMinutes, 1)) > start;
    }

    public static async Task<bool> IsUnavailableAsync(
        AppDbContext db, Guid vehicleId, DateTime departureUtc, int durationMinutes,
        CancellationToken cancellationToken = default)
    {
        var records = await db.MaintenanceRecords.AsNoTracking()
            .Where(record => record.VehicleId == vehicleId
                && (record.Status == MaintenanceStatus.Scheduled || record.Status == MaintenanceStatus.InProgress))
            .ToListAsync(cancellationToken);
        return records.Any(record => BlocksTrip(record, departureUtc, durationMinutes));
    }

    public static async Task<IReadOnlyList<Guid>> FindConflictingTripIdsAsync(
        AppDbContext db, MaintenanceRecord record, CancellationToken cancellationToken = default)
    {
        if (record.Status is MaintenanceStatus.Completed or MaintenanceStatus.Cancelled) return [];
        var trips = await db.Trips.AsNoTracking()
            .Include(trip => trip.Route)
            .Include(trip => trip.RouteDirection)
            .Where(trip => trip.VehicleId == record.VehicleId
                && trip.Status != TripStatus.Completed && trip.Status != TripStatus.Cancelled)
            .ToListAsync(cancellationToken);
        return trips
            .Where(trip => BlocksTrip(record, trip.ScheduledTime,
                trip.RouteDirection?.EstimatedDurationMin ?? trip.Route.EstimatedDurationMin))
            .Select(trip => trip.Id)
            .ToList();
    }
}
