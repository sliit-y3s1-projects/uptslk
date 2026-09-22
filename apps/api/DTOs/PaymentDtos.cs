using System.ComponentModel.DataAnnotations;
using api.Enums;

namespace api.DTOs;

public sealed class StartCheckoutRequest
{
    public Guid TripId { get; init; }
    public Guid PassengerId { get; init; }
    public PaymentProvider Provider { get; init; } = PaymentProvider.Stripe;
}


public sealed class RequestPaymentRefundRequest
{
    [Required, StringLength(1000)] public string Reason { get; init; } = default!;
}
