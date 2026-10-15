using api.Data;
using api.DTO.Passengers;
using api.Entities;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class PassengerRepository(AppDbContext db) : Repository<Passenger>(db), IPassengerRepository
{
    public async Task<List<Passenger>> SearchAsync(PassengerFilter filter, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking().Include(passenger => passenger.Wallet).Include(passenger => passenger.Bookings).AsQueryable();
        if (filter.Category.HasValue) query = query.Where(passenger => passenger.Category == filter.Category.Value);
        if (filter.Active.HasValue) query = query.Where(passenger => passenger.IsActive == filter.Active.Value);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(passenger => passenger.FullName.ToLower().Contains(term)
                || passenger.PhoneNumber.Contains(term)
                || (passenger.Email != null && passenger.Email.ToLower().Contains(term)));
        }
        return await query.OrderBy(passenger => passenger.FullName).ToListAsync(cancellationToken);
    }

    public Task<Passenger?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(passenger => passenger.Wallet).ThenInclude(wallet => wallet!.Transactions)
            .Include(passenger => passenger.Bookings).ThenInclude(booking => booking.Trip).ThenInclude(trip => trip.Route)
            .SingleOrDefaultAsync(passenger => passenger.Id == id, cancellationToken);

    public Task<Passenger?> GetWithUserAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.Include(passenger => passenger.User).SingleOrDefaultAsync(passenger => passenger.Id == id, cancellationToken);

    public Task<Passenger?> GetWithWalletAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.Include(passenger => passenger.Wallet).SingleOrDefaultAsync(passenger => passenger.Id == id, cancellationToken);

    public Task<Passenger?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Set.SingleOrDefaultAsync(passenger => passenger.UserId == userId, cancellationToken);

    public Task<Guid?> GetIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Where(passenger => passenger.UserId == userId)
            .Select(passenger => (Guid?)passenger.Id)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<Passenger?> GetActiveAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().SingleOrDefaultAsync(passenger => passenger.Id == id && passenger.IsActive, cancellationToken);

    public Task<Guid?> GetActiveIdByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Where(passenger => passenger.UserId == userId && passenger.IsActive)
            .Select(passenger => (Guid?)passenger.Id)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<bool> PhoneNumberExistsAsync(string phoneNumber, Guid? exceptPassengerId = null, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(passenger => passenger.PhoneNumber == phoneNumber && passenger.Id != exceptPassengerId, cancellationToken);
}
