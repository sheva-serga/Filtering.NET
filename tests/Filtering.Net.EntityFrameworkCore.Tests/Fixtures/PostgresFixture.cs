using Microsoft.EntityFrameworkCore;

using Testcontainers.PostgreSql;

using Xunit;

namespace Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

/// <summary>
/// Spins up an ephemeral PostgreSQL container via Testcontainers, then exposes a factory for
/// EF Core <see cref="ScenarioDbContext"/> instances bound to the container's connection string.
/// Skipped at runtime when Docker isn't reachable on the host.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    /// <summary>True when the container actually started; false on no-Docker hosts.</summary>
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
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .Build();
            using var startupCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
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
        if (!IsAvailable) throw new InvalidOperationException("Postgres container not available.");
        var contextOptions = new DbContextOptionsBuilder<ScenarioDbContext>()
            .UseNpgsql(ConnectionString)
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

/// <summary>xUnit collection marker for tests that share the same Postgres container.</summary>
[CollectionDefinition(nameof(PostgresCollection))]
public sealed class PostgresCollection : ICollectionFixture<PostgresFixture> { }
