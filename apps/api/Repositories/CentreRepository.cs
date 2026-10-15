using api.Data;
using api.DTO.Centres;
using api.Entities;
using api.Entities.Enums;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class CentreRepository(AppDbContext db) : Repository<Centre>(db), ICentreRepository
{
    public async Task<List<Centre>> SearchAsync(CentreFilter filter, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking()
            .Include(centre => centre.Bays)
            .Include(centre => centre.Routes.Where(route => route.IsActive))
            .AsQueryable();

        // Closed centres are hidden unless asked for, or unless a status filter is given.
        if (!filter.IncludeClosed && !filter.Status.HasValue) query = query.Where(centre => centre.Status != CentreStatus.Closed);
        if (filter.Status.HasValue) query = query.Where(centre => centre.Status == filter.Status.Value);
        if (!string.IsNullOrWhiteSpace(filter.District))
        {
            var district = filter.District.Trim();
            query = query.Where(centre => centre.District == district);
        }
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(centre => centre.Code.ToLower().Contains(term) || centre.Name.ToLower().Contains(term) || centre.City.ToLower().Contains(term));
        }

        return await query.OrderBy(centre => centre.Name).ToListAsync(cancellationToken);
    }

    public Task<Centre?> GetWithBaysAndRoutesAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking()
            .Include(centre => centre.Bays)
            .Include(centre => centre.Routes)
            .SingleOrDefaultAsync(centre => centre.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(centre => centre.Code == code, cancellationToken);
}
