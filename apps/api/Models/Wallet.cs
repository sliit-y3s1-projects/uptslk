namespace api.Models;

public class Wallet
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid PassengerId { get; set; }
    public Passenger Passenger { get; set; } = default!;

    public decimal Balance { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
}
