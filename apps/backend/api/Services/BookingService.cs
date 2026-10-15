using api.DTO.Bookings;
using api.Entities;
using api.Entities.Enums;
using api.Exceptions;
using api.Interfaces.Repositories;
using api.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public sealed class BookingService(
    IBookingRepository bookings,
    IPassengerRepository passengers,
    ITripRepository trips,
    IFareRuleRepository fareRules,
    IWalletRepository wallets,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    IBookingCancellationService cancellation) : IBookingService
{
    private const string NotFoundMessage = "Booking not found.";
    private const string TripNotFound = "Trip not found.";
    private const string BookingsClosed = "Bookings have closed for this departure. Please choose a later trip.";
    private const string InsufficientBalance = "Insufficient wallet balance.";

    public async Task<IReadOnlyList<TicketResponse>> ListTicketsForCurrentUserAsync(CancellationToken cancellationToken)
    {
        var tickets = await bookings.GetTicketsForUserAsync(currentUser.RequireUserId(), cancellationToken);
        var now = DateTime.UtcNow;
        return tickets.Select(booking => TicketResponse.From(booking, now)).ToList();
    }

    public async Task<IReadOnlyList<BookingListItemResponse>> ListAsync(BookingFilter filter, CancellationToken cancellationToken)
    {
        var found = await bookings.SearchAsync(filter, cancellationToken);
        return found.Select(BookingListItemResponse.From).ToList();
    }

    public async Task<BookingResponse> GetAsync(Guid bookingId, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetWithDetailsAsync(bookingId, cancellationToken) ?? throw new NotFoundException(NotFoundMessage);
        return BookingResponse.From(booking, DateTime.UtcNow);
    }

    public async Task<BookingCreatedResponse> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var passenger = await passengers.GetWithWalletAsync(request.PassengerId, cancellationToken);
        var trip = await trips.GetWithVehicleAndRouteAsync(request.TripId, track: true, cancellationToken);
        if (passenger is null || !passenger.IsActive) throw new BadRequestException("The selected passenger is not active.");
        if (trip is null) throw new BadRequestException("The selected trip does not exist.");
        if (trip.Vehicle is null || trip.Vehicle.Capacity < 1) throw new BadRequestException("This trip has no valid vehicle capacity configured.");
        if (!BookingEligibility.IsOpenForBooking(trip, DateTime.UtcNow)) throw new BadRequestException(BookingsClosed);
        if (request.PassengerCount is < 1 or > 10) throw new BadRequestException("Passenger count must be between 1 and 10.");

        var fare = await fareRules.GetActiveForRouteAsync(trip.RouteId, cancellationToken)
            ?? throw new BadRequestException("No active standard fare exists for this route.");
        var totalFare = fare.Amount * request.PassengerCount;
        var wallet = passenger.Wallet ?? throw new BadRequestException(InsufficientBalance);

        // Lock the trip and wallet so parallel bookings cannot oversell the trip or spend the same money twice.
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await trips.LockAndReloadAsync(trip, cancellationToken);
        await wallets.LockAndReloadAsync(wallet, cancellationToken);

        // The trip may have been cancelled while this request waited for the lock, so check it again.
        if (!BookingEligibility.IsOpenForBooking(trip, DateTime.UtcNow)) throw new BadRequestException(BookingsClosed);
        if (wallet.Balance < totalFare) throw new BadRequestException(InsufficientBalance);
        if (await bookings.GetOccupiedCapacityAsync(trip.Id, cancellationToken) + request.PassengerCount > trip.Vehicle.Capacity)
            throw new ConflictException("This departure does not have enough remaining spaces for every passenger.");

        var booking = new Booking
        {
            TripId = trip.Id,
            PassengerId = passenger.Id,
            SeatNumber = BoardingReference(),
            PassengerCount = request.PassengerCount,
            Fare = totalFare,
            PassengerCategory = passenger.Category,
            QrCode = $"BKG-{Guid.NewGuid():N}".ToUpperInvariant(),
            Status = BookingStatus.Confirmed
        };
        wallet.Balance -= totalFare;
        wallet.UpdatedAt = DateTime.UtcNow;
        bookings.Add(booking);
        wallets.AddTransaction(new Transaction { WalletId = wallet.Id, Booking = booking, Type = TransactionType.Fare, Amount = totalFare });

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ConflictException("This departure was just filled. Please choose another departure.");
        }

        return new BookingCreatedResponse(booking.Id, booking.TripId, booking.PassengerId, booking.PassengerCount, booking.Fare, booking.Status, booking.QrCode);
    }

    public async Task<BookingCreatedResponse> CreateForCurrentUserAsync(CreateMyBookingRequest request, CancellationToken cancellationToken)
    {
        var passengerId = await passengers.GetIdByUserIdAsync(currentUser.RequireUserId(), cancellationToken)
            ?? throw new BadRequestException("No passenger profile is linked to this account.");

        return await CreateAsync(new CreateBookingRequest
        {
            TripId = request.TripId,
            PassengerId = passengerId,
            PassengerCount = request.PassengerCount
        }, cancellationToken);
    }

    public async Task CompleteAsync(Guid bookingId, UpdateBookingStatusRequest request, CancellationToken cancellationToken)
    {
        var booking = await bookings.GetWithTripAsync(bookingId, cancellationToken) ?? throw new NotFoundException(NotFoundMessage);
        var canComplete = booking.Status == BookingStatus.Confirmed
            && request.Status == BookingStatus.Completed
            && booking.Trip.Status is TripStatus.Dispatched or TripStatus.Completed;
        if (!canComplete) throw new BadRequestException("Only confirmed bookings on dispatched or completed trips can be completed.");

        booking.Status = BookingStatus.Completed;
        booking.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task CancelAsync(Guid bookingId, CancelBookingRequest request, CancellationToken cancellationToken)
    {
        await cancellation.CancelBookingAsync(bookingId, request.Reason, cancellationToken);
    }

    public async Task<SeatAvailabilityResponse> GetSeatAvailabilityAsync(Guid tripId, CancellationToken cancellationToken)
    {
        var trip = await trips.GetWithVehicleAndRouteAsync(tripId, cancellationToken: cancellationToken) ?? throw new NotFoundException(TripNotFound);
        var capacity = trip.Vehicle.Capacity;
        var occupied = await bookings.GetOccupiedCapacityAsync(tripId, cancellationToken);
        return new SeatAvailabilityResponse(capacity, occupied, Math.Max(0, capacity - occupied), occupied >= capacity);
    }

    public async Task<TripManifestResponse> GetManifestAsync(Guid tripId, CancellationToken cancellationToken)
    {
        var trip = await trips.GetWithVehicleAndRouteAsync(tripId, cancellationToken: cancellationToken) ?? throw new NotFoundException(TripNotFound);
        var manifest = (await bookings.GetManifestAsync(tripId, cancellationToken)).Select(ManifestBooking.From).ToList();
        return new TripManifestResponse(
            new ManifestTripSummary(trip.Id, trip.ScheduledTime, trip.Route.RouteNumber, trip.Route.Name, trip.Vehicle.PlateNumber, trip.Vehicle.Capacity),
            manifest,
            manifest.Sum(item => item.PassengerCount));
    }

    private static string BoardingReference() => $"B{Guid.NewGuid():N}"[..8].ToUpperInvariant();
}
