using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Filtering.Net.EntityFrameworkCore.Tests.MapNested;

public sealed class MapNestedSqliteFixture : IAsyncDisposable
{
    private readonly SqliteConnection _sharedConnection;

    public MapNestedSqliteFixture()
    {
        _sharedConnection = new SqliteConnection("DataSource=:memory:");
        _sharedConnection.Open();
    }

    public async Task<MapNestedDbContext> CreateContextAsync()
    {
        var contextOptions = new DbContextOptionsBuilder<MapNestedDbContext>()
            .UseSqlite(_sharedConnection)
            .Options;
        var dbContext = new MapNestedDbContext(contextOptions);
        await dbContext.Database.EnsureCreatedAsync();
        return dbContext;
    }

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

[Xunit.CollectionDefinition(nameof(MapNestedSqliteCollection))]
public sealed class MapNestedSqliteCollection : Xunit.ICollectionFixture<MapNestedSqliteFixture> { }
