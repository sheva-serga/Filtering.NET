using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

/// <summary>
/// xUnit collection-fixture that owns one open SQLite in-memory connection. Each test creates a
/// brand-new <see cref="ScenarioDbContext"/> bound to the same connection so the schema and rows
/// persist across operations within one test method, but disposal of the fixture cleanly closes
/// the database.
/// </summary>
public sealed class SqliteFixture : IAsyncDisposable
{
    private readonly SqliteConnection _sharedConnection;

    public SqliteFixture()
    {
        // Use a shared-cache, named in-memory database so that multiple connections (if any) see
        // the same schema. Single-connection ":memory:" works too — we keep one persistent
        // connection open for the lifetime of the fixture.
        _sharedConnection = new SqliteConnection("DataSource=:memory:");
        _sharedConnection.Open();
    }

    /// <summary>Creates a fresh DbContext bound to the shared in-memory connection and ensures
    /// the schema exists. Caller owns disposal.</summary>
    public async Task<ScenarioDbContext> CreateContextAsync()
    {
        var contextOptions = new DbContextOptionsBuilder<ScenarioDbContext>()
            .UseSqlite(_sharedConnection)
            .Options;
        var dbContext = new ScenarioDbContext(contextOptions);
        await dbContext.Database.EnsureCreatedAsync();
        return dbContext;
    }

    /// <summary>Drops the schema and recreates it so tests start from a clean slate.</summary>
    public async Task ResetAsync()
    {
        await using var dbContext = await CreateContextAsync();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _sharedConnection.DisposeAsync();
    }
}

/// <summary>xUnit collection marker so fixture instances are shared across all scenarios.</summary>
[Xunit.CollectionDefinition(nameof(SqliteCollection))]
public sealed class SqliteCollection : Xunit.ICollectionFixture<SqliteFixture> { }
