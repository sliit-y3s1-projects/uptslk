using api.DTO.Routes;

namespace api.Interfaces.Services;

// Routes and their travel directions.
public interface IRouteService
{
    Task<IReadOnlyList<RouteSummaryResponse>> ListAsync(RouteFilter filter, CancellationToken cancellationToken);
    Task<RouteResponse> GetAsync(Guid routeId, CancellationToken cancellationToken);
    Task<RouteCreatedResponse> CreateAsync(CreateRouteRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(Guid routeId, UpdateRouteRequest request, CancellationToken cancellationToken);
    Task SetActiveAsync(Guid routeId, bool isActive, CancellationToken cancellationToken);

    Task<IReadOnlyList<RouteDirectionResponse>> ListDirectionsAsync(Guid routeId, CancellationToken cancellationToken);
    Task<RouteDirectionResponse> CreateDirectionAsync(Guid routeId, CreateRouteDirectionRequest request, CancellationToken cancellationToken);
    Task UpdateDirectionAsync(Guid directionId, UpdateRouteDirectionRequest request, CancellationToken cancellationToken);
}
