using api.Data;
using api.Entities;
using api.Entities.Enums;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class PaymentRepository(AppDbContext db) : Repository<Payment>(db), IPaymentRepository
{
    public Task<Payment?> GetByOrderIdAsync(string orderId, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().SingleOrDefaultAsync(payment => payment.ProviderOrderId == orderId, cancellationToken);

    public Task<bool> HasSucceededForBookingAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(payment => payment.BookingId == bookingId && payment.Status == PaymentStatus.Succeeded, cancellationToken);

    // Stripe may only send one of the two ids, so match either.
    public Task<Payment?> GetStripeForCheckoutAsync(string checkoutId, string? orderId, CancellationToken cancellationToken = default) =>
        StripeWithBooking().SingleOrDefaultAsync(payment => payment.ProviderCheckoutId == checkoutId || payment.ProviderOrderId == orderId, cancellationToken);

    public Task<Payment?> GetStripeByOrderIdAsync(string orderId, CancellationToken cancellationToken = default) =>
        StripeWithBooking().SingleOrDefaultAsync(payment => payment.ProviderOrderId == orderId, cancellationToken);

    public Task<Payment?> GetStripeByCheckoutIdAsync(string checkoutId, CancellationToken cancellationToken = default) =>
        StripeWithBooking().SingleOrDefaultAsync(payment => payment.ProviderCheckoutId == checkoutId, cancellationToken);

    public Task<Payment?> GetStripeByPaymentIntentAsync(string paymentIntentId, CancellationToken cancellationToken = default) =>
        StripeWithBooking().SingleOrDefaultAsync(payment => payment.ProviderPaymentId == paymentIntentId, cancellationToken);

    private IQueryable<Payment> StripeWithBooking() =>
        Set.Include(payment => payment.Booking).Where(payment => payment.Provider == PaymentProvider.Stripe);
}
