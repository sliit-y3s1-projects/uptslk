using api.Entities;
using api.Entities.Enums;

namespace api.DTO.Centres;

public sealed record CentreListItemResponse(
    Guid Id,
    string Code,
    string Name,
    string City,
    string District,
    CentreStatus Status,
    int BayCount,
    int RouteCount)
{
    // Expects Bays and only the active Routes to be loaded.
    public static CentreListItemResponse From(Centre centre) => new(
        centre.Id,
        centre.Code,
        centre.Name,
        centre.City,
        centre.District,
        centre.Status,
        centre.Bays.Count,
        centre.Routes.Count);
}

public sealed record CentreResponse(
    Guid Id,
    string Code,
    string Name,
    string City,
    string District,
    string? Description,
    CentreStatus Status,
    IEnumerable<CentreBaySummary> Bays,
    IEnumerable<CentreRouteSummary> Routes)
{
    public static CentreResponse From(Centre centre) => new(
        centre.Id,
        centre.Code,
        centre.Name,
        centre.City,
        centre.District,
        centre.Description,
        centre.Status,
        centre.Bays.OrderBy(bay => bay.Code).Select(bay => new CentreBaySummary(bay.Id, bay.Code, bay.Name, bay.Status)).ToList(),
        centre.Routes.OrderBy(route => route.RouteNumber)
            .Select(route => new CentreRouteSummary(route.Id, route.RouteNumber, route.Name, route.Origin, route.Destination, route.IsActive))
            .ToList());
}

public sealed record CentreBaySummary(Guid Id, string Code, string? Name, BayStatus Status);

public sealed record CentreRouteSummary(Guid Id, string RouteNumber, string Name, string Origin, string Destination, bool IsActive);

public sealed record CentreCreatedResponse(Guid Id, string Code, string Name, CentreStatus Status);

public sealed record BayResponse(Guid Id, Guid CentreId, string Code, string? Name, BayStatus Status)
{
    public static BayResponse From(Bay bay) => new(bay.Id, bay.CentreId, bay.Code, bay.Name, bay.Status);
}
