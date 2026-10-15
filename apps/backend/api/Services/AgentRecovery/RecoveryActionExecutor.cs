using api.Entities;
using api.Entities.Enums;
using api.Interfaces.Repositories;
using api.Interfaces.Services;

namespace api.Services.AgentRecovery;

// Applies an approved recovery proposal to the trip, closes the incident and notifies passengers.
public sealed class RecoveryActionExecutor(
    IUnitOfWork unitOfWork,
    PassengerNotificationService passengerNotifications,
    IMobileNotificationService notifications)
{
    public async Task ExecuteAsync(
        AgentWorkflow workflow,
        ApprovalRequest approval,
        RecoveryProposal proposal,
        CancellationToken cancellationToken)
    {
        // Nothing changes without a manager's approval.
        if (approval.Decision != ApprovalDecision.Approved)
            throw new InvalidOperationException("Only an approved recovery proposal can be applied.");
        if (workflow.Status != WorkflowStatus.PausedForApproval)
            throw new InvalidOperationException("The recovery workflow is not waiting for approval.");

        // The trip, incident, notices and workflow are all saved together or not at all.
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var before = TripAssignment.Of(workflow.Trip);
        workflow.Trip.VehicleId = proposal.VehicleId;
        workflow.Trip.DriverId = proposal.DriverId;
        workflow.Trip.BayId = proposal.BayId;
        workflow.Trip.ScheduledTime = proposal.ScheduledTime;
        workflow.Trip.Status = TripStatus.Delayed;
        workflow.Trip.Notes = $"Recovered by approved agent workflow {workflow.Id:N}.";
        workflow.Trip.UpdatedAt = DateTime.UtcNow;
        await notifications.TripChangedAsync(workflow.Trip, before, cancellationToken);

        workflow.Incident.Status = IncidentStatus.Resolved;
        workflow.Incident.ResolvedAt = DateTime.UtcNow;
        workflow.Incident.UpdatedAt = DateTime.UtcNow;

        await passengerNotifications.PrepareApprovedRecoveryNotificationsAsync(workflow, proposal, cancellationToken);

        approval.AppliedAt = DateTime.UtcNow;
        workflow.Status = WorkflowStatus.Completed;
        workflow.CompletedAt = DateTime.UtcNow;
        workflow.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
