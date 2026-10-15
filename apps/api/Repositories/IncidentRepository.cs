using api.Data;
using api.DTO.Incidents;
using api.Entities;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class IncidentRepository(AppDbContext db) : Repository<Incident>(db), IIncidentRepository
{
    public async Task<List<Incident>> SearchAsync(IncidentFilter filter, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking().Include(incident => incident.Trip).ThenInclude(trip => trip!.Route).AsQueryable();
        if (filter.CentreId.HasValue) query = query.Where(incident => incident.CentreId == filter.CentreId.Value);
        if (filter.TripId.HasValue) query = query.Where(incident => incident.TripId == filter.TripId.Value);
        if (filter.Status.HasValue) query = query.Where(incident => incident.Status == filter.Status.Value);
        if (filter.Type.HasValue) query = query.Where(incident => incident.Type == filter.Type.Value);
        if (filter.Severity.HasValue) query = query.Where(incident => incident.Severity == filter.Severity.Value);
        return await query.OrderByDescending(incident => incident.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<Incident?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(incident => incident.Centre)
            .Include(incident => incident.Trip)
            .SingleOrDefaultAsync(incident => incident.Id == id, cancellationToken);

    public Task<bool> ExistsInCentreAsync(Guid id, Guid centreId, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(incident => incident.Id == id && incident.CentreId == centreId, cancellationToken);
}
