using api.Data;
using api.Entities;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class PaymentRepository(AppDbContext db) : Repository<Payment>(db), IPaymentRepository
{
    public Task<Payment?> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().SingleOrDefaultAsync(payment => payment.ProviderOrderId == orderId, cancellationToken);
}
