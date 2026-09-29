using api.Data;
using api.Enums;
using api.Models;

namespace api.Services.AgentRecovery;

public sealed class RecoveryActionExecutor(
    AppDbContext db,
    PassengerNotificationService passengerNotifications)
{
    public async Task ExecuteAsync(
        AgentWorkflow workflow,
        ApprovalRequest approval,
        RecoveryProposal proposal,
        CancellationToken cancellationToken)
    {
        if (approval.Decision != ApprovalDecision.Approved)
            throw new InvalidOperationException("Only an approved recovery proposal can be applied.");
        if (workflow.Status != WorkflowStatus.PausedForApproval)
            throw new InvalidOperationException("The recovery workflow is not waiting for approval.");

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        workflow.Trip.VehicleId = proposal.VehicleId;
        workflow.Trip.DriverId = proposal.DriverId;
        workflow.Trip.BayId = proposal.BayId;
        workflow.Trip.ScheduledTime = proposal.ScheduledTime;
        workflow.Trip.Status = TripStatus.Delayed;
        workflow.Trip.Notes = $"Recovered by approved agent workflow {workflow.Id:N}.";
        workflow.Trip.UpdatedAt = DateTime.UtcNow;

        workflow.Incident.Status = IncidentStatus.Resolved;
        workflow.Incident.ResolvedAt = DateTime.UtcNow;
        workflow.Incident.UpdatedAt = DateTime.UtcNow;

        await passengerNotifications.PrepareApprovedRecoveryNotificationsAsync(
            workflow,
            proposal,
            cancellationToken);

        approval.AppliedAt = DateTime.UtcNow;
        workflow.Status = WorkflowStatus.Completed;
        workflow.CompletedAt = DateTime.UtcNow;
        workflow.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
