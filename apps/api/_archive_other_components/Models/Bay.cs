using api.Enums;

namespace api.Models;

public class Bay
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CentreId { get; set; }
    public Centre Centre { get; set; } = default!;
    public string Code { get; set; } = default!;
    public string? Name { get; set; }
    public BayStatus Status { get; set; } = BayStatus.Available;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<RouteSchedule> RouteSchedules { get; set; } = new List<RouteSchedule>();
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
