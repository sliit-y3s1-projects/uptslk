using api.Data;
using api.DTO.AgentRecovery;
using api.Entities;
using api.Entities.Enums;
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

    public Task<bool> HasActiveForIncidentAsync(Guid incidentId, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(workflow => workflow.IncidentId == incidentId
            && (workflow.Status == WorkflowStatus.Running || workflow.Status == WorkflowStatus.PausedForApproval), cancellationToken);

    public Task<AgentWorkflow?> GetForDecisionAsync(Guid id, CancellationToken cancellationToken = default) =>
        Set.Include(workflow => workflow.ApprovalRequests)
            .Include(workflow => workflow.Trip).ThenInclude(trip => trip.Route)
            .Include(workflow => workflow.Trip).ThenInclude(trip => trip.RouteDirection)
            .Include(workflow => workflow.Incident)
            .SingleOrDefaultAsync(workflow => workflow.Id == id, cancellationToken);

    public Task<AgentWorkflow?> GetForCopilotAsync(Guid id, Guid? centreId, CancellationToken cancellationToken = default)
    {
        var query = Set.Include(workflow => workflow.ApprovalRequests)
            .Include(workflow => workflow.Incident)
            .Include(workflow => workflow.Trip).ThenInclude(trip => trip.Route)
            .Include(workflow => workflow.Trip).ThenInclude(trip => trip.RouteDirection).ThenInclude(direction => direction!.StartCentre)
            .Include(workflow => workflow.Trip).ThenInclude(trip => trip.RouteDirection).ThenInclude(direction => direction!.EndCentre)
            .Include(workflow => workflow.Trip).ThenInclude(trip => trip.Vehicle)
            .Include(workflow => workflow.Trip).ThenInclude(trip => trip.Driver)
            .Include(workflow => workflow.Trip).ThenInclude(trip => trip.Bay)
            .Include(workflow => workflow.PassengerNotifications).ThenInclude(notification => notification.Passenger)
            .AsQueryable();
        if (centreId.HasValue) query = query.Where(workflow => workflow.CentreId == centreId.Value);
        return query.SingleOrDefaultAsync(workflow => workflow.Id == id, cancellationToken);
    }

    public void AddStep(AgentStep step) => Db.AgentSteps.Add(step);

    public void AddApprovalRequest(ApprovalRequest approvalRequest) => Db.ApprovalRequests.Add(approvalRequest);

    public Task<bool> ExistsInCentreAsync(Guid id, Guid centreId, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(workflow => workflow.Id == id && workflow.CentreId == centreId, cancellationToken);
}
