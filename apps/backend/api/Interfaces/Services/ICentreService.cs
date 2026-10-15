using api.DTO.Centres;

namespace api.Interfaces.Services;

public interface ICentreService
{
    Task<IReadOnlyList<CentreListItemResponse>> ListAsync(CentreFilter filter, CancellationToken cancellationToken);
    Task<CentreResponse> GetAsync(Guid centreId, CancellationToken cancellationToken);
    Task<CentreCreatedResponse> CreateAsync(CreateCentreRequest request, CancellationToken cancellationToken);
    Task UpdateAsync(Guid centreId, UpdateCentreRequest request, CancellationToken cancellationToken);
    Task CloseAsync(Guid centreId, CancellationToken cancellationToken);

    Task<IReadOnlyList<BayResponse>> ListBaysAsync(Guid centreId, CancellationToken cancellationToken);
    Task<BayResponse> GetBayAsync(Guid bayId, CancellationToken cancellationToken);
    Task<BayResponse> CreateBayAsync(Guid centreId, CreateBayRequest request, CancellationToken cancellationToken);
    Task UpdateBayAsync(Guid bayId, UpdateBayRequest request, CancellationToken cancellationToken);
    Task DeactivateBayAsync(Guid bayId, CancellationToken cancellationToken);
}
