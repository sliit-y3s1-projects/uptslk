namespace api.Models;

public class RouteSchedule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RouteId { get; set; }
    public Route Route { get; set; } = default!;
    public Guid? RouteDirectionId { get; set; }
    public RouteDirection? RouteDirection { get; set; }
    public Guid BayId { get; set; }
    public Bay Bay { get; set; } = default!;
    public TimeOnly FirstDeparture { get; set; }
    public TimeOnly LastDeparture { get; set; }
    public int HeadwayMinutes { get; set; }
    public string OperatingDays { get; set; } = "Mon,Tue,Wed,Thu,Fri,Sat,Sun";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
