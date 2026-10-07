using api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace api.Tests.Database;

/// <summary>Database testing: EF Core migrations against a real, empty PostgreSQL database.</summary>
[Collection(PostgresCollection.Name)]
[Trait("Category", "Database")]
public sealed class MigrationTests(PostgresFixture postgres)
{
    private async Task<AppDbContext> EmptyDatabaseAsync() =>
        PostgresFixture.CreateContext(await postgres.CreateEmptyDatabaseAsync());

    [Fact]
    public async Task Migrate_OnEmptyDatabase_AppliesEveryMigration()
    {
        await using var db = await EmptyDatabaseAsync();
        Assert.NotEmpty(await db.Database.GetPendingMigrationsAsync());

        await db.Database.MigrateAsync();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Equal(db.Database.GetMigrations().Count(), (await db.Database.GetAppliedMigrationsAsync()).Count());
    }

    [Fact]
    public async Task Migrate_CreatesTheCoreTables()
    {
        await using var db = await EmptyDatabaseAsync();
        await db.Database.MigrateAsync();

        foreach (var table in new[] { "Centres", "Bays", "Routes", "RouteStops", "RouteSchedules", "Vehicles", "MaintenanceRecords",
                     "Drivers", "Trips", "Passengers", "Wallets", "Transactions", "Bookings", "FareRules", "Payments",
                     "Incidents", "AgentWorkflows", "AgentSteps", "ApprovalRequests", "AspNetUsers", "AspNetRoles" })
        {
            var found = await PgData.ScalarAsync(db,
                $"SELECT count(*) FROM information_schema.tables WHERE table_schema='public' AND table_name='{table}'");
            Assert.Equal("1", found);
        }
    }

    [Fact]
    public async Task Migrate_CanBeRunTwice_WithoutError()
    {
        await using var db = await EmptyDatabaseAsync();

        await db.Database.MigrateAsync();
        await db.Database.MigrateAsync();

        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task Model_MatchesTheLatestMigrationSnapshot()
    {
        await using var db = await EmptyDatabaseAsync();
        var services = ((IInfrastructure<IServiceProvider>)db).Instance;
        var snapshot = services.GetRequiredService<IMigrationsAssembly>().ModelSnapshot;
        Assert.NotNull(snapshot);

        var initializer = services.GetRequiredService<IModelRuntimeInitializer>();
        var snapshotModel = initializer.Initialize(snapshot!.Model, designTime: true, validationLogger: null);
        var currentModel = services.GetRequiredService<IDesignTimeModel>().Model;
        var differences = services.GetRequiredService<IMigrationsModelDiffer>()
            .GetDifferences(snapshotModel.GetRelationalModel(), currentModel.GetRelationalModel());

        Assert.True(differences.Count == 0,
            "The model has changes that no migration covers. Run `dotnet ef migrations add <Name>`. " +
            $"Differences: {string.Join(", ", differences.Select(d => d.GetType().Name))}");
    }

    [Fact]
    public async Task Migrate_BackToZero_AndForwardAgain_Works()
    {
        await using var db = await EmptyDatabaseAsync();
        await db.Database.MigrateAsync();
        var migrator = db.GetService<IMigrator>();

        await migrator.MigrateAsync("0");
        Assert.Empty(await db.Database.GetAppliedMigrationsAsync());
        Assert.Equal("0", await PgData.ScalarAsync(db,
            "SELECT count(*) FROM information_schema.tables WHERE table_schema='public' AND table_name='Centres'"));

        await migrator.MigrateAsync();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Theory]
    [InlineData("AgentWorkflows", "PlanJson", "jsonb")]
    [InlineData("AgentWorkflows", "ValidationJson", "jsonb")]
    [InlineData("AgentSteps", "ToolCallsJson", "jsonb")]
    [InlineData("Trips", "ScheduledTime", "timestamp with time zone")]
    [InlineData("Wallets", "Balance", "numeric")]
    [InlineData("Bookings", "Fare", "numeric")]
    public async Task Column_HasTheExpectedPostgresType(string table, string column, string expectedType)
    {
        await using var db = PostgresFixture.CreateContext(await postgres.CreateMigratedDatabaseAsync());

        var type = await PgData.ScalarAsync(db,
            $"SELECT data_type FROM information_schema.columns WHERE table_name='{table}' AND column_name='{column}'");

        Assert.Equal(expectedType, type);
    }

    [Theory]
    [InlineData("Wallets", "Balance", 12, 2)]
    [InlineData("Bookings", "Fare", 12, 2)]
    [InlineData("Transactions", "Amount", 12, 2)]
    [InlineData("FareRules", "Amount", 12, 2)]
    public async Task MoneyColumns_UseNumeric12x2(string table, string column, int precision, int scale)
    {
        await using var db = PostgresFixture.CreateContext(await postgres.CreateMigratedDatabaseAsync());

        var actual = await PgData.ScalarAsync(db,
            $"SELECT numeric_precision || ',' || numeric_scale FROM information_schema.columns WHERE table_name='{table}' AND column_name='{column}'");

        Assert.Equal($"{precision},{scale}", actual);
    }

    [Fact]
    public async Task ActiveBookingSeatIndex_IsAPartialUniqueIndex()
    {
        await using var db = PostgresFixture.CreateContext(await postgres.CreateMigratedDatabaseAsync());

        var definition = await PgData.ScalarAsync(db,
            "SELECT indexdef FROM pg_indexes WHERE tablename='Bookings' AND indexdef LIKE '%SeatNumber%'");

        Assert.NotNull(definition);
        Assert.Contains("UNIQUE", definition);
        Assert.Contains("WHERE", definition);
    }
}
