using Microsoft.EntityFrameworkCore;

using Testcontainers.MsSql;

using Xunit;

namespace Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

/// <summary>
/// Spins up an ephemeral SQL Server container via Testcontainers. Same shape as
/// <see cref="PostgresFixture"/>; degrades to <see cref="IsAvailable"/>=false when Docker is
/// unavailable or the image fails to pull/start.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private MsSqlContainer? _container;

    /// <summary>True when the container actually started.</summary>
    public bool IsAvailable { get; private set; }

    /// <summary>The connection string for the running container, valid only when
    /// <see cref="IsAvailable"/> is true.</summary>
    public string ConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        if (!DockerAvailability.IsAvailable)
        {
            IsAvailable = false;
            return;
        }
        try
        {
            _container = new MsSqlBuilder()
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .Build();
            using var startupCts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            await _container.StartAsync(startupCts.Token);
            ConnectionString = _container.GetConnectionString();
            IsAvailable = true;
        }
        catch
        {
            IsAvailable = false;
            if (_container is not null) await _container.DisposeAsync();
            _container = null;
        }
    }

    /// <summary>Creates a fresh DbContext bound to the running container and ensures schema.</summary>
    public async Task<ScenarioDbContext> CreateContextAsync()
    {
        if (!IsAvailable) throw new InvalidOperationException("SQL Server container not available.");
        var contextOptions = new DbContextOptionsBuilder<ScenarioDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;
        var dbContext = new ScenarioDbContext(contextOptions);
        await dbContext.Database.EnsureCreatedAsync();
        return dbContext;
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null) await _container.DisposeAsync();
    }
}

/// <summary>xUnit collection marker for tests that share the same SQL Server container.</summary>
[CollectionDefinition(nameof(SqlServerCollection))]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture> { }
