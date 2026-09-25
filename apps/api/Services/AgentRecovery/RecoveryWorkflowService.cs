using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using api.Data;
using api.Enums;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services.AgentRecovery;

public sealed class RecoveryWorkflowService(AppDbContext db, IEnumerable<IRecoveryAgent> agents, TripConflictService conflictService)
{
    private const int AgentTimeoutSeconds = 5;
    private const int MaxAgentRetries = 1;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

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
            Objective = string.IsNullOrWhiteSpace(objective) ? $"Recover {incident.Trip.Route.RouteNumber} after {incident.Type.ToString().ToLowerInvariant()} incident: {incident.Title}" : objective.Trim(),
            PlanJson = JsonSerializer.Serialize(CreatePlan(), JsonOptions)
        };
        db.AgentWorkflows.Add(workflow);
        await db.SaveChangesAsync(cancellationToken);

        var context = new RecoveryContext(workflow, incident.Trip, affectedPassengers);
        var plan = CreatePlan();
        var recommendations = new List<AgentRecommendation>();
        foreach (var agent in agents)
        {
            MarkPlanStep(plan, agent.Name, "Running");
            var result = await RunAgentWithPolicyAsync(agent, context, cancellationToken);
            if (result.Execution is not null)
            {
                recommendations.Add(result.Execution.Recommendation);
                MarkPlanStep(plan, agent.Name, "Completed");
                db.AgentSteps.Add(new AgentStep
                {
                    WorkflowId = workflow.Id,
                    AgentName = agent.Name,
                    InputJson = JsonSerializer.Serialize(new { incident.Id, incident.TripId, affectedPassengers }, JsonOptions),
                    OutputJson = JsonSerializer.Serialize(result.Execution.Recommendation, JsonOptions),
                    ToolCallsJson = JsonSerializer.Serialize(result.Execution.ToolCalls, JsonOptions),
                    RetryCount = result.RetryCount,
                    DurationMs = result.DurationMs
                });
            }
            else
            {
                MarkPlanStep(plan, agent.Name, "Failed");
                db.AgentSteps.Add(new AgentStep
                {
                    WorkflowId = workflow.Id,
                    AgentName = agent.Name,
                    InputJson = JsonSerializer.Serialize(new { incident.Id, incident.TripId, affectedPassengers }, JsonOptions),
                    OutputJson = "{}",
                    ToolCallsJson = "[]",
                    Status = "Failed",
                    Error = result.Error ?? "Agent execution failed.",
                    RetryCount = result.RetryCount,
                    DurationMs = result.DurationMs
                });
            }
            workflow.PlanJson = JsonSerializer.Serialize(plan, JsonOptions);
            workflow.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        workflow.PlanJson = JsonSerializer.Serialize(plan, JsonOptions);

        var network = recommendations.SingleOrDefault(item => item.AgentName == "Network Continuity Agent");
        var fleet = recommendations.SingleOrDefault(item => item.AgentName == "Fleet Readiness Agent");
        var dispatch = recommendations.SingleOrDefault(item => item.AgentName == "Dispatch Recovery Agent");
        var warnings = recommendations.SelectMany(item => item.Warnings).ToList();
        if (fleet?.VehicleId is null || dispatch?.DriverId is null || network?.BayId is null || network.ScheduledTime is null || fleet.Capacity < affectedPassengers)
        {
            MarkPlanStep(plan, "Safety Validation", "Skipped");
            MarkPlanStep(plan, "Manager Approval", "Blocked");
            workflow.PlanJson = JsonSerializer.Serialize(plan, JsonOptions);
            workflow.Status = WorkflowStatus.Failed;
            workflow.FailureReason = fleet?.Capacity < affectedPassengers
                ? "No proposed replacement vehicle can carry all affected passengers."
                : dispatch?.DriverId is null
                    ? "No conflict-free alternate driver is available for the proposed recovery time."
                    : fleet?.VehicleId is null
                        ? "No active replacement vehicle is available at this centre."
                        : network?.BayId is null
                            ? "No available replacement bay is available at the departure centre."
                            : "The agents could not produce a safe recovery combination.";
            workflow.UpdatedAt = DateTime.UtcNow;
            workflow.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return (workflow, null);
        }

        var proposal = new RecoveryProposal(fleet.VehicleId.Value, dispatch.DriverId.Value, network.BayId.Value, network.ScheduledTime.Value, affectedPassengers, warnings);
        MarkPlanStep(plan, "Safety Validation", "Running");
        var validationResults = await ValidateProposalAsync(incident.Trip, proposal, "Pre-approval", cancellationToken);
        workflow.ValidationJson = JsonSerializer.Serialize(validationResults, JsonOptions);
        var validationErrors = validationResults.Where(result => !result.Passed).Select(result => result.Detail).ToList();
        if (validationErrors.Count > 0)
        {
            MarkPlanStep(plan, "Safety Validation", "Failed");
            workflow.PlanJson = JsonSerializer.Serialize(plan, JsonOptions);
            workflow.Status = WorkflowStatus.Failed;
            workflow.FailureReason = string.Join(" ", validationErrors);
            workflow.ProposalJson = JsonSerializer.Serialize(proposal, JsonOptions);
            workflow.UpdatedAt = DateTime.UtcNow;
            workflow.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return (workflow, null);
        }

        MarkPlanStep(plan, "Safety Validation", "Completed");
        MarkPlanStep(plan, "Manager Approval", "Pending");
        workflow.PlanJson = JsonSerializer.Serialize(plan, JsonOptions);
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
            MarkPlanStep(workflow, "Manager Approval", "Rejected");
            workflow.Status = WorkflowStatus.Failed;
            workflow.FailureReason = "Recovery proposal rejected by the approving manager.";
            workflow.CompletedAt = DateTime.UtcNow;
            workflow.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return (workflow, null);
        }

        var proposal = JsonSerializer.Deserialize<RecoveryProposal>(workflow.ProposalJson ?? "", JsonOptions);
        if (proposal is null) return (null, "The approved workflow has no valid recovery proposal.");
        var approvalValidation = await ValidateProposalAsync(workflow.Trip, proposal, "Approval", cancellationToken);
        AppendValidationResults(workflow, approvalValidation);
        var validationErrors = approvalValidation.Where(result => !result.Passed).Select(result => result.Detail).ToList();
        if (validationErrors.Count > 0)
        {
            MarkPlanStep(workflow, "Manager Approval", "Blocked");
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
        MarkPlanStep(workflow, "Manager Approval", "Completed");
        workflow.Status = WorkflowStatus.Completed;
        workflow.CompletedAt = DateTime.UtcNow;
        workflow.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return (workflow, null);
    }

    private async Task<IReadOnlyList<ValidationResult>> ValidateProposalAsync(Trip trip, RecoveryProposal proposal, string phase, CancellationToken cancellationToken)
    {
        var results = new List<ValidationResult>();
        var checkedAt = DateTime.UtcNow;
        var vehicle = await db.Vehicles.AsNoTracking().SingleOrDefaultAsync(item => item.Id == proposal.VehicleId, cancellationToken);
        var driver = await db.Drivers.AsNoTracking().SingleOrDefaultAsync(item => item.Id == proposal.DriverId, cancellationToken);
        var bay = await db.Bays.AsNoTracking().SingleOrDefaultAsync(item => item.Id == proposal.BayId, cancellationToken);
        results.Add(Check(phase, "Vehicle active at centre", vehicle is not null && vehicle.Status == VehicleStatus.Active && vehicle.CentreId == trip.CentreId, "The proposed vehicle is no longer active at this centre.", checkedAt));
        results.Add(Check(phase, "Driver active at centre", driver is not null && driver.Status == DriverStatus.Active && driver.CentreId == trip.CentreId, "The proposed driver is no longer active at this centre.", checkedAt));
        results.Add(Check(phase, "Bay available", bay is not null && bay.Status == BayStatus.Available, "The proposed bay is no longer available.", checkedAt));
        results.Add(Check(phase, "Vehicle capacity", vehicle is not null && vehicle.Capacity >= proposal.AffectedPassengers, "The proposed vehicle no longer has sufficient capacity.", checkedAt));
        if (vehicle is not null && driver is not null && bay is not null)
        {
            var conflicts = await conflictService.FindConflicts(vehicle.Id, driver.Id, bay.Id, proposal.ScheduledTime, trip.RouteDirection?.EstimatedDurationMin ?? trip.Route.EstimatedDurationMin, trip.Id);
            results.Add(Check(phase, "Vehicle, driver and bay conflicts", conflicts.Count == 0, conflicts.Count == 0 ? "No resource conflict was found." : string.Join(" ", conflicts), checkedAt));
        }
        else results.Add(Check(phase, "Vehicle, driver and bay conflicts", false, "Conflict validation could not run because a required resource is invalid.", checkedAt));
        return results;
    }

    private static ValidationResult Check(string phase, string check, bool passed, string failedDetail, DateTime checkedAt) =>
        new(phase, check, passed, passed ? $"{check} passed." : failedDetail, checkedAt);

    private static List<RecoveryPlanStep> CreatePlan() =>
    [
        new(1, "Assess service continuity", "Network Continuity Agent", "Select a departure-centre bay and a safe revised time.", "Pending"),
        new(2, "Assess fleet readiness", "Fleet Readiness Agent", "Select an active, maintenance-safe replacement vehicle.", "Pending"),
        new(3, "Assess dispatch availability", "Dispatch Recovery Agent", "Select a conflict-free alternate driver.", "Pending"),
        new(4, "Assess passenger impact", "Passenger & Fare Impact Agent", "Determine active passenger impact and fare action.", "Pending"),
        new(5, "Run deterministic safety checks", "Safety Validation", "Validate capacity, resource status and operational conflicts.", "Pending"),
        new(6, "Request manager decision", "Manager Approval", "Pause the high-impact trip update for authorized approval.", "Pending")
    ];

    private static void MarkPlanStep(List<RecoveryPlanStep> plan, string owner, string status)
    {
        var index = plan.FindIndex(step => step.Owner == owner);
        if (index < 0) return;
        var step = plan[index];
        plan[index] = step with
        {
            Status = status,
            CompletedAt = status is "Completed" or "Failed" or "Rejected" ? DateTime.UtcNow : null
        };
    }

    private static void MarkPlanStep(AgentWorkflow workflow, string owner, string status)
    {
        var plan = DeserializePlan(workflow.PlanJson);
        MarkPlanStep(plan, owner, status);
        workflow.PlanJson = JsonSerializer.Serialize(plan, JsonOptions);
    }

    private static List<RecoveryPlanStep> DeserializePlan(string? json)
    {
        try { return JsonSerializer.Deserialize<List<RecoveryPlanStep>>(json ?? "[]", JsonOptions) ?? CreatePlan(); }
        catch (JsonException) { return CreatePlan(); }
    }

    private static void AppendValidationResults(AgentWorkflow workflow, IReadOnlyList<ValidationResult> validationResults)
    {
        List<ValidationResult> allResults;
        try { allResults = JsonSerializer.Deserialize<List<ValidationResult>>(workflow.ValidationJson, JsonOptions) ?? []; }
        catch (JsonException) { allResults = []; }
        allResults.AddRange(validationResults);
        workflow.ValidationJson = JsonSerializer.Serialize(allResults, JsonOptions);
    }

    private static void ValidateToolCalls(IRecoveryAgent agent, AgentExecutionResult execution)
    {
        if (execution.ToolCalls.Count == 0)
            throw new InvalidOperationException($"{agent.Name} returned no auditable tool call.");
        if (execution.ToolCalls.Any(call => !agent.AllowedTools.Contains(call.Tool)))
            throw new InvalidOperationException($"{agent.Name} attempted a tool outside its allow-list.");
    }

    private static async Task<(AgentExecutionResult? Execution, string? Error, int RetryCount, int DurationMs)> RunAgentWithPolicyAsync(IRecoveryAgent agent, RecoveryContext context, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        Exception? lastError = null;
        for (var attempt = 0; attempt <= MaxAgentRetries; attempt++)
        {
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(TimeSpan.FromSeconds(AgentTimeoutSeconds));
                var execution = await agent.AnalyseAsync(context, timeout.Token);
                ValidateToolCalls(agent, execution);
                return (execution, null, attempt, (int)timer.ElapsedMilliseconds);
            }
            catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                lastError = new TimeoutException($"{agent.Name} exceeded the {AgentTimeoutSeconds}-second execution limit.", exception);
            }
            catch (Exception exception)
            {
                lastError = exception;
            }
        }
        return (null, lastError?.Message ?? "Agent execution failed.", MaxAgentRetries, (int)timer.ElapsedMilliseconds);
    }
}
