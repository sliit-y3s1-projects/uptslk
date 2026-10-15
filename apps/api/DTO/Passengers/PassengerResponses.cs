using api.Entities;
using api.Entities.Enums;

namespace api.DTO.Passengers;

public sealed record PassengerListItemResponse(
    Guid Id,
    string FullName,
    string PhoneNumber,
    string? Email,
    PassengerCategory Category,
    bool IsActive,
    decimal Balance,
    int BookingCount)
{
    public static PassengerListItemResponse From(Passenger passenger) => new(
        passenger.Id,
        passenger.FullName,
        passenger.PhoneNumber,
        passenger.Email,
        passenger.Category,
        passenger.IsActive,
        passenger.Wallet?.Balance ?? 0m,
        passenger.Bookings.Count);
}

public sealed record PassengerResponse(
    Guid Id,
    string FullName,
    string PhoneNumber,
    string? Email,
    PassengerCategory Category,
    bool IsActive,
    PassengerWalletSummary? Wallet,
    IEnumerable<PassengerBookingSummary> Bookings)
{
    public static PassengerResponse From(Passenger passenger) => new(
        passenger.Id,
        passenger.FullName,
        passenger.PhoneNumber,
        passenger.Email,
        passenger.Category,
        passenger.IsActive,
        passenger.Wallet is null ? null : PassengerWalletSummary.From(passenger.Wallet),
        passenger.Bookings
            .OrderByDescending(booking => booking.CreatedAt)
            .Select(booking => new PassengerBookingSummary(
                booking.Id, booking.TripId, booking.Trip.Route.RouteNumber, booking.PassengerCount, booking.Fare, booking.Status, booking.CreatedAt))
            .ToList());
}

public sealed record PassengerWalletSummary(Guid Id, decimal Balance, IEnumerable<WalletTransactionSummary> Transactions)
{
    public static PassengerWalletSummary From(Wallet wallet) => new(
        wallet.Id,
        wallet.Balance,
        wallet.Transactions
            .OrderByDescending(transaction => transaction.CreatedAt)
            .Select(transaction => new WalletTransactionSummary(transaction.Id, transaction.BookingId, transaction.Type, transaction.Amount, transaction.CreatedAt))
            .ToList());
}

public sealed record WalletTransactionSummary(Guid Id, Guid? BookingId, TransactionType Type, decimal Amount, DateTime CreatedAt);

public sealed record PassengerBookingSummary(Guid Id, Guid TripId, string Route, int PassengerCount, decimal Fare, BookingStatus Status, DateTime CreatedAt);

public sealed record PassengerCreatedResponse(
    Guid Id,
    string FullName,
    string PhoneNumber,
    PassengerCategory Category,
    decimal Balance,
    bool PortalAccountCreated);

public sealed record WalletTopUpResponse(Guid Id, decimal Balance, TransactionType Type, decimal Amount, DateTime CreatedAt);
