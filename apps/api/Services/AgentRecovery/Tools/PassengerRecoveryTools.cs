using api.Data;
using api.Enums;
using Microsoft.EntityFrameworkCore;

namespace api.Services.AgentRecovery.Tools;

public sealed class PassengerRecoveryTools(AppDbContext db) : IPassengerRecoveryTools
{
    public async Task<AssessPassengerImpactOutput> AssessPassengerImpactAsync(
        AssessPassengerImpactInput input,
        CancellationToken cancellationToken)
    {
        RecoveryToolInputValidator.RequireId(input.TripId, nameof(input.TripId));

        var affectedPassengers = await db.Bookings.AsNoTracking()
            .Where(booking => booking.TripId == input.TripId
                && (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed))
            .SumAsync(booking => (int?)booking.PassengerCount, cancellationToken) ?? 0;

        return new AssessPassengerImpactOutput(
            affectedPassengers,
            affectedPassengers > 0,
            false);
    }
}
