using api.Enums;

namespace api.Models;

public class Route
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string RouteNumber { get; set; } = default!;   // e.g. "01", "01-AC"
    public string Name { get; set; } = default!;           // e.g. "Kurunegala - Colombo via Road 6"
    public string Origin { get; set; } = default!;
    public string Destination { get; set; } = default!;
    public VehicleType ServiceType { get; set; }
    public decimal DistanceKm { get; set; }
    public int EstimatedDurationMin { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<RouteStop> Stops { get; set; } = new List<RouteStop>();
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}