using System.Security.Claims;
using System.Text.Json;
using api.Data;
using api.DTOs;
using api.Enums;
using api.Services.AgentRecovery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[ApiController]
[Route("api/v1/agent-recovery")]
[Authorize(Roles = "Admin,CentreManager,Dispatcher,FleetOfficer")]
public class AgentRecoveryController(AppDbContext db, RecoveryWorkflowService recoveryWorkflows) : ControllerBase
{
    [HttpGet("workflows")]
    public async Task<IActionResult> List([FromQuery] Guid? centreId, [FromQuery] WorkflowStatus? status, CancellationToken cancellationToken)
    {
        var query = db.AgentWorkflows.AsNoTracking().Include(workflow => workflow.Incident).Include(workflow => workflow.Trip).ThenInclude(trip => trip.Route).Include(workflow => workflow.ApprovalRequests).AsQueryable();
        if (centreId.HasValue) query = query.Where(workflow => workflow.CentreId == centreId.Value);
        if (status.HasValue) query = query.Where(workflow => workflow.Status == status.Value);
        var workflows = await query.OrderByDescending(workflow => workflow.CreatedAt).ToListAsync(cancellationToken);
        return Ok(workflows.Select(ToListItem));
    }

    [HttpGet("workflows/{workflowId:guid}")]
    public async Task<IActionResult> Get(Guid workflowId, CancellationToken cancellationToken)
    {
        var workflow = await db.AgentWorkflows.AsNoTracking()
            .Include(item => item.Incident)
            .Include(item => item.Trip).ThenInclude(trip => trip.Route)
            .Include(item => item.Trip).ThenInclude(trip => trip.Vehicle)
            .Include(item => item.Trip).ThenInclude(trip => trip.Driver)
            .Include(item => item.Trip).ThenInclude(trip => trip.Bay)
            .Include(item => item.Steps)
            .Include(item => item.ApprovalRequests).ThenInclude(approval => approval.ReviewedBy)
            .SingleOrDefaultAsync(item => item.Id == workflowId, cancellationToken);
        if (workflow is null) return NotFound();
        return Ok(new
        {
            Summary = ToListItem(workflow),
            Trip = new { workflow.Trip.Id, workflow.Trip.ScheduledTime, workflow.Trip.Status, Route = workflow.Trip.Route.RouteNumber, workflow.Trip.Route.Name, Vehicle = workflow.Trip.Vehicle.PlateNumber, Driver = workflow.Trip.Driver.FullName, Bay = workflow.Trip.Bay.Code },
            Plan = ReadJson(workflow.PlanJson),
            ValidationResults = ReadJson(workflow.ValidationJson),
            Steps = workflow.Steps.OrderBy(step => step.CreatedAt).Select(step => new { step.Id, step.AgentName, step.Status, Input = ReadJson(step.InputJson), Output = ReadJson(step.OutputJson), ToolCalls = ReadJson(step.ToolCallsJson), step.Error, step.RetryCount, step.DurationMs, step.CreatedAt }),
            Approvals = workflow.ApprovalRequests.OrderByDescending(approval => approval.CreatedAt).Select(approval => new { approval.Id, approval.Reason, approval.Decision, approval.DecisionNote, ReviewedBy = approval.ReviewedBy == null ? null : approval.ReviewedBy.Name, approval.DecidedAt, approval.AppliedAt, approval.CreatedAt })
        });
    }

    [HttpPost("workflows")]
    public async Task<IActionResult> Start(StartRecoveryWorkflowRequest request, CancellationToken cancellationToken)
    {
        var (workflow, error) = await recoveryWorkflows.StartAsync(request.IncidentId, request.Objective, cancellationToken);
        if (workflow is null) return BadRequest(new { error });
        return CreatedAtAction(nameof(Get), new { workflowId = workflow.Id }, new { workflow.Id, workflow.Status, workflow.FailureReason });
    }

    [HttpPost("workflows/{workflowId:guid}/approval")]
    [Authorize(Roles = "Admin,CentreManager")]
    public async Task<IActionResult> Decide(Guid workflowId, DecideRecoveryApprovalRequest request, CancellationToken cancellationToken)
    {
        if (request.Decision == ApprovalDecision.Pending) return BadRequest(new { error = "Choose Approve or Reject." });
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var reviewerId = Guid.TryParse(userId, out var parsedUserId) ? parsedUserId : (Guid?)null;
        var (workflow, error) = await recoveryWorkflows.DecideAsync(workflowId, request.Decision, reviewerId, request.Note, cancellationToken);
        if (workflow is null) return BadRequest(new { error });
        return Ok(new { workflow.Id, workflow.Status, workflow.FailureReason });
    }

    private static object ToListItem(api.Models.AgentWorkflow workflow) => new
    {
        workflow.Id,
        workflow.CentreId,
        workflow.IncidentId,
        workflow.TripId,
        workflow.Objective,
        workflow.Status,
        workflow.FailureReason,
        Incident = new { workflow.Incident.Title, workflow.Incident.Type, workflow.Incident.Severity },
        Trip = new { Route = workflow.Trip.Route.RouteNumber, workflow.Trip.ScheduledTime },
        PendingApproval = workflow.ApprovalRequests.Any(approval => approval.Decision == ApprovalDecision.Pending),
        workflow.CreatedAt,
        workflow.UpdatedAt,
        workflow.CompletedAt
    };

    private static object? ReadJson(string json)
    {
        try { return JsonSerializer.Deserialize<JsonElement>(json); }
        catch (JsonException) { return null; }
    }
}
