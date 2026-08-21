using api.Enums;

namespace api.Models;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TripId { get; set; }
    public Trip Trip { get; set; } = default!;

    public Guid CommuterId { get; set; }
    public User Commuter { get; set; } = default!;

    public string SeatNumber { get; set; } = default!;
    public decimal Fare { get; set; }
    public string QrCode { get; set; } = default!;
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public AgentWorkflow? AgentWorkflow { get; set; }
}