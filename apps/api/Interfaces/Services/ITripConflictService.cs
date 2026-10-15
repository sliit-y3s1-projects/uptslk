namespace api.Interfaces.Services;

// Checks whether a vehicle, driver and bay are free for a trip.
public interface ITripConflictService
{
    // Returns one message per clash. An empty list means the assignment is free.
    Task<IReadOnlyList<string>> FindConflicts(
        Guid vehicleId,
        Guid driverId,
        Guid bayId,
        DateTime scheduledTime,
        int durationMinutes,
        Guid? excludedTripId = null,
        CancellationToken cancellationToken = default);
}
