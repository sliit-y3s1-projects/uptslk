using api.Data;
using api.DTOs;
using api.Enums;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using api.Services.Payments;
using api.Services;

namespace api.Controllers;

[ApiController]
[Route("api/v1/bookings")]
public class BookingsController(AppDbContext db, BookingPaymentService bookingPayments) : ControllerBase
{
    [Authorize(Roles = "Commuter")]
    [HttpGet("me")]
    public async Task<IActionResult> ListForCurrentUser(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsedUserId)) return Unauthorized();

        var tickets = await db.Bookings.AsNoTracking()
            .Include(booking => booking.Trip).ThenInclude(trip => trip.Route)
            .Include(booking => booking.Trip).ThenInclude(trip => trip.Bay)
            .Include(booking => booking.Trip).ThenInclude(trip => trip.RouteDirection).ThenInclude(direction => direction!.StartCentre)
            .Include(booking => booking.Trip).ThenInclude(trip => trip.RouteDirection).ThenInclude(direction => direction!.EndCentre)
            .Include(booking => booking.Passenger)
            .Where(booking => booking.Passenger.UserId == parsedUserId)
            .OrderByDescending(booking => booking.CreatedAt)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        return Ok(tickets.Select(booking => new
            {
                booking.Id,
                booking.Status,
                booking.PassengerCount,
                booking.Fare,
                QrCode = BookingEligibility.CanBoard(booking, now) ? booking.QrCode : null,
                CanBoard = BookingEligibility.CanBoard(booking, now),
                TicketGroup = BookingEligibility.TicketGroup(booking, now),
                TripStatus = booking.Trip.Status,
                ActiveUntil = BookingEligibility.ActiveUntil(booking.Trip),
                PassengerName = booking.Passenger.FullName,
                Bay = booking.Trip.Bay.Code,
                Origin = booking.Trip.RouteDirection?.StartCentre.Name ?? booking.Trip.Route.Origin,
                Destination = booking.Trip.RouteDirection?.EndCentre.Name ?? booking.Trip.Route.Destination,
                booking.CreatedAt,
                TripTime = booking.Trip.ScheduledTime,
                Route = booking.Trip.Route.RouteNumber,
                RouteName = booking.Trip.Route.Name
            }));
    }

    [Authorize(Roles = "Admin,CentreManager,Dispatcher")]
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? passengerId, [FromQuery] Guid? tripId, [FromQuery] BookingStatus? status)
    {
        var query = db.Bookings.AsNoTracking()
            .Include(booking => booking.Passenger)
            .Include(booking => booking.Trip).ThenInclude(trip => trip.Route)
            .AsQueryable();
        if (passengerId.HasValue) query = query.Where(booking => booking.PassengerId == passengerId.Value);
        if (tripId.HasValue) query = query.Where(booking => booking.TripId == tripId.Value);
        if (status.HasValue) query = query.Where(booking => booking.Status == status.Value);

        var bookings = await query.OrderByDescending(booking => booking.CreatedAt).ToListAsync();
        return Ok(bookings.Select(ToListItem));
    }

    [Authorize(Roles = "Admin,CentreManager,Dispatcher")]
    [HttpGet("{bookingId:guid}")]
    public async Task<IActionResult> Get(Guid bookingId)
    {
        var booking = await LoadBooking(bookingId, true);
        if (booking is null) return NotFound();
        return Ok(ToDetail(booking));
    }

    [Authorize(Roles = "Admin,CentreManager,Dispatcher")]
    [HttpPost]
    public async Task<IActionResult> Create(CreateBookingRequest request)
    {
        var passenger = await db.Passengers.Include(item => item.Wallet).SingleOrDefaultAsync(item => item.Id == request.PassengerId);
        var trip = await db.Trips.Include(item => item.Vehicle).Include(item => item.Route).SingleOrDefaultAsync(item => item.Id == request.TripId);
        if (passenger is null || !passenger.IsActive) return BadRequest(new { error = "The selected passenger is not active." });
        if (trip is null) return BadRequest(new { error = "The selected trip does not exist." });
        if (trip.Vehicle is null || trip.Vehicle.Capacity < 1) return BadRequest(new { error = "This trip has no valid vehicle capacity configured." });
        if (!BookingEligibility.IsOpenForBooking(trip, DateTime.UtcNow)) return BadRequest(new { error = "Bookings have closed for this departure. Please choose a later trip." });
        if (request.PassengerCount is < 1 or > 10) return BadRequest(new { error = "Passenger count must be between 1 and 10." });

        var fare = await db.FareRules.SingleOrDefaultAsync(rule => rule.RouteId == trip.RouteId && rule.IsActive);
        if (fare is null) return BadRequest(new { error = "No active standard fare exists for this route." });
        var totalFare = fare.Amount * request.PassengerCount;
        if (passenger.Wallet is null) return BadRequest(new { error = "Insufficient wallet balance." });

        // Lock the trip and the wallet so parallel requests are checked one at a time. Without this two requests can
        // both pass the capacity and balance checks and oversell the trip or spend the same money twice.
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.LockRowAsync("Trips", trip.Id);
        await db.LockRowAsync("Wallets", passenger.Wallet.Id);
        await db.Entry(passenger.Wallet).ReloadAsync();
        if (passenger.Wallet.Balance < totalFare) return BadRequest(new { error = "Insufficient wallet balance." });
        if (await OccupiedCapacity(trip.Id) + request.PassengerCount > trip.Vehicle.Capacity) return Conflict(new { error = "This departure does not have enough remaining spaces for every passenger." });

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
        passenger.Wallet.Balance -= totalFare;
        passenger.Wallet.UpdatedAt = DateTime.UtcNow;
        db.Bookings.Add(booking);
        db.Transactions.Add(new Transaction { WalletId = passenger.Wallet.Id, Booking = booking, Type = TransactionType.Fare, Amount = totalFare });

        try
        {
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return Conflict(new { error = "This departure was just filled. Please choose another departure." });
        }

        return CreatedAtAction(nameof(Get), new { bookingId = booking.Id }, new { booking.Id, booking.TripId, booking.PassengerId, booking.PassengerCount, booking.Fare, booking.Status, booking.QrCode });
    }

    [HttpPost("me")]
    [Authorize(Roles = "Commuter")]
    public async Task<IActionResult> CreateForCurrentUser(CreateMyBookingRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userId, out var parsedUserId)) return Unauthorized();

        var passenger = await db.Passengers.AsNoTracking().SingleOrDefaultAsync(item => item.UserId == parsedUserId);
        if (passenger is null) return BadRequest(new { error = "No passenger profile is linked to this account." });

        return await Create(new CreateBookingRequest
        {
            TripId = request.TripId,
            PassengerId = passenger.Id,
            PassengerCount = request.PassengerCount
        });
    }

    [Authorize(Roles = "Admin,CentreManager,Dispatcher")]
    [HttpPatch("{bookingId:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid bookingId, UpdateBookingStatusRequest request)
    {
        var booking = await db.Bookings.Include(item => item.Trip).SingleOrDefaultAsync(item => item.Id == bookingId);
        if (booking is null) return NotFound();
        if (booking.Status != BookingStatus.Confirmed || request.Status != BookingStatus.Completed || booking.Trip.Status is not (TripStatus.Dispatched or TripStatus.Completed)) return BadRequest(new { error = "Only confirmed bookings on dispatched or completed trips can be completed." });

        booking.Status = BookingStatus.Completed;
        booking.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = "Admin,CentreManager,Dispatcher")]
    [HttpDelete("{bookingId:guid}")]
    public async Task<IActionResult> Cancel(Guid bookingId, CancelBookingRequest request)
    {
        var booking = await db.Bookings.Include(item => item.Payments).Include(item => item.Passenger).ThenInclude(passenger => passenger.Wallet).SingleOrDefaultAsync(item => item.Id == bookingId);
        if (booking is null) return NotFound();
        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Completed) return BadRequest(new { error = "Only active bookings can be cancelled." });
        if (booking.Payments.Any(payment => payment.Status == PaymentStatus.Succeeded))
        {
            var (error, statusCode) = await bookingPayments.RequestRefundAsync(bookingId, request.Reason, HttpContext.RequestAborted);
            return error is null ? NoContent() : StatusCode(statusCode, new { error });
        }
        if (booking.Payments.Count > 0)
        {
            booking.Status = BookingStatus.Cancelled;
            booking.CancellationReason = request.Reason.Trim();
            booking.CancelledAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return NoContent();
        }
        if (booking.Passenger.Wallet is null) return BadRequest(new { error = "The passenger wallet does not exist." });

        // Lock the booking and wallet, then re-read them, so a duplicate cancel request cannot refund the booking twice.
        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.LockRowAsync("Bookings", booking.Id);
        await db.LockRowAsync("Wallets", booking.Passenger.Wallet.Id);
        await db.Entry(booking).ReloadAsync();
        await db.Entry(booking.Passenger.Wallet).ReloadAsync();
        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Completed) return BadRequest(new { error = "Only active bookings can be cancelled." });
        booking.Status = BookingStatus.Cancelled;
        booking.CancellationReason = request.Reason.Trim();
        booking.CancelledAt = DateTime.UtcNow;
        booking.RefundAmount = booking.Fare;
        booking.UpdatedAt = DateTime.UtcNow;
        booking.Passenger.Wallet.Balance += booking.RefundAmount;
        booking.Passenger.Wallet.UpdatedAt = DateTime.UtcNow;
        db.Transactions.Add(new Transaction { WalletId = booking.Passenger.Wallet.Id, BookingId = booking.Id, Type = TransactionType.Refund, Amount = booking.RefundAmount });
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return NoContent();
    }

    [HttpGet("trips/{tripId:guid}/seats")]
    public async Task<IActionResult> Seats(Guid tripId)
    {
        var trip = await db.Trips.AsNoTracking().Include(item => item.Vehicle).SingleOrDefaultAsync(item => item.Id == tripId);
        if (trip is null) return NotFound();
        var occupied = await OccupiedCapacity(tripId);
        return Ok(new { capacity = trip.Vehicle.Capacity, occupied, available = Math.Max(0, trip.Vehicle.Capacity - occupied), isFull = occupied >= trip.Vehicle.Capacity });
    }

    [Authorize(Roles = "Admin,CentreManager,Dispatcher")]
    [HttpGet("trips/{tripId:guid}/manifest")]
    public async Task<IActionResult> Manifest(Guid tripId)
    {
        var trip = await db.Trips.AsNoTracking().Include(item => item.Route).Include(item => item.Vehicle).SingleOrDefaultAsync(item => item.Id == tripId);
        if (trip is null) return NotFound();
        var bookings = await db.Bookings.AsNoTracking().Include(item => item.Passenger)
            .Where(item => item.TripId == tripId && item.Status != BookingStatus.Cancelled)
            .OrderBy(item => item.CreatedAt)
            .Select(item => new { item.Id, item.PassengerCount, Passenger = item.Passenger.FullName, item.Passenger.PhoneNumber, item.Passenger.Category, item.Status, item.QrCode })
            .ToListAsync();
        return Ok(new { Trip = new { trip.Id, trip.ScheduledTime, Route = trip.Route.RouteNumber, trip.Route.Name, Vehicle = trip.Vehicle.PlateNumber, Capacity = trip.Vehicle.Capacity }, Bookings = bookings, PassengerCount = bookings.Sum(item => item.PassengerCount) });
    }

    private async Task<int> OccupiedCapacity(Guid tripId) =>
        await db.Bookings.Where(booking => booking.TripId == tripId && (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed)).SumAsync(booking => (int?)booking.PassengerCount) ?? 0;

    private Task<Booking?> LoadBooking(Guid bookingId, bool noTracking) =>
        (noTracking ? db.Bookings.AsNoTracking() : db.Bookings)
            .Include(item => item.Passenger).ThenInclude(passenger => passenger.Wallet)
            .Include(item => item.Trip).ThenInclude(trip => trip.Route)
            .Include(item => item.Trip).ThenInclude(trip => trip.RouteDirection)
            .Include(item => item.Trip).ThenInclude(trip => trip.Vehicle)
            .Include(item => item.Transactions)
            .SingleOrDefaultAsync(item => item.Id == bookingId);

    private static string BoardingReference() => $"B{Guid.NewGuid():N}"[..8].ToUpperInvariant();

    private static object ToListItem(Booking booking) => new
    {
        booking.Id,
        booking.TripId,
        Route = booking.Trip.Route.RouteNumber,
        TripTime = booking.Trip.ScheduledTime,
        booking.PassengerId,
        Passenger = booking.Passenger.FullName,
        booking.PassengerCount,
        booking.Fare,
        booking.Status,
        booking.CreatedAt
    };

    private static object ToDetail(Booking booking) => new
    {
        booking.Id,
        Trip = new { booking.Trip.Id, booking.Trip.ScheduledTime, Route = booking.Trip.Route.RouteNumber, booking.Trip.Route.Name, Vehicle = booking.Trip.Vehicle.PlateNumber },
        Passenger = new { booking.Passenger.Id, booking.Passenger.FullName, booking.Passenger.PhoneNumber, booking.Passenger.Category, Balance = booking.Passenger.Wallet?.Balance },
        booking.PassengerCount,
        booking.Fare,
        booking.PassengerCategory,
        QrCode = BookingEligibility.CanBoard(booking, DateTime.UtcNow) ? booking.QrCode : null,
        CanBoard = BookingEligibility.CanBoard(booking, DateTime.UtcNow),
        booking.Status,
        booking.CancelledAt,
        booking.CancellationReason,
        booking.RefundAmount,
        Transactions = booking.Transactions.OrderByDescending(transaction => transaction.CreatedAt).Select(transaction => new { transaction.Id, transaction.Type, transaction.Amount, transaction.CreatedAt }),
        booking.CreatedAt,
        booking.UpdatedAt
    };
}
