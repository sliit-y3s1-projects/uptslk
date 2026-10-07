using api.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace api.Tests.Database;

/// <summary>
/// Starts one real PostgreSQL 17 container (Testcontainers) for the whole database test collection.
/// The schema is created once, by applying every EF Core migration, into a template database.
/// Each test then gets its own isolated database cloned from that template.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private const string TemplateName = "upts_template";
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder()
        .WithImage("postgres:17")
        .WithUsername("upts_test")
        .WithPassword("upts_test_password")
        // Every test uses its own database and therefore its own connection pool; the default limit of 100 runs out.
        .WithCommand("-c", "max_connections=1000")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await ExecuteAdminAsync($"CREATE DATABASE {TemplateName}");
        await using var template = CreateContext(ConnectionStringFor(TemplateName));
        await template.Database.MigrateAsync();
        NpgsqlConnection.ClearAllPools(); // a template database must have no open connections
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    /// <summary>A new database that already contains the full migrated schema.</summary>
    public async Task<string> CreateMigratedDatabaseAsync()
    {
        var name = $"t_{Guid.NewGuid():N}";
        await ExecuteAdminAsync($"CREATE DATABASE {name} TEMPLATE {TemplateName}");
        return ConnectionStringFor(name);
    }

    /// <summary>A new, completely empty database (no tables), used by the migration tests.</summary>
    public async Task<string> CreateEmptyDatabaseAsync()
    {
        var name = $"e_{Guid.NewGuid():N}";
        await ExecuteAdminAsync($"CREATE DATABASE {name}");
        return ConnectionStringFor(name);
    }

    public static AppDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connectionString).Options);

    private string ConnectionStringFor(string database) =>
        new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = database, Pooling = true, MaxPoolSize = 20, ConnectionIdleLifetime = 2, ConnectionPruningInterval = 1 }.ConnectionString;

    private async Task ExecuteAdminAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture>
{
    public const string Name = "postgres";
}
