using Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using Npgsql;

using Testcontainers.MsSql;
using Testcontainers.PostgreSql;

using Xunit;

namespace Filtering.Net.EntityFrameworkCore.Tests.MapNested;

/// <summary>
/// Containerised PostgreSQL backing for <see cref="MapNestedDbContext"/>. Same shape as
/// <see cref="PostgresFixture"/>, but over the nested-navigation model so joined
/// <c>[MapNested]</c> SQL is produced by Npgsql and not only by SQLite.
/// </summary>
public sealed class MapNestedPostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;
    private int _databaseCounter;

    /// <summary>True when the container actually started; false on no-Docker hosts.</summary>
    public bool IsAvailable { get; private set; }

    public async ValueTask InitializeAsync()
    {
        if (!DockerAvailability.IsAvailable)
        {
            IsAvailable = false;
            return;
        }
        try
        {
            _container = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
            using var startupCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            await _container.StartAsync(startupCts.Token);
            IsAvailable = true;
        }
        catch
        {
            IsAvailable = false;
            if (_container is not null) await _container.DisposeAsync();
            _container = null;
        }
    }

    /// <summary>Creates a context over the running container with an empty, freshly created schema.</summary>
    public async Task<MapNestedDbContext> CreateResetContextAsync()
    {
        if (!IsAvailable || _container is null) throw new InvalidOperationException("Postgres container not available.");
        // A database per context: the container's default database cannot be dropped to reset it.
        var connectionString = new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Database = $"map_nested_{Interlocked.Increment(ref _databaseCounter)}",
        }.ConnectionString;
        var contextOptions = new DbContextOptionsBuilder<MapNestedDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        var dbContext = new MapNestedDbContext(contextOptions);
        await dbContext.Database.EnsureCreatedAsync();
        return dbContext;
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
    }
}

/// <summary>xUnit collection marker for the nested-navigation Postgres container.</summary>
[CollectionDefinition(nameof(MapNestedPostgresCollection))]
public sealed class MapNestedPostgresCollection : ICollectionFixture<MapNestedPostgresFixture> { }

/// <summary>Containerised SQL Server backing for <see cref="MapNestedDbContext"/>.</summary>
public sealed class MapNestedSqlServerFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;
    private int _databaseCounter;

    /// <summary>True when the container actually started; false on no-Docker hosts.</summary>
    public bool IsAvailable { get; private set; }

    public async ValueTask InitializeAsync()
    {
        if (!DockerAvailability.IsAvailable)
        {
            IsAvailable = false;
            return;
        }
        try
        {
            _container = new MsSqlBuilder().WithImage("mcr.microsoft.com/mssql/server:2022-latest").Build();
            using var startupCts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            await _container.StartAsync(startupCts.Token);
            IsAvailable = true;
        }
        catch
        {
            IsAvailable = false;
            if (_container is not null) await _container.DisposeAsync();
            _container = null;
        }
    }

    /// <summary>Creates a context over the running container with an empty, freshly created schema.</summary>
    public async Task<MapNestedDbContext> CreateResetContextAsync()
    {
        if (!IsAvailable || _container is null) throw new InvalidOperationException("SQL Server container not available.");
        // A database per context: the container's default database (master) cannot be dropped to reset it.
        var connectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = $"MapNested{Interlocked.Increment(ref _databaseCounter)}",
        }.ConnectionString;
        var contextOptions = new DbContextOptionsBuilder<MapNestedDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        var dbContext = new MapNestedDbContext(contextOptions);
        await dbContext.Database.EnsureCreatedAsync();
        return dbContext;
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
    }
}

/// <summary>xUnit collection marker for the nested-navigation SQL Server container.</summary>
[CollectionDefinition(nameof(MapNestedSqlServerCollection))]
public sealed class MapNestedSqlServerCollection : ICollectionFixture<MapNestedSqlServerFixture> { }
