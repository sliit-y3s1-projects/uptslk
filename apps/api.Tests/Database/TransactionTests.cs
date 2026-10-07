using api.Enums;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace api.Tests.Database;

/// <summary>Database testing: transaction behaviour (commit, rollback, atomic SaveChanges) on PostgreSQL.</summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Database")]
public sealed class TransactionTests(PostgresFixture postgres)
{
    [Fact]
    public async Task RolledBackTransaction_LeavesNoRows()
    {
        await using var db = PostgresFixture.CreateContext(await postgres.CreateMigratedDatabaseAsync());

        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            db.Add(PgData.Centre("RB1"));
            await db.SaveChangesAsync();
            Assert.Equal(1, await db.Centres.CountAsync()); // visible inside the transaction
            await transaction.RollbackAsync();
        }

        await using var reader = PostgresFixture.CreateContext(db.Database.GetConnectionString()!);
        Assert.Equal(0, await reader.Centres.CountAsync());
    }

    [Fact]
    public async Task CommittedTransaction_PersistsRows_ForOtherConnections()
    {
        await using var db = PostgresFixture.CreateContext(await postgres.CreateMigratedDatabaseAsync());

        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            db.Add(PgData.Centre("CM1"));
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        await using var reader = PostgresFixture.CreateContext(db.Database.GetConnectionString()!);
        Assert.Equal(1, await reader.Centres.CountAsync());
    }

    [Fact]
    public async Task UncommittedRows_AreInvisibleToOtherConnections()
    {
        await using var db = PostgresFixture.CreateContext(await postgres.CreateMigratedDatabaseAsync());
        await using var transaction = await db.Database.BeginTransactionAsync();
        db.Add(PgData.Centre("UN1"));
        await db.SaveChangesAsync();

        await using var reader = PostgresFixture.CreateContext(db.Database.GetConnectionString()!);

        Assert.Equal(0, await reader.Centres.CountAsync());
        await transaction.RollbackAsync();
    }

    [Fact]
    public async Task SaveChanges_IsAtomic_WhenOneRowViolatesAConstraint()
    {
        await using var db = PostgresFixture.CreateContext(await postgres.CreateMigratedDatabaseAsync());
        var existing = PgData.Centre("EXI");
        db.AddRange(existing, PgData.Vehicle(existing, "DUP-1"));
        await db.SaveChangesAsync();
        await using var second = PostgresFixture.CreateContext(db.Database.GetConnectionString()!);

        second.Add(PgData.Centre("NEW")); // valid
        second.Add(PgData.Vehicle(existing, "DUP-1")); // violates the unique plate index

        await PgData.AssertSqlStateAsync("23505", () => second.SaveChangesAsync());
        await using var reader = PostgresFixture.CreateContext(db.Database.GetConnectionString()!);
        Assert.Equal(1, await reader.Centres.CountAsync()); // "NEW" was rolled back together with the bad row
    }

    [Fact]
    public async Task FailureInsideTransaction_CanRollBackEarlierSaves()
    {
        await using var db = PostgresFixture.CreateContext(await postgres.CreateMigratedDatabaseAsync());
        var g = await PgData.SeedGraphAsync(db, balance: 300m);

        await using (var transaction = await db.Database.BeginTransactionAsync())
        {
            (await db.Wallets.SingleAsync()).Balance -= 100m;
            await db.SaveChangesAsync();
            db.Add(PgData.Booking(g, "S1"));
            db.Add(PgData.Booking(g, "S1")); // duplicate active seat
            await Assert.ThrowsAnyAsync<DbUpdateException>(() => db.SaveChangesAsync());
            await transaction.RollbackAsync();
        }

        await using var reader = PostgresFixture.CreateContext(db.Database.GetConnectionString()!);
        Assert.Equal(300m, (await reader.Wallets.SingleAsync()).Balance);
        Assert.Equal(0, await reader.Bookings.CountAsync());
    }

    [Fact]
    public async Task TwoContexts_UpdatingTheSameRow_BothSucceed_LastWriteWins()
    {
        // Documents the current behaviour: entities have no concurrency token, so PostgreSQL applies both updates.
        await using var db = PostgresFixture.CreateContext(await postgres.CreateMigratedDatabaseAsync());
        await PgData.SeedGraphAsync(db, balance: 100m);
        await using var first = PostgresFixture.CreateContext(db.Database.GetConnectionString()!);
        await using var second = PostgresFixture.CreateContext(db.Database.GetConnectionString()!);
        var a = await first.Wallets.SingleAsync();
        var b = await second.Wallets.SingleAsync();

        a.Balance -= 30m;
        b.Balance -= 50m;
        await first.SaveChangesAsync();
        await second.SaveChangesAsync();

        await using var reader = PostgresFixture.CreateContext(db.Database.GetConnectionString()!);
        Assert.Equal(50m, (await reader.Wallets.SingleAsync()).Balance); // 30 was lost - see BookingConcurrencyTests
    }
}
