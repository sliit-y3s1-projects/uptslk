using api.Enums;

namespace api.Models;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid WalletId { get; set; }
    public Wallet Wallet { get; set; } = default!;

    public Guid? BookingId { get; set; }
    public Booking? Booking { get; set; }

    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}