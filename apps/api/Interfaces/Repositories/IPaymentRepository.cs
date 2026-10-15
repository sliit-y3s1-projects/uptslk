using api.Entities;

namespace api.Interfaces.Repositories;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<Payment?> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken = default);
}
