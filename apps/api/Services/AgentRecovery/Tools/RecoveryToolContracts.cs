namespace api.Services.AgentRecovery.Tools;

public sealed record FindDepartureBayInput(
    Guid DepartureCentreId,
    Guid ExcludedBayId);

public sealed record FindDepartureBayOutput(
    Guid? CandidateBayId,
    string? CandidateBayCode,
    int CandidatesChecked);

public sealed record FindReplacementVehicleInput(
    Guid CentreId,
    Guid ExcludedVehicleId,
    int RequiredCapacity);

public sealed record FindReplacementVehicleOutput(
    Guid? CandidateVehicleId,
    string? PlateNumber,
    int? Capacity,
    bool MeetsRequiredCapacity,
    int CandidatesChecked);

public sealed record FindConflictFreeDriverInput(
    Guid CentreId,
    Guid ExcludedDriverId,
    Guid VehicleId,
    Guid BayId,
    DateTime ScheduledTime,
    int DurationMinutes,
    Guid ExcludedTripId);

public sealed record FindConflictFreeDriverOutput(
    Guid? CandidateDriverId,
    string? DriverName,
    int CandidatesChecked);

public sealed record AssessPassengerImpactInput(Guid TripId);

public sealed record AssessPassengerImpactOutput(
    int AffectedPassengers,
    bool NotificationRecommended,
    bool RefundRecommended);

public interface INetworkRecoveryTools
{
    Task<FindDepartureBayOutput> FindDepartureBayAsync(
        FindDepartureBayInput input,
        CancellationToken cancellationToken);
}

public interface IFleetRecoveryTools
{
    Task<FindReplacementVehicleOutput> FindReplacementVehicleAsync(
        FindReplacementVehicleInput input,
        CancellationToken cancellationToken);
}

public interface IDispatchRecoveryTools
{
    Task<FindConflictFreeDriverOutput> FindConflictFreeDriverAsync(
        FindConflictFreeDriverInput input,
        CancellationToken cancellationToken);
}

public interface IPassengerRecoveryTools
{
    Task<AssessPassengerImpactOutput> AssessPassengerImpactAsync(
        AssessPassengerImpactInput input,
        CancellationToken cancellationToken);
}
