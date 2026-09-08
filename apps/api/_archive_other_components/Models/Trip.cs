using api.Enums;

namespace api.Models;

public class Trip
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CentreId { get; set; }
    public Centre Centre { get; set; } = default!;

    public Guid RouteId { get; set; }
    public Route Route { get; set; } = default!;

    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = default!;

    public Guid DriverId { get; set; }
    public Driver Driver { get; set; } = default!;

    public Guid BayId { get; set; }
    public Bay Bay { get; set; } = default!;

    public DateTime ScheduledTime { get; set; }
    public TripStatus Status { get; set; } = TripStatus.Scheduled;
    public string? Notes { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime? ActualDepartureAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
}
