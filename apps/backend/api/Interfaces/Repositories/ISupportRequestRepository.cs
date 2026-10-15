using api.DTO.Support;
using api.Entities;

namespace api.Interfaces.Repositories;

public interface ISupportRequestRepository : IRepository<SupportRequest>
{
    Task<List<SupportRequest>> SearchAsync(SupportRequestFilter filter, CancellationToken cancellationToken = default);
    Task<SupportRequest?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);
}
