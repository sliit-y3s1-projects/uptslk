using api.Common.Helpers;
using api.DTO.Notifications;
using api.Entities;
using api.Entities.Enums;
using api.Interfaces.Repositories;
using api.Interfaces.Services;

namespace api.Services;

public sealed class MobileNotificationService(
    IUserNotificationRepository userNotifications,
    IDriverRepository drivers,
    IPassengerRepository passengers,
    IBookingRepository bookings,
    ITripRepository trips,
    IRouteRepository routes,
    IBayRepository bays,
    IVehicleRepository vehicles,
    INotificationPublisher publisher,
    ILogger<MobileNotificationService> logger) : IMobileNotificationService
{
    private const int MaxMessageLength = 1000;

    // Notifications created during this request. They are pushed once the request has succeeded.
    private readonly List<UserNotification> pending = [];

    public async Task TripAssignedAsync(Trip trip, CancellationToken cancellationToken = default)
    {
        var info = await DescribeAsync(trip, cancellationToken);
        await NotifyDriverAsync(trip.DriverId, NotificationType.TripAssigned, "New trip assigned",
            $"You are assigned to route {info.RouteNumber} departing {info.Time} from bay {info.BayCode}.", trip.Id, cancellationToken);
    }

    public async Task TripsAssignedAsync(IReadOnlyCollection<Trip> trips, CancellationToken cancellationToken = default)
    {
        // One message per driver, so a driver given many generated trips is not flooded.
        foreach (var driverTrips in trips.GroupBy(trip => trip.DriverId))
        {
            var ordered = driverTrips.OrderBy(trip => trip.ScheduledTime).ToList();
            if (ordered.Count == 1)
            {
                await TripAssignedAsync(ordered[0], cancellationToken);
                continue;
            }

            var first = await DescribeAsync(ordered[0], cancellationToken);
            await NotifyDriverAsync(driverTrips.Key, NotificationType.TripAssigned, $"{ordered.Count} new trips assigned",
                $"You are assigned to {ordered.Count} route {first.RouteNumber} trips. The first departs {first.Time} from bay {first.BayCode}.",
                null, cancellationToken);
        }
    }

    public async Task TripChangedAsync(Trip trip, TripAssignment before, CancellationToken cancellationToken = default)
    {
        var after = TripAssignment.Of(trip);
        if (after == before) return;

        var info = await DescribeAsync(trip, cancellationToken);
        var previousTime = FormatTime(before.ScheduledTime);
        var changes = new List<string>();
        if (after.ScheduledTime != before.ScheduledTime) changes.Add($"it now departs {info.Time}");
        if (after.BayId != before.BayId) changes.Add($"the departure bay is now {info.BayCode}");

        // Passengers only care about the time and the bay.
        if (changes.Count > 0)
            await NotifyPassengersAsync(trip.Id, NotificationType.TripUpdated, $"Route {info.RouteNumber} trip changed",
                $"Your route {info.RouteNumber} trip at {previousTime} has changed: {string.Join("; ", changes)}. Your ticket is still valid.",
                cancellationToken);

        // A new driver gets an assignment message and the old driver is told they are off the trip.
        if (after.DriverId != before.DriverId)
        {
            await NotifyDriverAsync(before.DriverId, NotificationType.TripUnassigned, "Trip reassigned",
                $"You are no longer assigned to route {info.RouteNumber} at {previousTime}.", trip.Id, cancellationToken);
            await TripAssignedAsync(trip, cancellationToken);
            return;
        }

        if (after.VehicleId != before.VehicleId)
        {
            var vehicle = await vehicles.GetReadOnlyAsync(after.VehicleId, cancellationToken)
                ?? throw new InvalidOperationException($"Vehicle {after.VehicleId} does not exist.");
            changes.Add($"your bus is now {vehicle.PlateNumber}");
        }
        await NotifyDriverAsync(trip.DriverId, NotificationType.TripUpdated, "Trip changed",
            $"Your route {info.RouteNumber} trip at {previousTime} has changed: {string.Join("; ", changes)}.", trip.Id, cancellationToken);
    }

    public async Task TripStatusChangedAsync(Trip trip, string? note, bool changedByDriver, CancellationToken cancellationToken = default)
    {
        var info = await DescribeAsync(trip, cancellationToken);
        var status = trip.Status.ToString().ToLowerInvariant();

        // Passengers only hear about delays and boarding.
        var passengerMessage = trip.Status switch
        {
            TripStatus.Delayed => $"Your route {info.RouteNumber} trip at {info.Time} is delayed.{Reason(note)}",
            TripStatus.Boarding => $"Boarding has started for your route {info.RouteNumber} trip at bay {info.BayCode}.",
            _ => null
        };
        if (passengerMessage is not null)
            await NotifyPassengersAsync(trip.Id, NotificationType.TripStatusChanged, $"Route {info.RouteNumber}: {trip.Status}",
                passengerMessage, cancellationToken);

        // The driver already knows about changes they made themselves.
        if (!changedByDriver)
            await NotifyDriverAsync(trip.DriverId, NotificationType.TripStatusChanged, $"Trip {status}",
                $"Dispatch marked your route {info.RouteNumber} trip at {info.Time} as {status}.{Reason(note)}",
                trip.Id, cancellationToken);
    }

    public async Task TripCancelledAsync(Trip trip, CancellationToken cancellationToken = default)
    {
        var info = await DescribeAsync(trip, cancellationToken);
        await NotifyDriverAsync(trip.DriverId, NotificationType.TripCancelled, "Trip cancelled",
            $"Your route {info.RouteNumber} trip at {info.Time} was cancelled.{Reason(trip.CancellationReason)}", trip.Id, cancellationToken);
    }

    public async Task BookingCancelledAsync(Booking booking, RefundDestination refund, CancellationToken cancellationToken = default)
    {
        var userId = await passengers.GetUserIdAsync(booking.PassengerId, cancellationToken);
        if (userId is null) return;

        var info = await DescribeTripAsync(booking.TripId, cancellationToken);
        var refundText = refund switch
        {
            RefundDestination.Wallet => $" {Money(booking.RefundAmount)} was refunded to your wallet.",
            RefundDestination.Card => $" A refund of {Money(booking.RefundAmount)} to your card has been requested.",
            _ => ""
        };
        Add(userId.Value, NotificationType.BookingCancelled, "Booking cancelled",
            $"Your seat {booking.SeatNumber} on route {info.RouteNumber} at {info.Time} was cancelled.{Reason(booking.CancellationReason)}{refundText}",
            booking.TripId, booking.Id);
    }

    public async Task BookingConfirmedAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        var userId = await passengers.GetUserIdAsync(booking.PassengerId, cancellationToken);
        if (userId is null) return;

        var info = await DescribeTripAsync(booking.TripId, cancellationToken);
        Add(userId.Value, NotificationType.BookingConfirmed, "Booking confirmed",
            $"Payment received. Seat {booking.SeatNumber} on route {info.RouteNumber} at {info.Time} from bay {info.BayCode} is confirmed.",
            booking.TripId, booking.Id);
    }

    public async Task RefundUpdatedAsync(PaymentRefund refund, CancellationToken cancellationToken = default)
    {
        // Only a finished refund is worth telling the passenger about.
        if (refund.Status is not (PaymentRefundStatus.Succeeded or PaymentRefundStatus.Failed)) return;

        var booking = await bookings.GetWithPassengerAsync(refund.Payment.BookingId, cancellationToken);
        if (booking?.Passenger.UserId is not { } userId) return;

        if (refund.Status == PaymentRefundStatus.Succeeded)
            Add(userId, NotificationType.RefundCompleted, "Refund completed",
                $"Your refund of {Money(refund.Amount)} has been sent to your card.", booking.TripId, booking.Id);
        else
            Add(userId, NotificationType.RefundFailed, "Refund failed",
                $"Your refund of {Money(refund.Amount)} could not be completed. Please contact support.", booking.TripId, booking.Id);
    }

    public void IncidentUpdated(Incident incident)
    {
        // Only incidents reported from the driver app have someone to notify.
        if (incident.ReportedById is not { } userId) return;
        Add(userId, NotificationType.IncidentUpdated, "Incident report updated",
            $"Your report \"{incident.Title}\" is now {IncidentStatusLabel(incident.Status)}.", incident.TripId, null);
    }

    public async Task SendPendingAsync(CancellationToken cancellationToken = default)
    {
        var notifications = pending.ToArray();
        pending.Clear();
        foreach (var notification in notifications)
        {
            // The notification is already saved, so a failed push only means the app shows it on its next refresh.
            try
            {
                await publisher.PublishAsync(notification.UserId, NotificationResponse.From(notification), cancellationToken);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Could not push notification {NotificationId} to user {UserId}.", notification.Id, notification.UserId);
            }
        }
    }

    private async Task NotifyDriverAsync(Guid driverId, NotificationType type, string title, string message, Guid? tripId, CancellationToken cancellationToken)
    {
        var userId = await drivers.GetUserIdAsync(driverId, cancellationToken);
        if (userId is not null) Add(userId.Value, type, title, message, tripId, null);
    }

    private async Task NotifyPassengersAsync(Guid tripId, NotificationType type, string title, string message, CancellationToken cancellationToken)
    {
        var activeBookings = await bookings.GetActiveForTripWithPassengerAsync(tripId, cancellationToken);

        // A passenger with several bookings on the trip gets one message.
        foreach (var booking in activeBookings.Where(booking => booking.Passenger.UserId.HasValue).DistinctBy(booking => booking.Passenger.UserId))
            Add(booking.Passenger.UserId!.Value, type, title, message, tripId, booking.Id);
    }

    private void Add(Guid userId, NotificationType type, string title, string message, Guid? tripId, Guid? bookingId)
    {
        var notification = new UserNotification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message.Length > MaxMessageLength ? message[..MaxMessageLength] : message,
            TripId = tripId,
            BookingId = bookingId
        };
        // Saved by the caller's SaveChanges, together with the change it describes.
        userNotifications.Add(notification);
        pending.Add(notification);
    }

    private async Task<TripInfo> DescribeAsync(Trip trip, CancellationToken cancellationToken)
    {
        // The trip may have unsaved changes, so read the route and bay by id instead of reloading the trip.
        var route = await routes.GetReadOnlyAsync(trip.RouteId, cancellationToken)
            ?? throw new InvalidOperationException($"Route {trip.RouteId} does not exist.");
        var bay = await bays.GetReadOnlyAsync(trip.BayId, cancellationToken)
            ?? throw new InvalidOperationException($"Bay {trip.BayId} does not exist.");
        return new TripInfo(route.RouteNumber, bay.Code, FormatTime(trip.ScheduledTime));
    }

    private async Task<TripInfo> DescribeTripAsync(Guid tripId, CancellationToken cancellationToken)
    {
        var trip = await trips.GetWithRouteAndBayAsync(tripId, cancellationToken)
            ?? throw new InvalidOperationException($"Trip {tripId} does not exist.");
        return new TripInfo(trip.Route.RouteNumber, trip.Bay.Code, FormatTime(trip.ScheduledTime));
    }

    private static string FormatTime(DateTime utc) => SriLankaTime.FromUtc(utc).ToString("HH:mm, d MMM");

    private static string Money(decimal amount) => $"LKR {amount:N2}";

    private static string Reason(string? text) =>
        string.IsNullOrWhiteSpace(text) ? "" : $" Reason: {text.Trim().TrimEnd('.')}.";

    private static string IncidentStatusLabel(IncidentStatus status) => status switch
    {
        IncidentStatus.InProgress => "in progress",
        _ => status.ToString().ToLowerInvariant()
    };

    private sealed record TripInfo(string RouteNumber, string BayCode, string Time);
}
