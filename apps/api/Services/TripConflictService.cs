using api.Data;
using api.Enums;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public class TripConflictService(AppDbContext db)
{
    private static readonly TripStatus[] ActiveStatuses = [TripStatus.Scheduled, TripStatus.Ready, TripStatus.Boarding, TripStatus.Delayed, TripStatus.Dispatched];
    // A bay is needed while a bus boards and leaves, not for the whole journey.
    private const int BayOccupancyMinutes = 10;

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
            var journeysOverlap = trip.ScheduledTime < requestedEnd && scheduledTime < existingEnd;
            var existingBayEnd = trip.ScheduledTime.AddMinutes(BayOccupancyMinutes);
            var requestedBayEnd = scheduledTime.AddMinutes(BayOccupancyMinutes);
            var baysOverlap = trip.ScheduledTime < requestedBayEnd && scheduledTime < existingBayEnd;

            var tripLabel = string.IsNullOrWhiteSpace(trip.Route.RouteNumber) ? "another trip" : $"route {trip.Route.RouteNumber}";
            if (journeysOverlap && trip.VehicleId == vehicleId) conflicts.Add($"Vehicle is already assigned to {tripLabel} at this time.");
            if (journeysOverlap && trip.DriverId == driverId) conflicts.Add($"Driver is already assigned to {tripLabel} at this time.");
            if (baysOverlap && trip.BayId == bayId) conflicts.Add($"Bay is already assigned to {tripLabel} at this time.");
        }

        return conflicts.ToList();
    }
}
