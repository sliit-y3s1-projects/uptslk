using api.Data;
using api.Entities;
using api.Interfaces.Repositories;

namespace api.Repositories;

public sealed class VehicleRepository(AppDbContext db) : Repository<Vehicle>(db), IVehicleRepository
{
}
