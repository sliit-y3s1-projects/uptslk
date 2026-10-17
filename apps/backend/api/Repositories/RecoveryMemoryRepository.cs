using api.Data;
using api.Entities;
using api.Entities.Enums;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class RecoveryMemoryRepository(AppDbContext db) : Repository<RecoveryMemory>(db), IRecoveryMemoryRepository
{
    public Task<List<RecoveryMemory>> ListForContextAsync(Guid centreId, string routeNumber, IncidentType incidentType, int take, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Where(memory => memory.CentreId == centreId && memory.RouteNumber == routeNumber && memory.IncidentType == incidentType)
            .OrderByDescending(memory => memory.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
}
