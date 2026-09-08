using api.Enums;

namespace api.Models;

public class Centre
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string City { get; set; } = default!;
    public string District { get; set; } = default!;
    public string? Description { get; set; }
    public CentreStatus Status { get; set; } = CentreStatus.Planned;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Bay> Bays { get; set; } = new List<Bay>();
    public ICollection<Route> Routes { get; set; } = new List<Route>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
    public ICollection<Driver> Drivers { get; set; } = new List<Driver>();
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    public ICollection<Incident> Incidents { get; set; } = new List<Incident>();
}
