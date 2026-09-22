using api.Data;
using api.DTOs;
using api.Enums;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using api.Services.Payments;

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
            .Where(booking => booking.Passenger.UserId == parsedUserId)
            .OrderByDescending(booking => booking.CreatedAt)
            .Select(booking => new
            {
                booking.Id,
                booking.Status,
                booking.SeatNumber,
                booking.Fare,
                booking.QrCode,
                booking.CreatedAt,
                TripTime = booking.Trip.ScheduledTime,
                Route = booking.Trip.Route.RouteNumber,
                RouteName = booking.Trip.Route.Name
            })
            .ToListAsync(cancellationToken);
        return Ok(tickets);
    }

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

    [HttpGet("{bookingId:guid}")]
    public async Task<IActionResult> Get(Guid bookingId)
    {
        var booking = await LoadBooking(bookingId, true);
        if (booking is null) return NotFound();
        return Ok(ToDetail(booking));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateBookingRequest request)
    {
        var passenger = await db.Passengers.Include(item => item.Wallet).SingleOrDefaultAsync(item => item.Id == request.PassengerId);
        var trip = await db.Trips.Include(item => item.Vehicle).Include(item => item.Route).SingleOrDefaultAsync(item => item.Id == request.TripId);
        if (passenger is null || !passenger.IsActive) return BadRequest(new { error = "The selected passenger is not active." });
        if (trip is null) return BadRequest(new { error = "The selected trip does not exist." });
        if (trip.Vehicle is null || trip.Vehicle.Capacity < 1) return BadRequest(new { error = "This trip has no valid vehicle capacity configured." });
        if (trip.Status is not (TripStatus.Scheduled or TripStatus.Ready or TripStatus.Boarding)) return BadRequest(new { error = "Bookings are not available for this trip." });

        var fare = await db.FareRules.SingleOrDefaultAsync(rule => rule.RouteId == trip.RouteId && rule.PassengerCategory == passenger.Category && rule.IsActive);
        if (fare is null) return BadRequest(new { error = "No active fare rule exists for this passenger category and route." });
        if (passenger.Wallet is null || passenger.Wallet.Balance < fare.Amount) return BadRequest(new { error = "Insufficient wallet balance." });
        if (await ActiveBookingCount(trip.Id) >= trip.Vehicle.Capacity) return Conflict(new { error = "This departure is full. Please choose another departure." });

        await using var transaction = await db.Database.BeginTransactionAsync();
        var booking = new Booking
        {
            TripId = trip.Id,
            PassengerId = passenger.Id,
            SeatNumber = BoardingReference(),
            Fare = fare.Amount,
            PassengerCategory = passenger.Category,
            QrCode = $"BKG-{Guid.NewGuid():N}".ToUpperInvariant(),
            Status = BookingStatus.Confirmed
        };
        passenger.Wallet.Balance -= fare.Amount;
        passenger.Wallet.UpdatedAt = DateTime.UtcNow;
        db.Bookings.Add(booking);
        db.Transactions.Add(new Transaction { WalletId = passenger.Wallet.Id, Booking = booking, Type = TransactionType.Fare, Amount = fare.Amount });

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

        return CreatedAtAction(nameof(Get), new { bookingId = booking.Id }, new { booking.Id, booking.TripId, booking.PassengerId, booking.SeatNumber, booking.Fare, booking.Status, booking.QrCode });
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
            PassengerId = passenger.Id
        });
    }

    [HttpPost("{bookingId:guid}/seat")]
    public IActionResult ChangeSeat(Guid bookingId, ChangeBookingSeatRequest request)
    {
        return BadRequest(new { error = "Seat selection is not available for urban journeys. Your ticket provides boarding approval, not an assigned seat." });
    }

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

        await using var transaction = await db.Database.BeginTransactionAsync();
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
        var occupied = await ActiveBookingCount(tripId);
        return Ok(new { capacity = trip.Vehicle.Capacity, occupied, available = Math.Max(0, trip.Vehicle.Capacity - occupied), isFull = occupied >= trip.Vehicle.Capacity });
    }

    [HttpGet("trips/{tripId:guid}/manifest")]
    public async Task<IActionResult> Manifest(Guid tripId)
    {
        var trip = await db.Trips.AsNoTracking().Include(item => item.Route).Include(item => item.Vehicle).SingleOrDefaultAsync(item => item.Id == tripId);
        if (trip is null) return NotFound();
        var bookings = await db.Bookings.AsNoTracking().Include(item => item.Passenger)
            .Where(item => item.TripId == tripId && item.Status != BookingStatus.Cancelled)
            .OrderBy(item => item.SeatNumber)
            .Select(item => new { item.Id, item.SeatNumber, Passenger = item.Passenger.FullName, item.Passenger.PhoneNumber, item.Passenger.Category, item.Status, item.QrCode })
            .ToListAsync();
        return Ok(new { Trip = new { trip.Id, trip.ScheduledTime, Route = trip.Route.RouteNumber, trip.Route.Name, Vehicle = trip.Vehicle.PlateNumber }, Bookings = bookings, PassengerCount = bookings.Count });
    }

    private Task<int> ActiveBookingCount(Guid tripId) =>
        db.Bookings.CountAsync(booking => booking.TripId == tripId && (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed));

    private Task<Booking?> LoadBooking(Guid bookingId, bool noTracking) =>
        (noTracking ? db.Bookings.AsNoTracking() : db.Bookings)
            .Include(item => item.Passenger).ThenInclude(passenger => passenger.Wallet)
            .Include(item => item.Trip).ThenInclude(trip => trip.Route)
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
        booking.SeatNumber,
        booking.Fare,
        booking.Status,
        booking.CreatedAt
    };

    private static object ToDetail(Booking booking) => new
    {
        booking.Id,
        Trip = new { booking.Trip.Id, booking.Trip.ScheduledTime, Route = booking.Trip.Route.RouteNumber, booking.Trip.Route.Name, Vehicle = booking.Trip.Vehicle.PlateNumber },
        Passenger = new { booking.Passenger.Id, booking.Passenger.FullName, booking.Passenger.PhoneNumber, booking.Passenger.Category, Balance = booking.Passenger.Wallet?.Balance },
        booking.SeatNumber,
        booking.Fare,
        booking.PassengerCategory,
        booking.QrCode,
        booking.Status,
        booking.CancelledAt,
        booking.CancellationReason,
        booking.RefundAmount,
        Transactions = booking.Transactions.OrderByDescending(transaction => transaction.CreatedAt).Select(transaction => new { transaction.Id, transaction.Type, transaction.Amount, transaction.CreatedAt }),
        booking.CreatedAt,
        booking.UpdatedAt
    };
}
