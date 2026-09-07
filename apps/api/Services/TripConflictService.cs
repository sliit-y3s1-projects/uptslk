using api.Data;
using api.Enums;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public class TripConflictService(AppDbContext db)
{
    private static readonly TripStatus[] ActiveStatuses = [TripStatus.Scheduled, TripStatus.Ready, TripStatus.Boarding, TripStatus.Delayed, TripStatus.Dispatched];

    public async Task<IReadOnlyList<string>> FindConflicts(Guid vehicleId, Guid driverId, Guid bayId, DateTime scheduledTime, int durationMinutes, Guid? excludedTripId = null)
    {
        var candidates = await db.Trips.AsNoTracking()
            .Include(trip => trip.Route)
            .Where(trip => ActiveStatuses.Contains(trip.Status)
                && (!excludedTripId.HasValue || trip.Id != excludedTripId.Value)
                && (trip.VehicleId == vehicleId || trip.DriverId == driverId || trip.BayId == bayId))
            .ToListAsync();

        var requestedEnd = scheduledTime.AddMinutes(durationMinutes);
        var conflicts = new HashSet<string>();

        foreach (var trip in candidates)
        {
            var existingEnd = trip.ScheduledTime.AddMinutes(trip.Route.EstimatedDurationMin);
            if (trip.ScheduledTime >= requestedEnd || scheduledTime >= existingEnd) continue;

            if (trip.VehicleId == vehicleId) conflicts.Add($"Vehicle is already assigned to trip {trip.Id} at this time.");
            if (trip.DriverId == driverId) conflicts.Add($"Driver is already assigned to trip {trip.Id} at this time.");
            if (trip.BayId == bayId) conflicts.Add($"Bay is already assigned to trip {trip.Id} at this time.");
        }

        return conflicts.ToList();
    }
}
