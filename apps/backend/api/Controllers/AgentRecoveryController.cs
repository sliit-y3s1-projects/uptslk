using api.Common.Constants;
using api.DTO.AgentRecovery;
using api.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace api.Controllers;

[ApiController]
[Route("api/v1/agent-recovery")]
[Authorize(Roles = AppRoles.OperationsStaff)]
public class AgentRecoveryController(IAgentRecoveryService recovery) : ControllerBase
{
    [HttpGet("workflows")]
    public async Task<ActionResult<IReadOnlyList<RecoveryWorkflowListItemResponse>>> List([FromQuery] RecoveryWorkflowFilter filter, CancellationToken cancellationToken) =>
        Ok(await recovery.ListAsync(filter, cancellationToken));

    [HttpGet("workflows/{workflowId:guid}")]
    public async Task<ActionResult<RecoveryWorkflowResponse>> Get(Guid workflowId, CancellationToken cancellationToken) =>
        Ok(await recovery.GetAsync(workflowId, cancellationToken));

    [HttpPost("workflows")]
    [EnableRateLimiting(RateLimitPolicies.AgentRecoveryStart)]
    public async Task<ActionResult<RecoveryWorkflowStatusResponse>> Start(StartRecoveryWorkflowRequest request, CancellationToken cancellationToken)
    {
        var started = await recovery.StartAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Get), new { workflowId = started.Id }, started);
    }

    [HttpPost("workflows/{workflowId:guid}/approval")]
    [Authorize(Roles = AppRoles.Managers)]
    [EnableRateLimiting(RateLimitPolicies.AgentRecoveryApproval)]
    public async Task<ActionResult<RecoveryWorkflowStatusResponse>> Decide(Guid workflowId, DecideRecoveryApprovalRequest request, CancellationToken cancellationToken) =>
        Ok(await recovery.DecideAsync(workflowId, request, cancellationToken));
}
