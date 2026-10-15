using api.Data;
using api.Entities;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class BayRepository(AppDbContext db) : Repository<Bay>(db), IBayRepository
{
    public Task<List<Bay>> ListForCentreAsync(Guid centreId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().Where(bay => bay.CentreId == centreId).OrderBy(bay => bay.Code).ToListAsync(cancellationToken);

    public Task<Bay?> GetReadOnlyAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().SingleOrDefaultAsync(bay => bay.Id == id, cancellationToken);

    public Task<Bay?> GetInCentreAsync(Guid bayId, Guid centreId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().SingleOrDefaultAsync(bay => bay.Id == bayId && bay.CentreId == centreId, cancellationToken);

    public Task<bool> CodeExistsAsync(Guid centreId, string code, Guid? exceptBayId = null, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(bay => bay.CentreId == centreId && bay.Code == code && bay.Id != exceptBayId, cancellationToken);
}
