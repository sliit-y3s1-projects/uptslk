using api.Common.Constants;
using api.DTO.AgentRecovery;
using api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace api.Controllers;

// The tools a coordinator uses to steer a recovery proposal before a manager decides on it.
[ApiController]
[Route("api/v1/agent-recovery/workflows/{workflowId:guid}")]
[Authorize(Roles = AppRoles.OperationsStaff)]
[EnableRateLimiting(RateLimitPolicies.AgentRecoveryCopilot)]
public class RecoveryCopilotController(IRecoveryCopilotService copilot) : ControllerBase
{
    // Fleet agent: what-if simulation

    [HttpGet("alternatives")]
    public async Task<ActionResult<RecoveryAlternativesResponse>> Alternatives(Guid workflowId, CancellationToken cancellationToken) =>
        Ok(await copilot.GetAlternativesAsync(workflowId, cancellationToken));

    [HttpPost("simulate")]
    public async Task<ActionResult<RecoveryAlternativeResponse>> Simulate(Guid workflowId, SimulateScenarioRequest request, CancellationToken cancellationToken) =>
        Ok(await copilot.SimulateAsync(workflowId, request, cancellationToken));

    [HttpPost("proposal")]
    public async Task<ActionResult<RecoveryWorkflowStatusResponse>> ApplyAlternative(Guid workflowId, SimulateScenarioRequest request, CancellationToken cancellationToken) =>
        Ok(await copilot.ApplyAlternativeAsync(workflowId, request, cancellationToken));

    // Network agent: cascading impact

    [HttpGet("impact")]
    public async Task<ActionResult<RecoveryImpactResponse>> Impact(Guid workflowId, CancellationToken cancellationToken) =>
        Ok(await copilot.GetImpactAsync(workflowId, cancellationToken));

    [HttpPost("impact/explain")]
    public async Task<ActionResult<ImpactExplanationResponse>> ExplainImpact(Guid workflowId, ExplainImpactRequest request, CancellationToken cancellationToken) =>
        Ok(await copilot.ExplainImpactAsync(workflowId, request, cancellationToken));

    [HttpPost("impact/review")]
    public async Task<ActionResult<RecoveryImpactResponse>> ReviewImpact(Guid workflowId, ReviewImpactRequest request, CancellationToken cancellationToken) =>
        Ok(await copilot.ReviewImpactAsync(workflowId, request, cancellationToken));

    // Passenger agent: message desk

    [HttpGet("messages")]
    public async Task<ActionResult<RecoveryMessagesResponse>> Messages(Guid workflowId, CancellationToken cancellationToken) =>
        Ok(await copilot.GetMessagesAsync(workflowId, cancellationToken));

    [HttpPut("messages/{segment}")]
    public async Task<ActionResult<MessageSegmentResponse>> SaveMessage(Guid workflowId, string segment, SaveMessageDraftRequest request, CancellationToken cancellationToken) =>
        Ok(await copilot.SaveMessageAsync(workflowId, segment, request, cancellationToken));

    [HttpPost("messages/{segment}/regenerate")]
    public async Task<ActionResult<MessageSegmentResponse>> RegenerateMessage(Guid workflowId, string segment, RegenerateMessageRequest request, CancellationToken cancellationToken) =>
        Ok(await copilot.RegenerateMessageAsync(workflowId, segment, request, cancellationToken));

    [HttpDelete("messages/{segment}")]
    public async Task<ActionResult<MessageSegmentResponse>> ResetMessage(Guid workflowId, string segment, CancellationToken cancellationToken) =>
        Ok(await copilot.ResetMessageAsync(workflowId, segment, cancellationToken));

    // Dispatch agent: reject, revise and remember

    [HttpPost("revise")]
    [Authorize(Roles = AppRoles.Managers)]
    public async Task<ActionResult<ReviseRecoveryResponse>> Revise(Guid workflowId, ReviseRecoveryRequest request, CancellationToken cancellationToken) =>
        Ok(await copilot.ReviseAsync(workflowId, request, cancellationToken));

    [HttpGet("lessons")]
    public async Task<ActionResult<IReadOnlyList<RecoveryLessonResponse>>> Lessons(Guid workflowId, CancellationToken cancellationToken) =>
        Ok(await copilot.GetLessonsAsync(workflowId, cancellationToken));
}
