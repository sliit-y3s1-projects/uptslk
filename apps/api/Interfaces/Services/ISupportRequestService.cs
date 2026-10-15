using api.DTO.Support;

namespace api.Interfaces.Services;

public interface ISupportRequestService
{
    Task<IReadOnlyList<SupportRequestResponse>> ListAsync(SupportRequestFilter filter, CancellationToken cancellationToken);
    Task<SupportRequestResponse> GetAsync(Guid id, CancellationToken cancellationToken);
    Task<SupportRequestCreatedResponse> CreateAsync(CreateSupportRequest input, CancellationToken cancellationToken);
    Task UpdateAsync(Guid id, UpdateSupportRequest input, CancellationToken cancellationToken);
}
