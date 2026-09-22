namespace api.Models;

/// <summary>One travel direction within a two-way public transport route.</summary>
public class RouteDirection
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RouteId { get; set; }
    public Route Route { get; set; } = default!;
    public Guid StartCentreId { get; set; }
    public Centre StartCentre { get; set; } = default!;
    public Guid EndCentreId { get; set; }
    public Centre EndCentre { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal DistanceKm { get; set; }
    public int EstimatedDurationMin { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<RouteStop> Stops { get; set; } = new List<RouteStop>();
    public ICollection<RouteSchedule> Schedules { get; set; } = new List<RouteSchedule>();
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
