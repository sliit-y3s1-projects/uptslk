using api.Entities;
using api.Entities.Enums;
using api.Services;

namespace api.DTO.Bookings;

// A commuter's ticket. The QR code is only shown while the ticket can be used to board.
public sealed record TicketResponse(
    Guid Id,
    BookingStatus Status,
    int PassengerCount,
    decimal Fare,
    string? QrCode,
    bool CanBoard,
    string TicketGroup,
    TripStatus TripStatus,
    DateTime ActiveUntil,
    string PassengerName,
    string Bay,
    string Origin,
    string Destination,
    DateTime CreatedAt,
    DateTime TripTime,
    string Route,
    string RouteName)
{
    public static TicketResponse From(Booking booking, DateTime now)
    {
        var canBoard = BookingEligibility.CanBoard(booking, now);
        var trip = booking.Trip;
        return new TicketResponse(
            booking.Id,
            booking.Status,
            booking.PassengerCount,
            booking.Fare,
            canBoard ? booking.QrCode : null,
            canBoard,
            BookingEligibility.TicketGroup(booking, now),
            trip.Status,
            BookingEligibility.ActiveUntil(trip),
            booking.Passenger.FullName,
            trip.Bay.Code,
            trip.RouteDirection?.StartCentre.Name ?? trip.Route.Origin,
            trip.RouteDirection?.EndCentre.Name ?? trip.Route.Destination,
            booking.CreatedAt,
            trip.ScheduledTime,
            trip.Route.RouteNumber,
            trip.Route.Name);
    }
}

public sealed record BookingListItemResponse(
    Guid Id,
    Guid TripId,
    string Route,
    DateTime TripTime,
    Guid PassengerId,
    string Passenger,
    int PassengerCount,
    decimal Fare,
    BookingStatus Status,
    DateTime CreatedAt)
{
    public static BookingListItemResponse From(Booking booking) => new(
        booking.Id,
        booking.TripId,
        booking.Trip.Route.RouteNumber,
        booking.Trip.ScheduledTime,
        booking.PassengerId,
        booking.Passenger.FullName,
        booking.PassengerCount,
        booking.Fare,
        booking.Status,
        booking.CreatedAt);
}

public sealed record BookingResponse(
    Guid Id,
    BookingTripSummary Trip,
    BookingPassengerSummary Passenger,
    int PassengerCount,
    decimal Fare,
    PassengerCategory PassengerCategory,
    string? QrCode,
    bool CanBoard,
    BookingStatus Status,
    DateTime? CancelledAt,
    string? CancellationReason,
    decimal RefundAmount,
    IEnumerable<BookingTransactionSummary> Transactions,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static BookingResponse From(Booking booking, DateTime now)
    {
        var canBoard = BookingEligibility.CanBoard(booking, now);
        var trip = booking.Trip;
        var passenger = booking.Passenger;
        return new BookingResponse(
            booking.Id,
            new BookingTripSummary(trip.Id, trip.ScheduledTime, trip.Route.RouteNumber, trip.Route.Name, trip.Vehicle.PlateNumber),
            new BookingPassengerSummary(passenger.Id, passenger.FullName, passenger.PhoneNumber, passenger.Category, passenger.Wallet?.Balance),
            booking.PassengerCount,
            booking.Fare,
            booking.PassengerCategory,
            canBoard ? booking.QrCode : null,
            canBoard,
            booking.Status,
            booking.CancelledAt,
            booking.CancellationReason,
            booking.RefundAmount,
            booking.Transactions
                .OrderByDescending(transaction => transaction.CreatedAt)
                .Select(transaction => new BookingTransactionSummary(transaction.Id, transaction.Type, transaction.Amount, transaction.CreatedAt))
                .ToList(),
            booking.CreatedAt,
            booking.UpdatedAt);
    }
}

public sealed record BookingTripSummary(Guid Id, DateTime ScheduledTime, string Route, string Name, string Vehicle);

public sealed record BookingPassengerSummary(Guid Id, string FullName, string PhoneNumber, PassengerCategory Category, decimal? Balance);

public sealed record BookingTransactionSummary(Guid Id, TransactionType Type, decimal Amount, DateTime CreatedAt);

public sealed record BookingCreatedResponse(
    Guid Id,
    Guid TripId,
    Guid PassengerId,
    int PassengerCount,
    decimal Fare,
    BookingStatus Status,
    string QrCode);

public sealed record SeatAvailabilityResponse(int Capacity, int Occupied, int Available, bool IsFull);

public sealed record TripManifestResponse(ManifestTripSummary Trip, IReadOnlyList<ManifestBooking> Bookings, int PassengerCount);

public sealed record ManifestTripSummary(Guid Id, DateTime ScheduledTime, string Route, string Name, string Vehicle, int Capacity);

public sealed record ManifestBooking(
    Guid Id,
    int PassengerCount,
    string Passenger,
    string PhoneNumber,
    PassengerCategory Category,
    BookingStatus Status,
    string QrCode)
{
    public static ManifestBooking From(Booking booking) => new(
        booking.Id,
        booking.PassengerCount,
        booking.Passenger.FullName,
        booking.Passenger.PhoneNumber,
        booking.Passenger.Category,
        booking.Status,
        booking.QrCode);
}
