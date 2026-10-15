using api.Interfaces.Repositories;
using api.Interfaces.Services;

namespace api.Services;

public sealed class TripConflictService(
    ITripRepository trips,
    IMaintenanceRecordRepository maintenanceRecords) : ITripConflictService
{
    // A bay is only needed while a bus boards and leaves, not for the whole journey.
    private const int BayOccupancyMinutes = 10;

    public async Task<IReadOnlyList<string>> FindConflicts(
        Guid vehicleId,
        Guid driverId,
        Guid bayId,
        DateTime scheduledTime,
        int durationMinutes,
        Guid? excludedTripId = null,
        CancellationToken cancellationToken = default)
    {
        var conflicts = new HashSet<string>();

        var openMaintenance = await maintenanceRecords.GetOpenForVehicleAsync(vehicleId, cancellationToken);
        if (openMaintenance.Any(record => VehicleMaintenanceRules.BlocksTrip(record, scheduledTime, durationMinutes)))
            conflicts.Add("Vehicle is unavailable because maintenance is scheduled for this service day or currently in progress.");

        var requestedEnd = scheduledTime.AddMinutes(durationMinutes);
        var requestedBayEnd = scheduledTime.AddMinutes(BayOccupancyMinutes);
        var candidates = await trips.GetActiveUsingAnyAsync(vehicleId, driverId, bayId, excludedTripId, cancellationToken);
        foreach (var trip in candidates)
        {
            var existingEnd = trip.ScheduledTime.AddMinutes(trip.Route.EstimatedDurationMin);
            var journeysOverlap = trip.ScheduledTime < requestedEnd && scheduledTime < existingEnd;
            var baysOverlap = trip.ScheduledTime < requestedBayEnd && scheduledTime < trip.ScheduledTime.AddMinutes(BayOccupancyMinutes);

            var tripLabel = string.IsNullOrWhiteSpace(trip.Route.RouteNumber) ? "another trip" : $"route {trip.Route.RouteNumber}";
            if (journeysOverlap && trip.VehicleId == vehicleId) conflicts.Add($"Vehicle is already assigned to {tripLabel} at this time.");
            if (journeysOverlap && trip.DriverId == driverId) conflicts.Add($"Driver is already assigned to {tripLabel} at this time.");
            if (baysOverlap && trip.BayId == bayId) conflicts.Add($"Bay is already assigned to {tripLabel} at this time.");
        }

        return conflicts.ToList();
    }
}
