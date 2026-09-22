using api.Enums;

namespace api.Models;

public class PaymentRefund
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PaymentId { get; set; }
    public Payment Payment { get; set; } = default!;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = default!;
    public PaymentRefundStatus Status { get; set; } = PaymentRefundStatus.Requested;
    public string? ProviderRefundId { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
