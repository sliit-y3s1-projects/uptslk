using api.Data;
using api.Enums;
using Microsoft.EntityFrameworkCore;

namespace api.Services.AgentRecovery.Agents;

public sealed class DispatchRecoveryAgent(AppDbContext db, TripConflictService conflictService) : IRecoveryAgent
{
    public string Name => "Dispatch Recovery Agent";
    public IReadOnlySet<RecoveryToolName> AllowedTools { get; } = new HashSet<RecoveryToolName>
    {
        RecoveryToolName.FindConflictFreeDriver
    };

    public async Task<AgentExecutionResult> AnalyseAsync(RecoveryContext context, CancellationToken cancellationToken)
    {
        var drivers = await db.Drivers.AsNoTracking()
            .Where(driver => driver.CentreId == context.Trip.CentreId && driver.Id != context.Trip.DriverId && driver.Status == DriverStatus.Active)
            .OrderBy(driver => driver.FullName)
            .ToListAsync(cancellationToken);
        var vehicle = (await db.Vehicles.AsNoTracking()
            .Include(item => item.MaintenanceRecords)
            .Where(item => item.CentreId == context.Trip.CentreId && item.Status == VehicleStatus.Active && item.Id != context.Trip.VehicleId)
            .ToListAsync(cancellationToken))
            .Where(item => !item.MaintenanceRecords.Any(record => record.Status == MaintenanceStatus.InProgress))
            .OrderByDescending(item => item.Capacity >= context.AffectedPassengers)
            .ThenBy(item => item.Capacity)
            .FirstOrDefault();
        var departureCentreId = context.Trip.RouteDirection?.StartCentreId ?? context.Trip.CentreId;
        var bay = await db.Bays.AsNoTracking()
            .Where(item => item.CentreId == departureCentreId && item.Status == BayStatus.Available && item.Id != context.Trip.BayId)
            .OrderBy(item => item.Code)
            .FirstOrDefaultAsync(cancellationToken);
        var proposedTime = context.Trip.ScheduledTime.AddMinutes(15);
        var duration = context.Trip.RouteDirection?.EstimatedDurationMin ?? context.Trip.Route.EstimatedDurationMin;
        if (drivers.Count == 0)
        {
            var unavailable = new AgentRecommendation(Name, "No alternate active driver is assigned to the affected trip's centre.", [], ["Assign the recovery driver to the same centre record used by the affected trip, then run the assessment again."]);
            return new AgentExecutionResult(unavailable,
            [new AgentToolCall(RecoveryToolName.FindConflictFreeDriver, new { context.Trip.CentreId, ProposedTime = proposedTime, context.Trip.Id }, new { CandidateDriverId = (Guid?)null, Reason = "No active alternate driver" })]);
        }

        if (vehicle is null || bay is null)
        {
            var unavailable = new AgentRecommendation(Name, "No compatible replacement vehicle and bay are available for the proposed recovery time.", [], ["An active replacement vehicle and an available departure-centre bay are required before approval."]);
            return new AgentExecutionResult(unavailable,
            [new AgentToolCall(RecoveryToolName.FindConflictFreeDriver, new { context.Trip.CentreId, ProposedTime = proposedTime, context.Trip.Id }, new { CandidateDriverId = (Guid?)null, Reason = "Vehicle or bay unavailable" })]);
        }

        var candidate = await FindAvailableDriverAsync(drivers, vehicle.Id, bay.Id, proposedTime, duration, context.Trip.Id, cancellationToken);
        if (candidate is null)
        {
            var unavailable = new AgentRecommendation(Name, "No alternate active driver is currently available for the proposed recovery time.", [], ["All alternate drivers are busy or no compatible replacement vehicle and bay are available; a dispatcher must assign an eligible combination before approval."]);
            return new AgentExecutionResult(unavailable,
            [new AgentToolCall(RecoveryToolName.FindConflictFreeDriver, new { context.Trip.CentreId, ProposedTime = proposedTime, context.Trip.Id }, new { CandidateDriverId = (Guid?)null, CheckedDrivers = drivers.Count })]);
        }

        var recommendation = new AgentRecommendation(
            Name,
            $"Assign {candidate.FullName} to the recovered departure.",
            ["Driver is active and belongs to the affected trip's centre."],
            [],
            DriverId: candidate.Id);
        return new AgentExecutionResult(recommendation,
        [new AgentToolCall(RecoveryToolName.FindConflictFreeDriver, new { context.Trip.CentreId, ProposedTime = proposedTime, context.Trip.Id }, new { CandidateDriverId = candidate.Id, candidate.FullName, CheckedDrivers = drivers.Count })]);
    }

    private async Task<api.Models.Driver?> FindAvailableDriverAsync(IEnumerable<api.Models.Driver> drivers, Guid vehicleId, Guid bayId, DateTime scheduledTime, int duration, Guid tripId, CancellationToken cancellationToken)
    {
        foreach (var driver in drivers)
        {
            var conflicts = await conflictService.FindConflicts(vehicleId, driver.Id, bayId, scheduledTime, duration, tripId);
            if (conflicts.Count == 0) return driver;
        }
        return null;
    }
}
