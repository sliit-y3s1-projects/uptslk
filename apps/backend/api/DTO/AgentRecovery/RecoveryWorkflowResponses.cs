using System.Text.Json;
using api.Entities;
using api.Entities.Enums;

namespace api.DTO.AgentRecovery;

public sealed record RecoveryWorkflowListItemResponse(
    Guid Id,
    Guid CentreId,
    Guid IncidentId,
    Guid TripId,
    string Objective,
    WorkflowStatus Status,
    string PlanningMode,
    string? ModelName,
    string? FailureReason,
    WorkflowIncidentSummary Incident,
    WorkflowTripSummary Trip,
    bool PendingApproval,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? CompletedAt)
{
    public static RecoveryWorkflowListItemResponse From(AgentWorkflow workflow) => new(
        workflow.Id,
        workflow.CentreId,
        workflow.IncidentId,
        workflow.TripId,
        workflow.Objective,
        workflow.Status,
        workflow.PlanningMode,
        workflow.ModelName,
        workflow.FailureReason,
        new WorkflowIncidentSummary(workflow.Incident.Title, workflow.Incident.Type, workflow.Incident.Severity),
        new WorkflowTripSummary(workflow.Trip.Route.RouteNumber, workflow.Trip.ScheduledTime),
        workflow.ApprovalRequests.Any(approval => approval.Decision == ApprovalDecision.Pending),
        workflow.CreatedAt,
        workflow.UpdatedAt,
        workflow.CompletedAt);
}

public sealed record WorkflowIncidentSummary(string Title, IncidentType Type, IncidentSeverity Severity);

public sealed record WorkflowTripSummary(string Route, DateTime ScheduledTime);

// Everything the recovery screen shows about one workflow. Stored JSON is passed through as JSON.
public sealed record RecoveryWorkflowResponse(
    RecoveryWorkflowListItemResponse Summary,
    WorkflowTripDetail Trip,
    WorkflowPlanning Planning,
    JsonElement? Plan,
    JsonElement? ValidationResults,
    IEnumerable<WorkflowStepResponse> Steps,
    IEnumerable<WorkflowApprovalResponse> Approvals,
    WorkflowNotificationSummary Notifications)
{
    public static RecoveryWorkflowResponse From(AgentWorkflow workflow)
    {
        var trip = workflow.Trip;
        var notifications = workflow.PassengerNotifications;
        return new RecoveryWorkflowResponse(
            RecoveryWorkflowListItemResponse.From(workflow),
            new WorkflowTripDetail(trip.Id, trip.ScheduledTime, trip.Status, trip.Route.RouteNumber, trip.Route.Name, trip.Vehicle.PlateNumber, trip.Driver.FullName, trip.Bay.Code),
            new WorkflowPlanning(
                workflow.PlanningMode,
                workflow.ModelProvider,
                workflow.ModelName,
                workflow.PromptVersion,
                workflow.PlanningDurationMs,
                workflow.PromptTokenCount,
                workflow.OutputTokenCount,
                workflow.TotalTokenCount,
                workflow.PlanningFallbackReason,
                workflow.ReplanCount,
                StoredJson.Read(workflow.ReplanHistoryJson)),
            StoredJson.Read(workflow.PlanJson),
            StoredJson.Read(workflow.ValidationJson),
            workflow.Steps.OrderBy(step => step.CreatedAt).Select(WorkflowStepResponse.From).ToList(),
            workflow.ApprovalRequests.OrderByDescending(approval => approval.CreatedAt).Select(WorkflowApprovalResponse.From).ToList(),
            new WorkflowNotificationSummary(
                notifications.Count,
                notifications.Count(notification => notification.Status == PassengerNotificationStatus.Delivered),
                notifications.Count(notification => notification.Status == PassengerNotificationStatus.Failed),
                notifications.OrderBy(notification => notification.CreatedAt).Select(WorkflowNotificationRecord.From).ToList()));
    }
}

public sealed record WorkflowTripDetail(Guid Id, DateTime ScheduledTime, TripStatus Status, string Route, string Name, string Vehicle, string Driver, string Bay);

public sealed record WorkflowPlanning(
    string PlanningMode,
    string? ModelProvider,
    string? ModelName,
    string? PromptVersion,
    int PlanningDurationMs,
    int PromptTokenCount,
    int OutputTokenCount,
    int TotalTokenCount,
    string? PlanningFallbackReason,
    int ReplanCount,
    JsonElement? ReplanHistory);

public sealed record WorkflowStepResponse(
    Guid Id,
    string AgentName,
    string Status,
    JsonElement? Input,
    JsonElement? Output,
    JsonElement? ToolCalls,
    string? Error,
    int RetryCount,
    int DurationMs,
    DateTime CreatedAt)
{
    public static WorkflowStepResponse From(AgentStep step) => new(
        step.Id,
        step.AgentName,
        step.Status,
        StoredJson.Read(step.InputJson),
        StoredJson.Read(step.OutputJson),
        StoredJson.Read(step.ToolCallsJson),
        step.Error,
        step.RetryCount,
        step.DurationMs,
        step.CreatedAt);
}

public sealed record WorkflowApprovalResponse(
    Guid Id,
    string Reason,
    ApprovalDecision Decision,
    string? DecisionNote,
    string? ReviewedBy,
    DateTime? DecidedAt,
    DateTime? AppliedAt,
    DateTime CreatedAt)
{
    public static WorkflowApprovalResponse From(ApprovalRequest approval) => new(
        approval.Id,
        approval.Reason,
        approval.Decision,
        approval.DecisionNote,
        approval.ReviewedBy?.Name,
        approval.DecidedAt,
        approval.AppliedAt,
        approval.CreatedAt);
}

public sealed record WorkflowNotificationSummary(int Total, int Delivered, int Failed, IEnumerable<WorkflowNotificationRecord> Records);

public sealed record WorkflowNotificationRecord(
    Guid Id,
    Guid BookingId,
    PassengerNotificationChannel Channel,
    PassengerNotificationStatus Status,
    int DeliveryAttemptCount,
    string? DeliveryError,
    DateTime? DeliveredAt,
    DateTime? ReadAt,
    DateTime CreatedAt)
{
    public static WorkflowNotificationRecord From(PassengerNotification notification) => new(
        notification.Id,
        notification.BookingId,
        notification.Channel,
        notification.Status,
        notification.DeliveryAttemptCount,
        notification.DeliveryError,
        notification.DeliveredAt,
        notification.ReadAt,
        notification.CreatedAt);
}

public sealed record RecoveryWorkflowStatusResponse(Guid Id, WorkflowStatus Status, string? FailureReason)
{
    public static RecoveryWorkflowStatusResponse From(AgentWorkflow workflow) => new(workflow.Id, workflow.Status, workflow.FailureReason);
}

internal static class StoredJson
{
    // Returns null instead of failing when a stored value is not valid JSON.
    public static JsonElement? Read(string json)
    {
        try { return JsonSerializer.Deserialize<JsonElement>(json); }
        catch (JsonException) { return null; }
    }
}
