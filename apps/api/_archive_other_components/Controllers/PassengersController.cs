using api.Data;
using api.DTOs;
using api.Enums;
using api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Route("api/v1/passengers")]
public class PassengersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PassengerCategory? category, [FromQuery] bool? active, [FromQuery] string? search)
    {
        var query = db.Passengers.AsNoTracking().Include(passenger => passenger.Wallet).AsQueryable();
        if (category.HasValue) query = query.Where(passenger => passenger.Category == category.Value);
        if (active.HasValue) query = query.Where(passenger => passenger.IsActive == active.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(passenger => passenger.FullName.ToLower().Contains(term) || passenger.PhoneNumber.Contains(term) || (passenger.Email != null && passenger.Email.ToLower().Contains(term)));
        }

        var passengers = await query.OrderBy(passenger => passenger.FullName).Select(passenger => new
        {
            passenger.Id,
            passenger.FullName,
            passenger.PhoneNumber,
            passenger.Email,
            passenger.Category,
            passenger.IsActive,
            Balance = passenger.Wallet != null ? passenger.Wallet.Balance : 0m,
            BookingCount = passenger.Bookings.Count
        }).ToListAsync();
        return Ok(passengers);
    }

    [HttpGet("{passengerId:guid}")]
    public async Task<IActionResult> Get(Guid passengerId)
    {
        var passenger = await db.Passengers.AsNoTracking()
            .Include(item => item.Wallet).ThenInclude(wallet => wallet!.Transactions)
            .Include(item => item.Bookings).ThenInclude(booking => booking.Trip).ThenInclude(trip => trip.Route)
            .SingleOrDefaultAsync(item => item.Id == passengerId);
        if (passenger is null) return NotFound();

        return Ok(new
        {
            passenger.Id,
            passenger.FullName,
            passenger.PhoneNumber,
            passenger.Email,
            passenger.Category,
            passenger.IsActive,
            Wallet = passenger.Wallet is null ? null : new
            {
                passenger.Wallet.Id,
                passenger.Wallet.Balance,
                Transactions = passenger.Wallet.Transactions.OrderByDescending(transaction => transaction.CreatedAt).Select(transaction => new { transaction.Id, transaction.BookingId, transaction.Type, transaction.Amount, transaction.CreatedAt })
            },
            Bookings = passenger.Bookings.OrderByDescending(booking => booking.CreatedAt).Select(booking => new { booking.Id, booking.TripId, Route = booking.Trip.Route.RouteNumber, booking.SeatNumber, booking.Fare, booking.Status, booking.CreatedAt })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePassengerRequest request)
    {
        var phoneNumber = NormalizePhone(request.PhoneNumber);
        if (await db.Passengers.AnyAsync(passenger => passenger.PhoneNumber == phoneNumber)) return Conflict(new { error = "A passenger with this phone number already exists." });

        var passenger = new Passenger
        {
            FullName = request.FullName.Trim(),
            PhoneNumber = phoneNumber,
            Email = CleanOptional(request.Email),
            Category = request.Category
        };
        passenger.Wallet = new Wallet { Passenger = passenger };
        db.Passengers.Add(passenger);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { passengerId = passenger.Id }, new { passenger.Id, passenger.FullName, passenger.PhoneNumber, passenger.Category, Balance = passenger.Wallet.Balance });
    }

    [HttpPut("{passengerId:guid}")]
    public async Task<IActionResult> Update(Guid passengerId, UpdatePassengerRequest request)
    {
        var passenger = await db.Passengers.FindAsync(passengerId);
        if (passenger is null) return NotFound();

        var phoneNumber = NormalizePhone(request.PhoneNumber);
        if (await db.Passengers.AnyAsync(item => item.Id != passengerId && item.PhoneNumber == phoneNumber)) return Conflict(new { error = "A passenger with this phone number already exists." });

        passenger.FullName = request.FullName.Trim();
        passenger.PhoneNumber = phoneNumber;
        passenger.Email = CleanOptional(request.Email);
        passenger.Category = request.Category;
        passenger.IsActive = request.IsActive;
        passenger.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{passengerId:guid}")]
    public async Task<IActionResult> Deactivate(Guid passengerId)
    {
        var passenger = await db.Passengers.FindAsync(passengerId);
        if (passenger is null) return NotFound();
        passenger.IsActive = false;
        passenger.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{passengerId:guid}/wallet/top-ups")]
    public async Task<IActionResult> TopUp(Guid passengerId, TopUpWalletRequest request)
    {
        var wallet = await db.Wallets.SingleOrDefaultAsync(item => item.PassengerId == passengerId);
        if (wallet is null) return NotFound();

        wallet.Balance += request.Amount;
        wallet.UpdatedAt = DateTime.UtcNow;
        var transaction = new Transaction { WalletId = wallet.Id, Type = TransactionType.Topup, Amount = request.Amount };
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { passengerId }, new { transaction.Id, wallet.Balance, transaction.Type, transaction.Amount, transaction.CreatedAt });
    }

    private static string NormalizePhone(string value) => value.Trim().Replace(" ", string.Empty).Replace("-", string.Empty);
    private static string? CleanOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
