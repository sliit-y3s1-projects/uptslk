using api.DTO.Fares;
using api.Entities;
using api.Exceptions;
using api.Interfaces.Repositories;
using api.Interfaces.Services;

namespace api.Services;

public sealed class FareRuleService(
    IFareRuleRepository fareRules,
    ITripRepository trips,
    IPassengerRepository passengers,
    IRouteRepository routes,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IFareRuleService
{
    private const string NotFoundMessage = "Fare rule not found.";

    public async Task<FareQuoteResponse> QuoteAsync(Guid tripId, Guid passengerId, CancellationToken cancellationToken)
    {
        var trip = await trips.GetWithRouteAsync(tripId, cancellationToken);
        var passenger = await passengers.GetActiveAsync(passengerId, cancellationToken);
        if (trip is null) throw new BadRequestException("The selected trip does not exist.");
        if (passenger is null) throw new BadRequestException("The selected passenger is not active.");

        var rule = await fareRules.GetActiveForRouteAsync(trip.RouteId, cancellationToken)
            ?? throw new NotFoundException("No active standard fare exists for this route.");

        return new FareQuoteResponse(
            new FareQuoteTrip(trip.Id, trip.ScheduledTime, trip.Route.RouteNumber, trip.Route.Name),
            new FareQuotePassenger(passenger.Id, passenger.Category),
            rule.Id,
            rule.Amount);
    }

    public async Task<FareQuoteResponse> QuoteForCurrentUserAsync(Guid tripId, CancellationToken cancellationToken)
    {
        var userId = currentUser.RequireUserId();
        var passengerId = await passengers.GetActiveIdByUserIdAsync(userId, cancellationToken)
            ?? throw new BadRequestException("No active passenger profile is linked to this account.");
        return await QuoteAsync(tripId, passengerId, cancellationToken);
    }

    public async Task<IReadOnlyList<FareRuleListItemResponse>> ListAsync(FareRuleFilter filter, CancellationToken cancellationToken)
    {
        var rules = await fareRules.SearchAsync(filter, cancellationToken);
        return rules.Select(FareRuleListItemResponse.From).ToList();
    }

    public async Task<FareRuleResponse> GetAsync(Guid fareRuleId, CancellationToken cancellationToken)
    {
        var rule = await fareRules.GetWithRouteAsync(fareRuleId, cancellationToken: cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);
        return FareRuleResponse.From(rule);
    }

    public async Task<FareRuleCreatedResponse> CreateAsync(CreateFareRuleRequest request, CancellationToken cancellationToken)
    {
        var routeCentreId = await routes.GetActiveRouteCentreIdAsync(request.RouteId, cancellationToken)
            ?? throw new BadRequestException("The selected active route does not exist.");
        currentUser.EnsureCanManageCentre(routeCentreId);
        if (await fareRules.ExistsForRouteAsync(request.RouteId, cancellationToken))
            throw new ConflictException("A standard fare already exists for this route. Edit the existing fare instead.");

        var rule = new FareRule { RouteId = request.RouteId, Amount = request.Amount };
        fareRules.Add(rule);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new FareRuleCreatedResponse(rule.Id, rule.RouteId, rule.Amount, rule.IsActive);
    }

    public async Task UpdateAsync(Guid fareRuleId, UpdateFareRuleRequest request, CancellationToken cancellationToken)
    {
        var rule = await GetManagedRuleAsync(fareRuleId, cancellationToken);
        rule.Amount = request.Amount;
        rule.IsActive = request.IsActive;
        rule.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task DeactivateAsync(Guid fareRuleId, CancellationToken cancellationToken)
    {
        var rule = await GetManagedRuleAsync(fareRuleId, cancellationToken);
        rule.IsActive = false;
        rule.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    // Loads a fare rule for editing and checks the user can manage its route's centre.
    private async Task<FareRule> GetManagedRuleAsync(Guid fareRuleId, CancellationToken cancellationToken)
    {
        var rule = await fareRules.GetWithRouteAsync(fareRuleId, track: true, cancellationToken)
            ?? throw new NotFoundException(NotFoundMessage);
        currentUser.EnsureCanManageCentre(rule.Route.CentreId);
        return rule;
    }
}
