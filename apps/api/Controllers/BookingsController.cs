using api.Data;
using api.DTOs;
using api.Enums;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace api.Controllers;

[ApiController]
[Route("api/v1/bookings")]
public class BookingsController(AppDbContext db) : ControllerBase
{
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

        var seatNumber = NormalizeSeat(request.SeatNumber, trip.Vehicle.Capacity);
        if (seatNumber is null) return BadRequest(new { error = $"Seat number must be between 1 and {trip.Vehicle.Capacity}." });
        var fare = await db.FareRules.SingleOrDefaultAsync(rule => rule.RouteId == trip.RouteId && rule.PassengerCategory == passenger.Category && rule.IsActive);
        if (fare is null) return BadRequest(new { error = "No active fare rule exists for this passenger category and route." });
        if (passenger.Wallet is null || passenger.Wallet.Balance < fare.Amount) return BadRequest(new { error = "Insufficient wallet balance." });
        if (await HasActiveSeatBooking(trip.Id, seatNumber)) return Conflict(new { error = "This seat is already booked for the selected trip." });

        await using var transaction = await db.Database.BeginTransactionAsync();
        var booking = new Booking
        {
            TripId = trip.Id,
            PassengerId = passenger.Id,
            SeatNumber = seatNumber,
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
            return Conflict(new { error = "This seat was just booked by another passenger. Select another seat." });
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
            PassengerId = passenger.Id,
            SeatNumber = request.SeatNumber
        });
    }

    [HttpPost("{bookingId:guid}/seat")]
    public async Task<IActionResult> ChangeSeat(Guid bookingId, ChangeBookingSeatRequest request)
    {
        var booking = await db.Bookings.Include(item => item.Trip).ThenInclude(trip => trip.Vehicle).SingleOrDefaultAsync(item => item.Id == bookingId);
        if (booking is null) return NotFound();
        if (booking.Status != BookingStatus.Confirmed || booking.Trip.Status is not (TripStatus.Scheduled or TripStatus.Ready)) return BadRequest(new { error = "Only confirmed bookings before boarding can change seats." });

        var seatNumber = NormalizeSeat(request.SeatNumber, booking.Trip.Vehicle.Capacity);
        if (seatNumber is null) return BadRequest(new { error = $"Seat number must be between 1 and {booking.Trip.Vehicle.Capacity}." });
        if (seatNumber != booking.SeatNumber && await HasActiveSeatBooking(booking.TripId, seatNumber)) return Conflict(new { error = "This seat is already booked for the selected trip." });

        booking.SeatNumber = seatNumber;
        booking.UpdatedAt = DateTime.UtcNow;
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "This seat was just booked by another passenger. Select another seat." });
        }
        return NoContent();
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
        var booking = await db.Bookings.Include(item => item.Passenger).ThenInclude(passenger => passenger.Wallet).SingleOrDefaultAsync(item => item.Id == bookingId);
        if (booking is null) return NotFound();
        if (booking.Status is BookingStatus.Cancelled or BookingStatus.Completed) return BadRequest(new { error = "Only active bookings can be cancelled." });
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
        var occupied = await db.Bookings.AsNoTracking()
            .Where(booking => booking.TripId == tripId && (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed))
            .Select(booking => booking.SeatNumber)
            .ToListAsync();
        var occupiedSeats = occupied.ToHashSet(StringComparer.Ordinal);
        return Ok(Enumerable.Range(1, trip.Vehicle.Capacity).Select(number => new { SeatNumber = number.ToString(), IsAvailable = !occupiedSeats.Contains(number.ToString()) }));
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

    private Task<bool> HasActiveSeatBooking(Guid tripId, string seatNumber) =>
        db.Bookings.AnyAsync(booking => booking.TripId == tripId && booking.SeatNumber == seatNumber && (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed));

    private Task<Booking?> LoadBooking(Guid bookingId, bool noTracking) =>
        (noTracking ? db.Bookings.AsNoTracking() : db.Bookings)
            .Include(item => item.Passenger).ThenInclude(passenger => passenger.Wallet)
            .Include(item => item.Trip).ThenInclude(trip => trip.Route)
            .Include(item => item.Trip).ThenInclude(trip => trip.Vehicle)
            .Include(item => item.Transactions)
            .SingleOrDefaultAsync(item => item.Id == bookingId);

    private static string? NormalizeSeat(string value, int capacity)
    {
        if (!int.TryParse(value.Trim(), out var seat) || seat < 1 || seat > capacity) return null;
        return seat.ToString();
    }

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
