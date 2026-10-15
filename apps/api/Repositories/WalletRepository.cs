using api.Data;
using api.Entities;
using api.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public sealed class WalletRepository(AppDbContext db) : Repository<Wallet>(db), IWalletRepository
{
    public Task<Guid?> GetIdByPassengerIdAsync(Guid passengerId, CancellationToken cancellationToken = default) =>
        Set.Where(wallet => wallet.PassengerId == passengerId).Select(wallet => (Guid?)wallet.Id).SingleOrDefaultAsync(cancellationToken);

    public async Task<Wallet> LockForUpdateAsync(Guid walletId, CancellationToken cancellationToken = default)
    {
        await Db.LockRowAsync("Wallets", walletId, cancellationToken);
        // Forget any copy loaded before the lock so the balance is read fresh.
        Db.ChangeTracker.Clear();
        return await Set.SingleAsync(wallet => wallet.Id == walletId, cancellationToken);
    }

    // Added through its own set: new rows already have a Guid key, so EF would treat a row
    // added through the wallet's collection as an existing one.
    public void AddTransaction(Transaction transaction) => Db.Transactions.Add(transaction);
}
