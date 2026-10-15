using api.Entities;

namespace api.Interfaces.Repositories;

public interface IWalletRepository : IRepository<Wallet>
{
    Task<Guid?> GetIdByPassengerIdAsync(Guid passengerId, CancellationToken cancellationToken = default);

    // Locks and reloads the wallet, so parallel balance changes run one at a time. Use inside a transaction.
    Task<Wallet> LockForUpdateAsync(Guid walletId, CancellationToken cancellationToken = default);

    void AddTransaction(Transaction transaction);
}
