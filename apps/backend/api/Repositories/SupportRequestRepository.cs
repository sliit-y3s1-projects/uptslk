using api.Data;
using api.DTO.Support;
using api.Entities;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class SupportRequestRepository(AppDbContext db) : Repository<SupportRequest>(db), ISupportRequestRepository
{
    public async Task<List<SupportRequest>> SearchAsync(SupportRequestFilter filter, CancellationToken cancellationToken = default)
    {
        var query = WithDetails();
        if (filter.Type.HasValue) query = query.Where(request => request.Type == filter.Type.Value);
        if (filter.Status.HasValue) query = query.Where(request => request.Status == filter.Status.Value);
        if (filter.PassengerId.HasValue) query = query.Where(request => request.PassengerId == filter.PassengerId.Value);
        if (filter.TripId.HasValue) query = query.Where(request => request.TripId == filter.TripId.Value);
        return await query.OrderByDescending(request => request.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<SupportRequest?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        WithDetails().SingleOrDefaultAsync(request => request.Id == id, cancellationToken);

    private IQueryable<SupportRequest> WithDetails() =>
        Set.AsNoTracking()
            .Include(request => request.Passenger)
            .Include(request => request.Trip).ThenInclude(trip => trip!.Route);
}
