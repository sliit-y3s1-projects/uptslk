using api.Data;
using api.Entities;
using api.Interfaces.Repositories;

namespace api.Repositories;

public sealed class CentreRepository(AppDbContext db) : Repository<Centre>(db), ICentreRepository
{
}
