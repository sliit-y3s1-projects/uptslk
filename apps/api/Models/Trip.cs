using api.Enums;

namespace api.Models;

public class Trip
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RouteId { get; set; }
    public Route Route { get; set; } = default!;

    public Guid VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = default!;

    public Guid DriverId { get; set; }
    public Driver Driver { get; set; } = default!;

    public DateTime ScheduledTime { get; set; }
    public TripStatus Status { get; set; } = TripStatus.Scheduled;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
}