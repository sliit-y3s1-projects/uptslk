using api.Entities;

namespace api.Interfaces.Services;

// Who a trip was assigned to before a change, so the right people can be told what changed.
public sealed record TripAssignment(Guid DriverId, Guid VehicleId, Guid BayId, DateTime ScheduledTime)
{
    public static TripAssignment Of(Trip trip) => new(trip.DriverId, trip.VehicleId, trip.BayId, trip.ScheduledTime);
}

// Where a cancelled booking's money went.
public enum RefundDestination
{
    None,
    Wallet,
    Card
}

// In-app notifications for commuters and drivers. Saved with the change that caused them, pushed after the request succeeds.
public interface IMobileNotificationService
{
    Task TripAssignedAsync(Trip trip, CancellationToken cancellationToken = default);
    Task TripsAssignedAsync(IReadOnlyCollection<Trip> trips, CancellationToken cancellationToken = default);
    Task TripChangedAsync(Trip trip, TripAssignment before, CancellationToken cancellationToken = default);
    Task TripStatusChangedAsync(Trip trip, string? note, bool changedByDriver, CancellationToken cancellationToken = default);
    Task TripCancelledAsync(Trip trip, CancellationToken cancellationToken = default);
    Task BookingCancelledAsync(Booking booking, RefundDestination refund, CancellationToken cancellationToken = default);
    Task BookingConfirmedAsync(Booking booking, CancellationToken cancellationToken = default);
    Task RefundUpdatedAsync(PaymentRefund refund, CancellationToken cancellationToken = default);
    void IncidentUpdated(Incident incident);

    // Pushes the notifications created during this request over SignalR. Called by SendNotificationsFilter.
    Task SendPendingAsync(CancellationToken cancellationToken = default);
}
