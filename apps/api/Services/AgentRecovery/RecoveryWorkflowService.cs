using System.Diagnostics;
using System.Text.Json;
using api.Data;
using api.Enums;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services.AgentRecovery;

public sealed class RecoveryWorkflowService(AppDbContext db, IEnumerable<IRecoveryAgent> agents, TripConflictService conflictService)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<(AgentWorkflow? Workflow, string? Error)> StartAsync(Guid incidentId, string? objective, CancellationToken cancellationToken)
    {
        var incident = await db.Incidents
            .Include(item => item.Trip).ThenInclude(trip => trip!.Route)
            .Include(item => item.Trip).ThenInclude(trip => trip!.RouteDirection).ThenInclude(direction => direction!.StartCentre)
            .Include(item => item.Trip).ThenInclude(trip => trip!.RouteDirection).ThenInclude(direction => direction!.EndCentre)
            .SingleOrDefaultAsync(item => item.Id == incidentId, cancellationToken);
        if (incident is null) return (null, "The incident does not exist.");
        if (incident.Trip is null) return (null, "Recovery requires an incident linked to a scheduled trip.");
        if (incident.Trip.Status is TripStatus.Completed or TripStatus.Cancelled) return (null, "Completed or cancelled trips cannot enter recovery.");
        if (await db.AgentWorkflows.AnyAsync(workflow => workflow.IncidentId == incidentId && (workflow.Status == WorkflowStatus.Running || workflow.Status == WorkflowStatus.PausedForApproval), cancellationToken))
            return (null, "This incident already has an active recovery workflow.");

        var affectedPassengers = await db.Bookings
            .Where(booking => booking.TripId == incident.TripId && (booking.Status == BookingStatus.Pending || booking.Status == BookingStatus.Confirmed))
            .SumAsync(booking => (int?)booking.PassengerCount, cancellationToken) ?? 0;
        var workflow = new AgentWorkflow
        {
            CentreId = incident.CentreId,
            IncidentId = incident.Id,
            TripId = incident.TripId!.Value,
            Objective = string.IsNullOrWhiteSpace(objective) ? $"Recover {incident.Trip.Route.RouteNumber} after {incident.Type.ToString().ToLowerInvariant()} incident: {incident.Title}" : objective.Trim()
        };
        db.AgentWorkflows.Add(workflow);
        await db.SaveChangesAsync(cancellationToken);

        var context = new RecoveryContext(workflow, incident.Trip, affectedPassengers);
        var recommendations = new List<AgentRecommendation>();
        foreach (var agent in agents)
        {
            var timer = Stopwatch.StartNew();
            try
            {
                var recommendation = await agent.AnalyseAsync(context, cancellationToken);
                recommendations.Add(recommendation);
                db.AgentSteps.Add(new AgentStep { WorkflowId = workflow.Id, AgentName = agent.Name, InputJson = JsonSerializer.Serialize(new { incident.Id, incident.TripId, affectedPassengers }, JsonOptions), OutputJson = JsonSerializer.Serialize(recommendation, JsonOptions), DurationMs = (int)timer.ElapsedMilliseconds });
            }
            catch (Exception exception)
            {
                db.AgentSteps.Add(new AgentStep { WorkflowId = workflow.Id, AgentName = agent.Name, InputJson = JsonSerializer.Serialize(new { incident.Id, incident.TripId, affectedPassengers }, JsonOptions), OutputJson = "{}", Status = "Failed", Error = exception.Message, DurationMs = (int)timer.ElapsedMilliseconds });
            }
        }

        var network = recommendations.SingleOrDefault(item => item.AgentName == "Network Continuity Agent");
        var fleet = recommendations.SingleOrDefault(item => item.AgentName == "Fleet Readiness Agent");
        var dispatch = recommendations.SingleOrDefault(item => item.AgentName == "Dispatch Recovery Agent");
        var warnings = recommendations.SelectMany(item => item.Warnings).ToList();
        if (fleet?.VehicleId is null || dispatch?.DriverId is null || network?.BayId is null || network.ScheduledTime is null || fleet.Capacity < affectedPassengers)
        {
            workflow.Status = WorkflowStatus.Failed;
            workflow.FailureReason = fleet?.Capacity < affectedPassengers ? "No proposed replacement vehicle can carry all affected passengers." : "The agents could not produce a safe bus, driver, bay, and time combination.";
            workflow.UpdatedAt = DateTime.UtcNow;
            workflow.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return (workflow, null);
        }

        var proposal = new RecoveryProposal(fleet.VehicleId.Value, dispatch.DriverId.Value, network.BayId.Value, network.ScheduledTime.Value, affectedPassengers, warnings);
        var validationErrors = await ValidateProposalAsync(incident.Trip, proposal, cancellationToken);
        if (validationErrors.Count > 0)
        {
            workflow.Status = WorkflowStatus.Failed;
            workflow.FailureReason = string.Join(" ", validationErrors);
            workflow.ProposalJson = JsonSerializer.Serialize(proposal, JsonOptions);
            workflow.UpdatedAt = DateTime.UtcNow;
            workflow.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return (workflow, null);
        }

        workflow.ProposalJson = JsonSerializer.Serialize(proposal, JsonOptions);
        workflow.Status = WorkflowStatus.PausedForApproval;
        workflow.UpdatedAt = DateTime.UtcNow;
        db.ApprovalRequests.Add(new ApprovalRequest { WorkflowId = workflow.Id, Reason = "Approve the validated recovery proposal before any dispatch assignment or passenger notification is applied." });
        incident.Status = IncidentStatus.InProgress;
        incident.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return (workflow, null);
    }

    public async Task<(AgentWorkflow? Workflow, string? Error)> DecideAsync(Guid workflowId, ApprovalDecision decision, Guid? reviewerId, string? note, CancellationToken cancellationToken)
    {
        var workflow = await db.AgentWorkflows.Include(item => item.ApprovalRequests)
            .Include(item => item.Trip).ThenInclude(trip => trip.Route)
            .Include(item => item.Trip).ThenInclude(trip => trip.RouteDirection)
            .Include(item => item.Incident)
            .SingleOrDefaultAsync(item => item.Id == workflowId, cancellationToken);
        if (workflow is null) return (null, "The recovery workflow does not exist.");
        var approval = workflow.ApprovalRequests.OrderByDescending(item => item.CreatedAt).FirstOrDefault(item => item.Decision == ApprovalDecision.Pending);
        if (approval is null || workflow.Status != WorkflowStatus.PausedForApproval) return (null, "This workflow is not waiting for an approval decision.");

        approval.Decision = decision;
        approval.ReviewedById = reviewerId;
        approval.DecisionNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        approval.DecidedAt = DateTime.UtcNow;
        if (decision == ApprovalDecision.Rejected)
        {
            workflow.Status = WorkflowStatus.Failed;
            workflow.FailureReason = "Recovery proposal rejected by the approving manager.";
            workflow.CompletedAt = DateTime.UtcNow;
            workflow.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return (workflow, null);
        }

        var proposal = JsonSerializer.Deserialize<RecoveryProposal>(workflow.ProposalJson ?? "", JsonOptions);
        if (proposal is null) return (null, "The approved workflow has no valid recovery proposal.");
        var validationErrors = await ValidateProposalAsync(workflow.Trip, proposal, cancellationToken);
        if (validationErrors.Count > 0)
        {
            workflow.Status = WorkflowStatus.Failed;
            workflow.FailureReason = string.Join(" ", validationErrors);
            workflow.CompletedAt = DateTime.UtcNow;
            workflow.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return (workflow, null);
        }

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
        approval.AppliedAt = DateTime.UtcNow;
        workflow.Status = WorkflowStatus.Completed;
        workflow.CompletedAt = DateTime.UtcNow;
        workflow.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return (workflow, null);
    }

    private async Task<IReadOnlyList<string>> ValidateProposalAsync(Trip trip, RecoveryProposal proposal, CancellationToken cancellationToken)
    {
        var errors = new List<string>();
        var vehicle = await db.Vehicles.AsNoTracking().SingleOrDefaultAsync(item => item.Id == proposal.VehicleId, cancellationToken);
        var driver = await db.Drivers.AsNoTracking().SingleOrDefaultAsync(item => item.Id == proposal.DriverId, cancellationToken);
        var bay = await db.Bays.AsNoTracking().SingleOrDefaultAsync(item => item.Id == proposal.BayId, cancellationToken);
        if (vehicle is null || vehicle.Status != VehicleStatus.Active || vehicle.CentreId != trip.CentreId) errors.Add("The proposed vehicle is no longer active at this centre.");
        if (driver is null || driver.Status != DriverStatus.Active || driver.CentreId != trip.CentreId) errors.Add("The proposed driver is no longer active at this centre.");
        if (bay is null || bay.Status != BayStatus.Available) errors.Add("The proposed bay is no longer available.");
        if (vehicle is not null && vehicle.Capacity < proposal.AffectedPassengers) errors.Add("The proposed vehicle no longer has sufficient capacity.");
        if (vehicle is not null && driver is not null && bay is not null)
            errors.AddRange(await conflictService.FindConflicts(vehicle.Id, driver.Id, bay.Id, proposal.ScheduledTime, trip.RouteDirection?.EstimatedDurationMin ?? trip.Route.EstimatedDurationMin, trip.Id));
        return errors;
    }
}
