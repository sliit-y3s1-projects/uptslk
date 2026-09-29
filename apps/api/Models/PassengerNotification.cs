using api.Enums;

namespace api.Models;

public class PassengerNotification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WorkflowId { get; set; }
    public AgentWorkflow Workflow { get; set; } = default!;

    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = default!;

    public Guid PassengerId { get; set; }
    public Passenger Passenger { get; set; } = default!;

    public PassengerNotificationChannel Channel { get; set; } = PassengerNotificationChannel.InApp;
    public PassengerNotificationStatus Status { get; set; } = PassengerNotificationStatus.Delivered;
    public string Subject { get; set; } = default!;
    public string Message { get; set; } = default!;
    public int DeliveryAttemptCount { get; set; } = 1;
    public string? DeliveryError { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
