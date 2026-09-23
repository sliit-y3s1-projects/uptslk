using api.Enums;

namespace api.Models;

public class PaymentWebhookEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public PaymentProvider Provider { get; set; }
    public string ProviderEventId { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
}
