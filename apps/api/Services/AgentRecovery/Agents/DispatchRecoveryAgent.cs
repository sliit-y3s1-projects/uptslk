using api.Data;
using api.Enums;
using Microsoft.EntityFrameworkCore;

namespace api.Services.AgentRecovery.Agents;

public sealed class DispatchRecoveryAgent(AppDbContext db) : IRecoveryAgent
{
    public string Name => "Dispatch Recovery Agent";

    public async Task<AgentRecommendation> AnalyseAsync(RecoveryContext context, CancellationToken cancellationToken)
    {
        var drivers = await db.Drivers.AsNoTracking()
            .Where(driver => driver.CentreId == context.Trip.CentreId && driver.Id != context.Trip.DriverId && driver.Status == DriverStatus.Active)
            .OrderBy(driver => driver.FullName)
            .ToListAsync(cancellationToken);
        var candidate = drivers.FirstOrDefault();
        if (candidate is null)
            return new AgentRecommendation(Name, "No alternate active driver is currently available.", [], ["A dispatcher must assign an eligible driver before the proposed recovery can be approved."]);

        return new AgentRecommendation(
            Name,
            $"Assign {candidate.FullName} to the recovered departure.",
            ["Driver is active and belongs to the affected trip's centre."],
            [],
            DriverId: candidate.Id);
    }
}
