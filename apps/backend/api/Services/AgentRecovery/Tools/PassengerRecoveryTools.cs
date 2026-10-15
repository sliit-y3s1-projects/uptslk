using api.Interfaces.Repositories;

namespace api.Services.AgentRecovery.Tools;

public sealed class PassengerRecoveryTools(IBookingRepository bookings) : IPassengerRecoveryTools
{
    public async Task<AssessPassengerImpactOutput> AssessPassengerImpactAsync(
        AssessPassengerImpactInput input,
        CancellationToken cancellationToken)
    {
        RecoveryToolInputValidator.RequireId(input.TripId, nameof(input.TripId));

        // Passengers on pending and confirmed bookings are the ones a change affects.
        var affectedPassengers = await bookings.GetOccupiedCapacityAsync(input.TripId, cancellationToken);

        return new AssessPassengerImpactOutput(
            affectedPassengers,
            affectedPassengers > 0,
            false);
    }
}
