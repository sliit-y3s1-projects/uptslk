using api.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace api.Tests.AgentRecovery;

internal sealed class RecoveryTestDb : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private RecoveryTestDb(SqliteConnection connection, AppDbContext context)
    {
        _connection = connection;
        Context = context;
    }

    public AppDbContext Context { get; }

    public static async Task<RecoveryTestDb> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;
        var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return new RecoveryTestDb(connection, context);
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
