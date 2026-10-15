using api.Entities;
using api.Entities.Enums;

namespace api.DTO.Fares;

public sealed record FareRuleListItemResponse(Guid Id, Guid RouteId, FareRuleRouteSummary Route, decimal Amount, bool IsActive, DateTime UpdatedAt)
{
    public static FareRuleListItemResponse From(FareRule rule) => new(
        rule.Id,
        rule.RouteId,
        new FareRuleRouteSummary(rule.Route.RouteNumber, rule.Route.Name, rule.Route.CentreId),
        rule.Amount,
        rule.IsActive,
        rule.UpdatedAt);
}

public sealed record FareRuleRouteSummary(string RouteNumber, string Name, Guid CentreId);

public sealed record FareRuleResponse(
    Guid Id,
    Guid RouteId,
    FareRuleRouteDetail Route,
    decimal Amount,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static FareRuleResponse From(FareRule rule) => new(
        rule.Id,
        rule.RouteId,
        new FareRuleRouteDetail(rule.Route.Id, rule.Route.RouteNumber, rule.Route.Name, rule.Route.Origin, rule.Route.Destination, rule.Route.CentreId),
        rule.Amount,
        rule.IsActive,
        rule.CreatedAt,
        rule.UpdatedAt);
}

public sealed record FareRuleRouteDetail(Guid Id, string RouteNumber, string Name, string Origin, string Destination, Guid CentreId);

public sealed record FareRuleCreatedResponse(Guid Id, Guid RouteId, decimal Amount, bool IsActive);

public sealed record FareQuoteResponse(FareQuoteTrip Trip, FareQuotePassenger Passenger, Guid FareRuleId, decimal Fare);

public sealed record FareQuoteTrip(Guid Id, DateTime ScheduledTime, string Route, string Name);

public sealed record FareQuotePassenger(Guid Id, PassengerCategory Category);
