namespace api.Entities.Enums;

public enum PaymentStatus
{
    Initiated,
    Pending,
    Succeeded,
    Failed,
    Cancelled,
    Chargebacked,
    RefundPending,
    Refunded
}
