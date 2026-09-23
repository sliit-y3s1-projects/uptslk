using api.Data;
using api.Enums;
using Microsoft.EntityFrameworkCore;

namespace api.Services.AgentRecovery.Agents;

public sealed class FleetReadinessAgent(AppDbContext db) : IRecoveryAgent
{
    public string Name => "Fleet Readiness Agent";

    public async Task<AgentRecommendation> AnalyseAsync(RecoveryContext context, CancellationToken cancellationToken)
    {
        var vehicles = await db.Vehicles.AsNoTracking()
            .Include(vehicle => vehicle.MaintenanceRecords)
            .Where(vehicle => vehicle.CentreId == context.Trip.CentreId && vehicle.Id != context.Trip.VehicleId && vehicle.Status == VehicleStatus.Active)
            .ToListAsync(cancellationToken);
        var candidate = vehicles
            .Where(vehicle => !vehicle.MaintenanceRecords.Any(record => record.Status == MaintenanceStatus.InProgress))
            .OrderByDescending(vehicle => vehicle.Capacity >= context.AffectedPassengers)
            .ThenBy(vehicle => vehicle.Capacity)
            .FirstOrDefault();
        if (candidate is null)
            return new AgentRecommendation(Name, "No roadworthy replacement bus is currently available.", [], ["The workflow cannot proceed until a Fleet Officer provides an active replacement bus."]);

        var capacityWarning = candidate.Capacity < context.AffectedPassengers
            ? $"The replacement has {candidate.Capacity} spaces for {context.AffectedPassengers} affected passengers."
            : null;
        return new AgentRecommendation(
            Name,
            $"Use {candidate.PlateNumber} ({candidate.Capacity} passenger capacity) as the replacement bus.",
            ["Vehicle is active at the affected trip's centre.", "Vehicle has no maintenance record currently in progress."],
            capacityWarning is null ? [] : [capacityWarning],
            VehicleId: candidate.Id,
            Capacity: candidate.Capacity);
    }
}
