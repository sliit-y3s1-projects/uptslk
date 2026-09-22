using api.Enums;

namespace api.Models;

public class Payment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = default!;
    public PaymentProvider Provider { get; set; }
    public string ProviderOrderId { get; set; } = default!;
    public string? ProviderCheckoutId { get; set; }
    public string? ProviderPaymentId { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Initiated;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "LKR";
    public string? PaymentMethod { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<PaymentRefund> Refunds { get; set; } = new List<PaymentRefund>();
}
