namespace api.DTO.Trips;

public sealed class TripHistoryFilter
{
    public Guid? CentreId { get; init; }
    public DateOnly? Date { get; init; }
}
