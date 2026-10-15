namespace api.DTO.Drivers;

// Either one service date, or a range of up to seven days. Defaults to today.
public sealed class DriverTripFilter
{
    public DateOnly? Date { get; init; }
    public DateOnly? FromDate { get; init; }
    public DateOnly? ToDate { get; init; }
}
