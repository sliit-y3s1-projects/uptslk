using api.Entities;

namespace api.Interfaces.Repositories;

public interface IWalletRepository : IRepository<Wallet>
{
    Task<Guid?> GetIdByPassengerIdAsync(Guid passengerId, CancellationToken cancellationToken = default);

    // Locks the wallet row and reloads it, so parallel balance changes run one after another.
    // Call it inside a transaction.
    Task<Wallet> LockForUpdateAsync(Guid walletId, CancellationToken cancellationToken = default);

    void AddTransaction(Transaction transaction);
}
