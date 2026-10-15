using api.DTO.AgentRecovery;
using api.Entities.Enums;
using api.Exceptions;
using api.Interfaces.Repositories;
using api.Interfaces.Services;
using api.Services.AgentRecovery;
using api.Services.AgentRecovery.Planning;

namespace api.Services;

public sealed class AgentRecoveryService(
    IAgentWorkflowRepository workflows,
    IIncidentRepository incidents,
    ICurrentUserService currentUser,
    RecoveryWorkflowService recoveryWorkflows) : IAgentRecoveryService
{
    private const string WorkflowNotFound = "Recovery workflow not found.";

    public async Task<IReadOnlyList<RecoveryWorkflowListItemResponse>> ListAsync(RecoveryWorkflowFilter filter, CancellationToken cancellationToken)
    {
        var scope = RequireScope();
        if (!scope.IsAdmin && filter.CentreId.HasValue && filter.CentreId != scope.CentreId) throw new ForbiddenException();

        var effectiveFilter = new RecoveryWorkflowFilter { CentreId = scope.IsAdmin ? filter.CentreId : scope.CentreId, Status = filter.Status };
        var found = await workflows.SearchAsync(effectiveFilter, cancellationToken);
        return found.Select(RecoveryWorkflowListItemResponse.From).ToList();
    }

    public async Task<RecoveryWorkflowResponse> GetAsync(Guid workflowId, CancellationToken cancellationToken)
    {
        var scope = RequireScope();
        var workflow = await workflows.GetWithDetailsAsync(workflowId, scope.IsAdmin ? null : scope.CentreId, cancellationToken)
            ?? throw new NotFoundException(WorkflowNotFound);
        return RecoveryWorkflowResponse.From(workflow);
    }

    public async Task<RecoveryWorkflowStatusResponse> StartAsync(StartRecoveryWorkflowRequest request, CancellationToken cancellationToken)
    {
        var scope = RequireScope();
        if (!RecoveryTextValidator.TryNormalizeObjective(request.Objective, out var objective, out var validationError))
            throw new BadRequestException(validationError!);
        if (!scope.IsAdmin && !await incidents.ExistsInCentreAsync(request.IncidentId, scope.CentreId!.Value, cancellationToken))
            throw new NotFoundException("Incident not found.");

        try
        {
            var (workflow, error) = await recoveryWorkflows.StartAsync(request.IncidentId, objective, scope.CentreId, cancellationToken, request.AllowFallback);
            return workflow is null ? throw new BadRequestException(error!) : RecoveryWorkflowStatusResponse.From(workflow);
        }
        catch (RecoveryPlanningUnavailableException exception)
        {
            // The web app looks for this code to offer the safe built-in plan instead.
            throw new ServiceUnavailableException(exception.Message) { Details = { ["code"] = "ai_planning_unavailable" } };
        }
    }

    public async Task<RecoveryWorkflowStatusResponse> DecideAsync(Guid workflowId, DecideRecoveryApprovalRequest request, CancellationToken cancellationToken)
    {
        var scope = RequireScope();
        if (request.Decision == ApprovalDecision.Pending) throw new BadRequestException("Choose Approve or Reject.");
        if (!RecoveryTextValidator.TryNormalizeDecisionNote(request.Note, out var note, out var validationError))
            throw new BadRequestException(validationError!);
        if (!scope.IsAdmin && !await workflows.ExistsInCentreAsync(workflowId, scope.CentreId!.Value, cancellationToken))
            throw new NotFoundException(WorkflowNotFound);

        var (workflow, error) = await recoveryWorkflows.DecideAsync(workflowId, request.Decision, currentUser.UserId, note, scope.CentreId, cancellationToken);
        return workflow is null ? throw new BadRequestException(error!) : RecoveryWorkflowStatusResponse.From(workflow);
    }

    // Admins can see every centre. Other staff need a centre assignment.
    private RecoveryAccessScope RequireScope() =>
        RecoveryAccessScope.TryCreate(currentUser.Principal, out var scope) ? scope : throw new ForbiddenException();
}
