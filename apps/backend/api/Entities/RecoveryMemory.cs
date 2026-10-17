using api.Entities.Enums;

namespace api.Entities;

// What a manager decided about a recovery proposal, kept so later recoveries can learn from it.
public class RecoveryMemory
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CentreId { get; set; }
    public Centre Centre { get; set; } = default!;

    public Guid WorkflowId { get; set; }
    public AgentWorkflow Workflow { get; set; } = default!;

    public RecoveryMemoryKind Kind { get; set; }
    public IncidentType IncidentType { get; set; }
    public string RouteNumber { get; set; } = default!;
    public string? Reason { get; set; }
    // A one-line description of the proposal, so people and prompts can read it without looking up each id.
    public string Summary { get; set; } = default!;

    // The resources in the proposal that was decided on.
    public Guid? VehicleId { get; set; }
    public Guid? DriverId { get; set; }
    public Guid? BayId { get; set; }
    public int DelayMinutes { get; set; }

    // What the manager wants avoided next time. Only used for rejections.
    public bool AvoidVehicle { get; set; }
    public bool AvoidDriver { get; set; }
    public bool AvoidBay { get; set; }
    public int? MaxDelayMinutes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
