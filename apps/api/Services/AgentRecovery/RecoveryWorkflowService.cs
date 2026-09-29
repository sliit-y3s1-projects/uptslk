using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using api.Data;
using api.Enums;
using api.Models;
using api.Services.AgentRecovery.Planning;
using Microsoft.EntityFrameworkCore;

namespace api.Services.AgentRecovery;

public sealed class RecoveryWorkflowService(
    AppDbContext db,
    RecoveryPlanningService planningService,
    RecoveryAgentRegistry agentRegistry,
    RecoveryProposalComposer proposalComposer,
    PassengerNotificationService passengerNotifications,
    TripConflictService conflictService,
    Microsoft.Extensions.Options.IOptions<AgentAiOptions> agentAiOptions,
    ILogger<RecoveryWorkflowService> logger)
{
    private const int AgentTimeoutSeconds = 5;
    private const int MaxAgentRetries = 1;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
    private readonly AgentAiOptions _agentAiOptions = agentAiOptions.Value;

    public async Task<(AgentWorkflow? Workflow, string? Error)> StartAsync(
        Guid incidentId,
        string? objective,
        Guid? scopedCentreId,
        CancellationToken cancellationToken)
    {
        var incident = await db.Incidents
            .Include(item => item.Trip).ThenInclude(trip => trip!.Route)
            .Include(item => item.Trip).ThenInclude(trip => trip!.Vehicle)
            .Include(item => item.Trip).ThenInclude(trip => trip!.RouteDirection).ThenInclude(direction => direction!.StartCentre)
            .Include(item => item.Trip).ThenInclude(trip => trip!.RouteDirection).ThenInclude(direction => direction!.EndCentre)
            .SingleOrDefaultAsync(item => item.Id == incidentId, cancellationToken);
        if (incident is null) return (null, "The incident does not exist.");
        if (scopedCentreId.HasValue && incident.CentreId != scopedCentreId.Value)
            return (null, "The incident does not exist.");
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
            PlanJson = "[]"
        };
        db.AgentWorkflows.Add(workflow);
        await db.SaveChangesAsync(cancellationToken);

        var planningInput = CreatePlanningInput(workflow, incident, affectedPassengers, agentRegistry.Capabilities);
        RecoveryPlanningResult planningResult;
        try
        {
            planningResult = await planningService.CreatePlanAsync(planningInput, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            workflow.Status = WorkflowStatus.Failed;
            workflow.FailureReason = "Recovery planning was cancelled before a safe plan was created.";
            workflow.UpdatedAt = DateTime.UtcNow;
            workflow.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        ApplyPlanningResult(workflow, planningResult);
        var execution = await ExecutePlanWithCancellationCleanupAsync(
            workflow,
            incident,
            affectedPassengers,
            planningResult.Plan,
            0,
            cancellationToken);
        var composition = proposalComposer.Compose(
            incident.Trip,
            affectedPassengers,
            planningResult.Plan,
            execution.Recommendations,
            execution.Errors);
        var failures = composition.Errors.ToList();

        if (failures.Count > 0
            && planningResult.Mode == "Gemini"
            && workflow.ReplanCount < _agentAiOptions.MaxWorkflowReplans)
        {
            RecoveryPlanningResult revisedPlanningResult;
            try
            {
                revisedPlanningResult = await planningService.CreateRevisedPlanAsync(
                    planningInput,
                    failures,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                workflow.Status = WorkflowStatus.Failed;
                workflow.FailureReason = "Recovery replanning was cancelled before a safe revised plan was created.";
                workflow.UpdatedAt = DateTime.UtcNow;
                workflow.CompletedAt = DateTime.UtcNow;
                await db.SaveChangesAsync(CancellationToken.None);
                throw;
            }

            workflow.ReplanCount++;
            AppendReplanAudit(workflow, revisedPlanningResult, failures);
            ApplyReplanningResult(workflow, revisedPlanningResult);
            await db.SaveChangesAsync(cancellationToken);

            if (revisedPlanningResult.Mode == "Gemini")
            {
                planningResult = revisedPlanningResult;
                execution = await ExecutePlanWithCancellationCleanupAsync(
                    workflow,
                    incident,
                    affectedPassengers,
                    revisedPlanningResult.Plan,
                    workflow.ReplanCount,
                    cancellationToken);
                composition = proposalComposer.Compose(
                    incident.Trip,
                    affectedPassengers,
                    revisedPlanningResult.Plan,
                    execution.Recommendations,
                    execution.Errors);
                failures = composition.Errors.ToList();
            }
            else
            {
                failures =
                [
                    .. failures,
                    revisedPlanningResult.FallbackReason ?? "Gemini could not produce a valid revised plan."
                ];
            }
        }

        var plan = execution.Plan;
        if (failures.Count > 0)
        {
            MarkPlanStep(plan, "Safety Validation", "Skipped");
            MarkPlanStep(plan, "Manager Approval", "Blocked");
            workflow.PlanJson = JsonSerializer.Serialize(plan, JsonOptions);
            workflow.Status = WorkflowStatus.Failed;
            workflow.FailureReason = string.Join(" ", failures.Distinct(StringComparer.Ordinal));
            workflow.UpdatedAt = DateTime.UtcNow;
            workflow.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
            return (workflow, null);
        }

        var proposal = composition.Proposal
            ?? throw new InvalidOperationException("Proposal composition succeeded without producing a proposal.");
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

    public async Task<(AgentWorkflow? Workflow, string? Error)> DecideAsync(
        Guid workflowId,
        ApprovalDecision decision,
        Guid? reviewerId,
        string? note,
        Guid? scopedCentreId,
        CancellationToken cancellationToken)
    {
        var workflow = await db.AgentWorkflows.Include(item => item.ApprovalRequests)
            .Include(item => item.Trip).ThenInclude(trip => trip.Route)
            .Include(item => item.Trip).ThenInclude(trip => trip.RouteDirection)
            .Include(item => item.Incident)
            .SingleOrDefaultAsync(item => item.Id == workflowId, cancellationToken);
        if (workflow is null) return (null, "The recovery workflow does not exist.");
        if (scopedCentreId.HasValue && workflow.CentreId != scopedCentreId.Value)
            return (null, "The recovery workflow does not exist.");
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
        await passengerNotifications.PrepareApprovedRecoveryNotificationsAsync(
            workflow,
            proposal,
            cancellationToken);
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
        var vehicle = await db.Vehicles.AsNoTracking()
            .Include(item => item.MaintenanceRecords)
            .SingleOrDefaultAsync(item => item.Id == proposal.VehicleId, cancellationToken);
        var driver = await db.Drivers.AsNoTracking().SingleOrDefaultAsync(item => item.Id == proposal.DriverId, cancellationToken);
        var bay = await db.Bays.AsNoTracking().SingleOrDefaultAsync(item => item.Id == proposal.BayId, cancellationToken);
        results.Add(Check(phase, "Vehicle active at centre", vehicle is not null && vehicle.Status == VehicleStatus.Active && vehicle.CentreId == trip.CentreId, "The proposed vehicle is no longer active at this centre.", checkedAt));
        results.Add(Check(phase, "Vehicle maintenance clearance", vehicle is not null && !vehicle.MaintenanceRecords.Any(record => record.Status == MaintenanceStatus.InProgress), "The proposed vehicle has maintenance currently in progress.", checkedAt));
        results.Add(Check(phase, "Driver active at centre", driver is not null && driver.Status == DriverStatus.Active && driver.CentreId == trip.CentreId, "The proposed driver is no longer active at this centre.", checkedAt));
        results.Add(Check(phase, "Bay available", bay is not null && bay.Status == BayStatus.Available, "The proposed bay is no longer available.", checkedAt));
        results.Add(Check(phase, "Vehicle capacity", vehicle is not null && vehicle.Capacity >= proposal.AffectedPassengers, "The proposed vehicle no longer has sufficient capacity.", checkedAt));
        if (vehicle is not null && driver is not null && bay is not null)
        {
            var conflicts = await conflictService.FindConflicts(vehicle.Id, driver.Id, bay.Id, proposal.ScheduledTime, trip.RouteDirection?.EstimatedDurationMin ?? trip.Route.EstimatedDurationMin, trip.Id, cancellationToken);
            results.Add(Check(phase, "Vehicle, driver and bay conflicts", conflicts.Count == 0, conflicts.Count == 0 ? "No resource conflict was found." : string.Join(" ", conflicts), checkedAt));
        }
        else results.Add(Check(phase, "Vehicle, driver and bay conflicts", false, "Conflict validation could not run because a required resource is invalid.", checkedAt));
        return results;
    }

    private static ValidationResult Check(string phase, string check, bool passed, string failedDetail, DateTime checkedAt) =>
        new(phase, check, passed, passed ? $"{check} passed." : failedDetail, checkedAt);

    private async Task<PlanExecutionResult> ExecutePlanAsync(
        AgentWorkflow workflow,
        Incident incident,
        int affectedPassengers,
        RecoveryPlanDraft generatedPlan,
        int planAttempt,
        CancellationToken cancellationToken)
    {
        var plan = CreateExecutionPlan(generatedPlan, agentRegistry);
        workflow.PlanJson = JsonSerializer.Serialize(plan, JsonOptions);
        workflow.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        var recommendations = new Dictionary<RecoveryAgentId, AgentRecommendation>();
        var stepOutcomes = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        var executionErrors = new List<string>();

        foreach (var plannedStep in generatedPlan.Steps.OrderBy(step => step.Order))
        {
            var agent = agentRegistry.GetRequired(plannedStep.AgentId);
            var failedDependencies = (plannedStep.DependsOn ?? [])
                .Where(dependencyId => !stepOutcomes.GetValueOrDefault(dependencyId))
                .ToArray();
            if (failedDependencies.Length > 0)
            {
                var error = $"{agent.Name} was skipped because required steps did not complete: {string.Join(", ", failedDependencies)}.";
                executionErrors.Add(error);
                stepOutcomes[plannedStep.StepId] = false;
                MarkPlanStep(plan, agent.Name, "Skipped");
                db.AgentSteps.Add(new AgentStep
                {
                    WorkflowId = workflow.Id,
                    AgentName = agent.Name,
                    InputJson = JsonSerializer.Serialize(new { PlanAttempt = planAttempt, plannedStep.StepId, incident.Id, incident.TripId, affectedPassengers, plannedStep.DependsOn }, JsonOptions),
                    OutputJson = "{}",
                    ToolCallsJson = "[]",
                    Status = "Skipped",
                    Error = error
                });
                await PersistPlanProgressAsync(workflow, plan, cancellationToken);
                continue;
            }

            var context = new RecoveryContext(
                workflow,
                incident.Trip!,
                affectedPassengers,
                new Dictionary<RecoveryAgentId, AgentRecommendation>(recommendations));
            MarkPlanStep(plan, agent.Name, "Running");
            var result = await RunAgentWithPolicyAsync(agent, context, cancellationToken);
            if (result.Execution is not null)
            {
                recommendations[agent.Id] = result.Execution.Recommendation;
                stepOutcomes[plannedStep.StepId] = true;
                MarkPlanStep(plan, agent.Name, "Completed");
                db.AgentSteps.Add(new AgentStep
                {
                    WorkflowId = workflow.Id,
                    AgentName = agent.Name,
                    InputJson = JsonSerializer.Serialize(new { PlanAttempt = planAttempt, plannedStep.StepId, incident.Id, incident.TripId, affectedPassengers }, JsonOptions),
                    OutputJson = JsonSerializer.Serialize(result.Execution.Recommendation, JsonOptions),
                    ToolCallsJson = JsonSerializer.Serialize(result.Execution.ToolCalls, JsonOptions),
                    RetryCount = result.RetryCount,
                    DurationMs = result.DurationMs
                });
            }
            else
            {
                var error = result.Error ?? $"{agent.Name} execution failed.";
                executionErrors.Add(error);
                stepOutcomes[plannedStep.StepId] = false;
                MarkPlanStep(plan, agent.Name, "Failed");
                db.AgentSteps.Add(new AgentStep
                {
                    WorkflowId = workflow.Id,
                    AgentName = agent.Name,
                    InputJson = JsonSerializer.Serialize(new { PlanAttempt = planAttempt, plannedStep.StepId, incident.Id, incident.TripId, affectedPassengers }, JsonOptions),
                    OutputJson = "{}",
                    ToolCallsJson = "[]",
                    Status = "Failed",
                    Error = error,
                    RetryCount = result.RetryCount,
                    DurationMs = result.DurationMs
                });
            }

            await PersistPlanProgressAsync(workflow, plan, cancellationToken);
        }

        return new PlanExecutionResult(plan, recommendations, executionErrors);
    }

    private async Task<PlanExecutionResult> ExecutePlanWithCancellationCleanupAsync(
        AgentWorkflow workflow,
        Incident incident,
        int affectedPassengers,
        RecoveryPlanDraft generatedPlan,
        int planAttempt,
        CancellationToken cancellationToken)
    {
        try
        {
            return await ExecutePlanAsync(
                workflow,
                incident,
                affectedPassengers,
                generatedPlan,
                planAttempt,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            workflow.Status = WorkflowStatus.Failed;
            workflow.FailureReason = "Recovery agent execution was cancelled before the assessment completed.";
            workflow.UpdatedAt = DateTime.UtcNow;
            workflow.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task PersistPlanProgressAsync(
        AgentWorkflow workflow,
        IReadOnlyCollection<RecoveryPlanStep> plan,
        CancellationToken cancellationToken)
    {
        workflow.PlanJson = JsonSerializer.Serialize(plan, JsonOptions);
        workflow.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static RecoveryPlanningInput CreatePlanningInput(
        AgentWorkflow workflow,
        Incident incident,
        int affectedPassengers,
        IReadOnlyCollection<AgentCapability> capabilities)
    {
        var trip = incident.Trip!;
        var direction = trip.RouteDirection is null
            ? null
            : $"{trip.RouteDirection.StartCentre.Name} to {trip.RouteDirection.EndCentre.Name}";

        return new RecoveryPlanningInput(
            workflow.Id,
            RecoveryTextValidator.SanitizeForPrompt(workflow.Objective, 1000),
            new IncidentPlanningSnapshot(
                incident.Type,
                incident.Severity,
                RecoveryTextValidator.SanitizeForPrompt(incident.Title, 200),
                RecoveryTextValidator.SanitizeForPrompt(incident.Description, 2000)),
            new TripPlanningSnapshot(
                trip.Id,
                trip.CentreId,
                RecoveryTextValidator.SanitizeForPrompt(trip.Route.RouteNumber, 32),
                RecoveryTextValidator.SanitizeForPrompt(trip.Route.Name, 160),
                direction is null ? null : RecoveryTextValidator.SanitizeForPrompt(direction, 340),
                trip.ScheduledTime,
                trip.RouteDirection?.EstimatedDurationMin ?? trip.Route.EstimatedDurationMin),
            affectedPassengers,
            capabilities);
    }

    private static void ApplyPlanningResult(AgentWorkflow workflow, RecoveryPlanningResult result)
    {
        workflow.PlanningMode = result.Mode;
        workflow.ModelProvider = result.Provider;
        workflow.ModelName = result.Model;
        workflow.PromptVersion = result.PromptVersion;
        workflow.PlannerInputJson = result.InputJson;
        workflow.PlannerOutputJson = result.OutputJson;
        workflow.PlanningDurationMs = result.DurationMs;
        workflow.PromptTokenCount = result.PromptTokenCount;
        workflow.OutputTokenCount = result.OutputTokenCount;
        workflow.TotalTokenCount = result.TotalTokenCount;
        workflow.PlanningFallbackReason = result.FallbackReason;
    }

    private static void ApplyReplanningResult(AgentWorkflow workflow, RecoveryPlanningResult result)
    {
        workflow.PlanningMode = result.Mode == "Gemini" ? "GeminiReplanned" : "GeminiReplanFallback";
        workflow.ModelProvider = result.Provider;
        workflow.ModelName = result.Model;
        workflow.PromptVersion = result.PromptVersion;
        workflow.PlanningDurationMs += result.DurationMs;
        workflow.PromptTokenCount += result.PromptTokenCount;
        workflow.OutputTokenCount += result.OutputTokenCount;
        workflow.TotalTokenCount += result.TotalTokenCount;
        workflow.PlanningFallbackReason = result.FallbackReason;
        workflow.UpdatedAt = DateTime.UtcNow;
    }

    private static void AppendReplanAudit(
        AgentWorkflow workflow,
        RecoveryPlanningResult result,
        IReadOnlyCollection<string> reasons)
    {
        List<RecoveryReplanAudit> history;
        try
        {
            history = JsonSerializer.Deserialize<List<RecoveryReplanAudit>>(
                workflow.ReplanHistoryJson,
                JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            history = [];
        }

        history.Add(new RecoveryReplanAudit(
            workflow.ReplanCount,
            reasons.ToArray(),
            result.Mode,
            result.Model,
            result.Plan,
            result.DurationMs,
            result.PromptTokenCount,
            result.OutputTokenCount,
            result.TotalTokenCount,
            result.FallbackReason,
            DateTime.UtcNow));
        workflow.ReplanHistoryJson = JsonSerializer.Serialize(history, JsonOptions);
    }

    private static List<RecoveryPlanStep> CreateExecutionPlan(
        RecoveryPlanDraft generatedPlan,
        RecoveryAgentRegistry registry)
    {
        var plan = generatedPlan.Steps
            .OrderBy(step => step.Order)
            .Select(step =>
            {
                var agent = registry.GetRequired(step.AgentId);
                return new RecoveryPlanStep(
                    step.Order,
                    GetStepTitle(step.AgentId),
                    agent.Name,
                    step.Objective,
                    "Pending");
            })
            .ToList();

        var nextOrder = plan.Count == 0 ? 1 : plan.Max(step => step.Order) + 1;
        plan.Add(new RecoveryPlanStep(nextOrder, "Run deterministic safety checks", "Safety Validation", "Validate capacity, resource status and operational conflicts.", "Pending"));
        plan.Add(new RecoveryPlanStep(nextOrder + 1, "Request manager decision", "Manager Approval", "Pause the high-impact trip update for authorized approval.", "Pending"));
        return plan;
    }

    private static string GetStepTitle(RecoveryAgentId agentId) => agentId switch
    {
        RecoveryAgentId.NetworkContinuity => "Assess service continuity",
        RecoveryAgentId.FleetReadiness => "Assess fleet readiness",
        RecoveryAgentId.DispatchRecovery => "Assess dispatch availability",
        RecoveryAgentId.PassengerFareImpact => "Assess passenger impact",
        _ => throw new ArgumentOutOfRangeException(nameof(agentId), agentId, "Unsupported recovery agent.")
    };

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
        try { return JsonSerializer.Deserialize<List<RecoveryPlanStep>>(json ?? "[]", JsonOptions) ?? []; }
        catch (JsonException) { return []; }
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

    private async Task<(AgentExecutionResult? Execution, string? Error, int RetryCount, int DurationMs)> RunAgentWithPolicyAsync(IRecoveryAgent agent, RecoveryContext context, CancellationToken cancellationToken)
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
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                lastError = exception;
                logger.LogWarning(
                    exception,
                    "Recovery agent {AgentId} failed for workflow {WorkflowId} on attempt {Attempt}",
                    agent.Id,
                    context.Workflow.Id,
                    attempt + 1);
            }
        }
        var safeError = lastError is TimeoutException
            ? lastError.Message
            : $"{agent.Name} failed while executing an allow-listed recovery tool.";
        return (null, safeError, MaxAgentRetries, (int)timer.ElapsedMilliseconds);
    }

    private sealed record PlanExecutionResult(
        List<RecoveryPlanStep> Plan,
        IReadOnlyDictionary<RecoveryAgentId, AgentRecommendation> Recommendations,
        IReadOnlyCollection<string> Errors);
}
