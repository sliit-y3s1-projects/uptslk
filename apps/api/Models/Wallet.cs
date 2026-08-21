namespace api.Models;

public class Wallet
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CommuterId { get; set; }
    public User Commuter { get; set; } = default!;

    public decimal Balance { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}