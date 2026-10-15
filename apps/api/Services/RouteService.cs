using api.DTO.Routes;
using api.Entities;
using api.Exceptions;
using api.Interfaces.Repositories;
using api.Interfaces.Services;
using Route = api.Entities.Route;
using RouteDirection = api.Entities.RouteDirection;

namespace api.Services;

public sealed class RouteService(
    IRouteRepository routes,
    IRouteDirectionRepository directions,
    IRouteStopRepository stops,
    ICentreRepository centres,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser) : IRouteService
{
    private const string RouteNotFound = "Route not found.";
    private const string DirectionNotFound = "Route direction not found.";
    private const string SameEndpoints = "A direction needs different start and end centres.";
    private const string MissingEndpoints = "One or more direction centres do not exist.";

    public async Task<IReadOnlyList<RouteSummaryResponse>> ListAsync(RouteFilter filter, CancellationToken cancellationToken)
    {
        var found = await routes.SearchAsync(filter, cancellationToken);
        return found.Select(RouteSummaryResponse.From).ToList();
    }

    public async Task<RouteResponse> GetAsync(Guid routeId, CancellationToken cancellationToken)
    {
        var route = await routes.GetWithDetailsAsync(routeId, cancellationToken: cancellationToken) ?? throw new NotFoundException(RouteNotFound);
        return RouteResponse.From(route);
    }

    public async Task<RouteCreatedResponse> CreateAsync(CreateRouteRequest request, CancellationToken cancellationToken)
    {
        currentUser.EnsureCanManageCentre(request.CentreId);
        if (!await centres.ExistsAsync(request.CentreId, cancellationToken)) throw new BadRequestException("The selected centre does not exist.");

        var routeNumber = request.RouteNumber.Trim().ToUpperInvariant();
        if (await routes.RouteNumberExistsAsync(request.CentreId, routeNumber, cancellationToken))
            throw new ConflictException("This route number already exists at the selected centre.");

        var route = new Route
        {
            CentreId = request.CentreId,
            RouteNumber = routeNumber,
            Name = request.Name.Trim(),
            Origin = request.Origin.Trim(),
            Destination = request.Destination.Trim(),
            ServiceType = request.ServiceType,
            DistanceKm = request.DistanceKm,
            EstimatedDurationMin = request.EstimatedDurationMin
        };

        // Without endpoint centres this is an old-style route with route-level stops.
        if (!request.StartCentreId.HasValue)
            route.Stops = ToStops(request.Stops, routeId: route.Id);

        if (request.StartCentreId.HasValue || request.EndCentreId.HasValue)
            route.Directions.Add(await CreateFirstDirectionAsync(route, request, cancellationToken));

        routes.Add(route);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new RouteCreatedResponse(route.Id, route.CentreId, route.RouteNumber, route.Name);
    }

    public async Task UpdateAsync(Guid routeId, UpdateRouteRequest request, CancellationToken cancellationToken)
    {
        var route = await routes.GetWithDetailsAsync(routeId, track: true, cancellationToken) ?? throw new NotFoundException(RouteNotFound);
        currentUser.EnsureCanManageCentre(route.CentreId);

        route.Name = request.Name.Trim();
        route.Origin = request.Origin.Trim();
        route.Destination = request.Destination.Trim();
        route.ServiceType = request.ServiceType;
        route.DistanceKm = request.DistanceKm;
        route.EstimatedDurationMin = request.EstimatedDurationMin;
        route.IsActive = request.IsActive;
        route.UpdatedAt = DateTime.UtcNow;

        stops.RemoveRange(route.Stops);
        stops.AddRange(ToStops(request.Stops, routeId: route.Id));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetActiveAsync(Guid routeId, bool isActive, CancellationToken cancellationToken)
    {
        var route = await routes.GetByIdAsync(routeId, cancellationToken) ?? throw new NotFoundException(RouteNotFound);
        currentUser.EnsureCanManageCentre(route.CentreId);

        route.IsActive = isActive;
        route.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RouteDirectionResponse>> ListDirectionsAsync(Guid routeId, CancellationToken cancellationToken)
    {
        var found = await directions.ListForRouteAsync(routeId, cancellationToken);
        return found.Select(RouteDirectionResponse.From).ToList();
    }

    public async Task<RouteDirectionResponse> CreateDirectionAsync(Guid routeId, CreateRouteDirectionRequest request, CancellationToken cancellationToken)
    {
        var route = await routes.GetByIdAsync(routeId, cancellationToken) ?? throw new NotFoundException(RouteNotFound);
        currentUser.EnsureCanManageCentre(route.CentreId);
        await EnsureValidEndpointsAsync(request.StartCentreId, request.EndCentreId, cancellationToken);

        var direction = new RouteDirection
        {
            RouteId = routeId,
            StartCentreId = request.StartCentreId,
            EndCentreId = request.EndCentreId,
            Name = request.Name.Trim(),
            DistanceKm = request.DistanceKm,
            EstimatedDurationMin = request.EstimatedDurationMin
        };
        direction.Stops = ToStops(request.Stops, direction, routeId);
        directions.Add(direction);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Reload so the response includes the centre names.
        var saved = await directions.GetWithDetailsAsync(direction.Id, cancellationToken: cancellationToken) ?? direction;
        return RouteDirectionResponse.From(saved);
    }

    public async Task UpdateDirectionAsync(Guid directionId, UpdateRouteDirectionRequest request, CancellationToken cancellationToken)
    {
        var direction = await directions.GetWithDetailsAsync(directionId, track: true, cancellationToken) ?? throw new NotFoundException(DirectionNotFound);
        currentUser.EnsureCanManageCentre(await routes.GetCentreIdAsync(direction.RouteId, cancellationToken));
        await EnsureValidEndpointsAsync(request.StartCentreId, request.EndCentreId, cancellationToken);

        direction.StartCentreId = request.StartCentreId;
        direction.EndCentreId = request.EndCentreId;
        direction.Name = request.Name.Trim();
        direction.DistanceKm = request.DistanceKm;
        direction.EstimatedDurationMin = request.EstimatedDurationMin;
        direction.IsActive = request.IsActive;
        direction.UpdatedAt = DateTime.UtcNow;

        // Old stops are deleted first so the (direction, sequence) unique index is never broken.
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);
        stops.RemoveRange(direction.Stops);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        stops.AddRange(ToStops(request.Stops, direction, direction.RouteId));
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<RouteDirection> CreateFirstDirectionAsync(Route route, CreateRouteRequest request, CancellationToken cancellationToken)
    {
        if (!request.StartCentreId.HasValue || !request.EndCentreId.HasValue)
            throw new BadRequestException("Both direction endpoint centres are required.");
        if (request.StartCentreId == request.EndCentreId) throw new BadRequestException(SameEndpoints);

        var start = await centres.GetByIdAsync(request.StartCentreId.Value, cancellationToken);
        var end = await centres.GetByIdAsync(request.EndCentreId.Value, cancellationToken);
        if (start is null || end is null) throw new BadRequestException(MissingEndpoints);

        // The route's origin and destination follow its first direction.
        route.Origin = start.Name;
        route.Destination = end.Name;
        var direction = new RouteDirection
        {
            StartCentreId = start.Id,
            EndCentreId = end.Id,
            Name = $"{start.Name} → {end.Name}",
            DistanceKm = request.DistanceKm,
            EstimatedDurationMin = request.EstimatedDurationMin
        };
        direction.Stops = ToStops(request.Stops, direction, route.Id);
        return direction;
    }

    private async Task EnsureValidEndpointsAsync(Guid startCentreId, Guid endCentreId, CancellationToken cancellationToken)
    {
        if (startCentreId == endCentreId) throw new BadRequestException(SameEndpoints);
        if (await centres.CountExistingAsync([startCentreId, endCentreId], cancellationToken) != 2)
            throw new BadRequestException(MissingEndpoints);
    }

    // Stops are numbered in the order they were sent, starting at 1.
    private static List<RouteStop> ToStops(IEnumerable<RouteStopInput> inputs, RouteDirection? direction = null, Guid? routeId = null) =>
        inputs.Select((stop, index) => new RouteStop
        {
            RouteId = routeId ?? direction?.RouteId ?? Guid.Empty,
            RouteDirection = direction,
            StopName = stop.StopName.Trim(),
            SequenceOrder = index + 1,
            Latitude = stop.Latitude,
            Longitude = stop.Longitude
        }).ToList();
}
