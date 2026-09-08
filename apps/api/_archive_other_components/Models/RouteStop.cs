namespace api.Models;

public class RouteStop
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RouteId { get; set; }
    public Route Route { get; set; } = default!;

    public string StopName { get; set; } = default!;
    public int SequenceOrder { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
}