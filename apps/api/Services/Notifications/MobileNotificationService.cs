using api.Data;
using api.DTOs;
using api.Enums;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services.Notifications;

public sealed record TripAssignment(Guid DriverId, Guid VehicleId, Guid BayId, DateTime ScheduledTime)
{
    public static TripAssignment Of(Trip trip) => new(trip.DriverId, trip.VehicleId, trip.BayId, trip.ScheduledTime);
}

public enum RefundDestination
{
    None,
    Wallet,
    Card
}

/// <summary>
/// Creates in-app notifications for commuters and drivers. Each notification is added to the shared DbContext so it is
/// saved together with the change that caused it, and is pushed over SignalR only after the request has succeeded
/// (see <see cref="SendNotificationsFilter"/>).
/// </summary>
public sealed class MobileNotificationService(
    AppDbContext db,
    INotificationPublisher publisher,
    ILogger<MobileNotificationService> logger)
{
    private const int MaxMessageLength = 1000;
    private static readonly TimeZoneInfo SriLankaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Colombo");
    private readonly List<UserNotification> pending = [];

    public async Task TripAssignedAsync(Trip trip, CancellationToken cancellationToken = default)
    {
        var info = await DescribeAsync(trip, cancellationToken);
        await NotifyDriverAsync(trip.DriverId, NotificationType.TripAssigned, "New trip assigned",
            $"You are assigned to route {info.RouteNumber} departing {info.Time} from bay {info.BayCode}.", trip.Id, cancellationToken);
    }

    public async Task TripsAssignedAsync(IReadOnlyCollection<Trip> trips, CancellationToken cancellationToken = default)
    {
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

        if (changes.Count > 0)
            await NotifyPassengersAsync(trip.Id, NotificationType.TripUpdated, $"Route {info.RouteNumber} trip changed",
                $"Your route {info.RouteNumber} trip at {previousTime} has changed: {string.Join("; ", changes)}. Your ticket is still valid.",
                cancellationToken);

        if (after.DriverId != before.DriverId)
        {
            await NotifyDriverAsync(before.DriverId, NotificationType.TripUnassigned, "Trip reassigned",
                $"You are no longer assigned to route {info.RouteNumber} at {previousTime}.", trip.Id, cancellationToken);
            await TripAssignedAsync(trip, cancellationToken);
            return;
        }

        if (after.VehicleId != before.VehicleId)
        {
            var plate = await db.Vehicles.AsNoTracking().Where(vehicle => vehicle.Id == after.VehicleId)
                .Select(vehicle => vehicle.PlateNumber).SingleAsync(cancellationToken);
            changes.Add($"your bus is now {plate}");
        }
        await NotifyDriverAsync(trip.DriverId, NotificationType.TripUpdated, "Trip changed",
            $"Your route {info.RouteNumber} trip at {previousTime} has changed: {string.Join("; ", changes)}.", trip.Id, cancellationToken);
    }

    public async Task TripStatusChangedAsync(Trip trip, string? note, bool changedByDriver, CancellationToken cancellationToken = default)
    {
        var info = await DescribeAsync(trip, cancellationToken);
        var status = trip.Status.ToString().ToLowerInvariant();
        var passengerMessage = trip.Status switch
        {
            TripStatus.Delayed => $"Your route {info.RouteNumber} trip at {info.Time} is delayed.{Reason(note)}",
            TripStatus.Boarding => $"Boarding has started for your route {info.RouteNumber} trip at bay {info.BayCode}.",
            _ => null
        };
        if (passengerMessage is not null)
            await NotifyPassengersAsync(trip.Id, NotificationType.TripStatusChanged, $"Route {info.RouteNumber}: {trip.Status}",
                passengerMessage, cancellationToken);

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
        var userId = await PassengerUserIdAsync(booking.PassengerId, cancellationToken);
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
        var userId = await PassengerUserIdAsync(booking.PassengerId, cancellationToken);
        if (userId is null) return;

        var info = await DescribeTripAsync(booking.TripId, cancellationToken);
        Add(userId.Value, NotificationType.BookingConfirmed, "Booking confirmed",
            $"Payment received. Seat {booking.SeatNumber} on route {info.RouteNumber} at {info.Time} from bay {info.BayCode} is confirmed.",
            booking.TripId, booking.Id);
    }

    public async Task RefundUpdatedAsync(PaymentRefund refund, CancellationToken cancellationToken = default)
    {
        if (refund.Status is not (PaymentRefundStatus.Succeeded or PaymentRefundStatus.Failed)) return;

        var booking = await db.Bookings.AsNoTracking()
            .Where(item => item.Id == refund.Payment.BookingId)
            .Select(item => new { item.Id, item.TripId, item.Passenger.UserId })
            .SingleOrDefaultAsync(cancellationToken);
        if (booking?.UserId is null) return;

        if (refund.Status == PaymentRefundStatus.Succeeded)
            Add(booking.UserId.Value, NotificationType.RefundCompleted, "Refund completed",
                $"Your refund of {Money(refund.Amount)} has been sent to your card.", booking.TripId, booking.Id);
        else
            Add(booking.UserId.Value, NotificationType.RefundFailed, "Refund failed",
                $"Your refund of {Money(refund.Amount)} could not be completed. Please contact support.", booking.TripId, booking.Id);
    }

    public void IncidentUpdated(Incident incident)
    {
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
        var userId = await db.Drivers.AsNoTracking().Where(driver => driver.Id == driverId)
            .Select(driver => driver.UserId).SingleOrDefaultAsync(cancellationToken);
        if (userId is not null) Add(userId.Value, type, title, message, tripId, null);
    }

    private async Task NotifyPassengersAsync(Guid tripId, NotificationType type, string title, string message, CancellationToken cancellationToken)
    {
        var bookings = await db.Bookings.AsNoTracking()
            .Where(booking => booking.TripId == tripId
                && (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed)
                && booking.Passenger.UserId != null)
            .Select(booking => new { booking.Id, UserId = booking.Passenger.UserId!.Value })
            .ToListAsync(cancellationToken);

        foreach (var booking in bookings.DistinctBy(booking => booking.UserId))
            Add(booking.UserId, type, title, message, tripId, booking.Id);
    }

    private Task<Guid?> PassengerUserIdAsync(Guid passengerId, CancellationToken cancellationToken) =>
        db.Passengers.AsNoTracking().Where(passenger => passenger.Id == passengerId)
            .Select(passenger => passenger.UserId).SingleOrDefaultAsync(cancellationToken);

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
        db.UserNotifications.Add(notification);
        pending.Add(notification);
    }

    private async Task<TripInfo> DescribeAsync(Trip trip, CancellationToken cancellationToken)
    {
        var routeNumber = await db.Routes.AsNoTracking().Where(route => route.Id == trip.RouteId)
            .Select(route => route.RouteNumber).SingleAsync(cancellationToken);
        var bayCode = await db.Bays.AsNoTracking().Where(bay => bay.Id == trip.BayId)
            .Select(bay => bay.Code).SingleAsync(cancellationToken);
        return new TripInfo(routeNumber, bayCode, FormatTime(trip.ScheduledTime));
    }

    private async Task<TripInfo> DescribeTripAsync(Guid tripId, CancellationToken cancellationToken)
    {
        var trip = await db.Trips.AsNoTracking().Where(item => item.Id == tripId)
            .Select(item => new { item.Route.RouteNumber, BayCode = item.Bay.Code, item.ScheduledTime })
            .SingleAsync(cancellationToken);
        return new TripInfo(trip.RouteNumber, trip.BayCode, FormatTime(trip.ScheduledTime));
    }

    private static string FormatTime(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), SriLankaTimeZone).ToString("HH:mm, d MMM");

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
