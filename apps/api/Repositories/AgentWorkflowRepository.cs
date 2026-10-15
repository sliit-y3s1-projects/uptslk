using api.Data;
using api.DTO.AgentRecovery;
using api.Entities;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class AgentWorkflowRepository(AppDbContext db) : Repository<AgentWorkflow>(db), IAgentWorkflowRepository
{
    public async Task<List<AgentWorkflow>> SearchAsync(RecoveryWorkflowFilter filter, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking()
            .Include(workflow => workflow.Incident)
            .Include(workflow => workflow.Trip).ThenInclude(trip => trip.Route)
            .Include(workflow => workflow.ApprovalRequests)
            .AsQueryable();
        if (filter.CentreId.HasValue) query = query.Where(workflow => workflow.CentreId == filter.CentreId.Value);
        if (filter.Status.HasValue) query = query.Where(workflow => workflow.Status == filter.Status.Value);
        return await query.OrderByDescending(workflow => workflow.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<AgentWorkflow?> GetWithDetailsAsync(Guid id, Guid? centreId, CancellationToken cancellationToken = default)
    {
        var query = Set.AsNoTracking()
            .Include(workflow => workflow.Incident)
            .Include(workflow => workflow.Trip).ThenInclude(trip => trip.Route)
            .Include(workflow => workflow.Trip).ThenInclude(trip => trip.Vehicle)
            .Include(workflow => workflow.Trip).ThenInclude(trip => trip.Driver)
            .Include(workflow => workflow.Trip).ThenInclude(trip => trip.Bay)
            .Include(workflow => workflow.Steps)
            .Include(workflow => workflow.ApprovalRequests).ThenInclude(approval => approval.ReviewedBy)
            .Include(workflow => workflow.PassengerNotifications)
            .AsQueryable();
        if (centreId.HasValue) query = query.Where(workflow => workflow.CentreId == centreId.Value);
        return query.SingleOrDefaultAsync(workflow => workflow.Id == id, cancellationToken);
    }

    public Task<bool> ExistsInCentreAsync(Guid id, Guid centreId, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(workflow => workflow.Id == id && workflow.CentreId == centreId, cancellationToken);
}
