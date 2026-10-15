using api.Entities;
using api.Entities.Enums;

namespace api.DTO.Payments;

public sealed record CheckoutSessionResponse(string Url, string? OrderId);

public sealed record PaymentStatusResponse(
    Guid BookingId,
    PaymentStatus Status,
    decimal Amount,
    string Currency,
    PaymentProvider Provider,
    DateTime UpdatedAt)
{
    public static PaymentStatusResponse From(Payment payment) => new(
        payment.BookingId,
        payment.Status,
        payment.Amount,
        payment.Currency,
        payment.Provider,
        payment.UpdatedAt);
}
