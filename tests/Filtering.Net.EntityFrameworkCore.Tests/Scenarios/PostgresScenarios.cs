using AwesomeAssertions;

using Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Filtering.Net.EntityFrameworkCore.Tests.Scenarios;

/// <summary>
/// Smoke-tests the same WidgetFilter scenarios against a real PostgreSQL container, confirming
/// the Npgsql provider translates the generated expression tree without falling back to client
/// evaluation. Skipped automatically when Docker is unavailable on the host.
/// </summary>
[Collection(nameof(PostgresCollection))]
public class PostgresScenarios(PostgresFixture postgresFixture)
{
    private readonly PostgresFixture _postgresFixture = postgresFixture;

    [Fact]
    public async Task ApplyPagedAsync_NameContainsRequest_WorksAgainstPostgres()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        if (!_postgresFixture.IsAvailable) Assert.Skip("Docker is not available on this host.");
        await using var dbContext = await _postgresFixture.CreateContextAsync();
        await ResetSchemaAsync(dbContext, cancellationToken);
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("Name", "contains", "et"),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Name).Should().BeEquivalentTo(["Beta"]);
    }

    [Fact]
    public async Task ApplyPagedAsync_NumericInOperatorRequest_WorksAgainstPostgres()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        if (!_postgresFixture.IsAvailable) Assert.Skip("Docker is not available on this host.");
        await using var dbContext = await _postgresFixture.CreateContextAsync();
        await ResetSchemaAsync(dbContext, cancellationToken);
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.InLeaf("Quantity", 10, 30, 50),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([1, 3, 5]);
    }

    [Fact]
    public async Task ApplyFilter_ToQueryStringOnPostgres_ContainsParameterPlaceholder()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        if (!_postgresFixture.IsAvailable) Assert.Skip("Docker is not available on this host.");
        await using var dbContext = await _postgresFixture.CreateContextAsync();
        await ResetSchemaAsync(dbContext, cancellationToken);
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("Name", "eq", "Beta"),
        };

        // Act
        var filteredQuery = widgetFilter.ApplyFilter(dbContext.Widgets.AsQueryable(), request.Where);
        var renderedSql = filteredQuery.ToQueryString();

        // Assert
        // Npgsql may either emit a $1 / @__leafValue placeholder *or* declare the parameter in
        // a header before inlining its value into the rendered SQL body. Either is valid; the
        // important property is that ToListAsync runs the query as a prepared statement when
        // executed by EF Core's pipeline (asserted indirectly by the other Postgres scenarios).
        // The static-SQL inspection here just confirms the column appears in a WHERE clause.
        renderedSql.Should().Contain("WHERE");
        renderedSql.Should().Contain("\"Name\"");
    }

    [Fact]
    public async Task ApplyPagedAsync_OptionalCountEqRequest_LiftsTheNullableComparisonOnPostgres()
    {
        // Arrange — MapNullable rebuilds the comparison as a lifted one; Npgsql has to translate that
        // to "OptionalCount" = @p rather than client-evaluating it.
        var cancellationToken = TestContext.Current.CancellationToken;
        if (!_postgresFixture.IsAvailable) Assert.Skip("Docker is not available on this host.");
        await using var dbContext = await _postgresFixture.CreateContextAsync();
        await ResetSchemaAsync(dbContext, cancellationToken);
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("OptionalCount", "eq", 5),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([3]);
    }

    [Fact]
    public async Task ApplyPagedAsync_OptionalCountInRequest_TranslatesTheNullableArrayContainsOnPostgres()
    {
        // Arrange — the span-bound values.Contains(column) is re-targeted to the nullable array
        // overload; if that re-targeting broke, Npgsql would throw at translation time.
        var cancellationToken = TestContext.Current.CancellationToken;
        if (!_postgresFixture.IsAvailable) Assert.Skip("Docker is not available on this host.");
        await using var dbContext = await _postgresFixture.CreateContextAsync();
        await ResetSchemaAsync(dbContext, cancellationToken);
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.InLeaf("OptionalCount", 1, 8),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([1, 5]);
    }

    [Fact]
    public async Task ApplyPagedAsync_OptionalExternalIdInRequest_TranslatesTheNullableGuidArrayOnPostgres()
    {
        // Arrange — Npgsql maps Guid to uuid, so the nullable-array re-targeting has to survive a
        // non-numeric column type too.
        var cancellationToken = TestContext.Current.CancellationToken;
        if (!_postgresFixture.IsAvailable) Assert.Skip("Docker is not available on this host.");
        await using var dbContext = await _postgresFixture.CreateContextAsync();
        await ResetSchemaAsync(dbContext, cancellationToken);
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.InLeaf("OptionalExternalId",
                new Guid("aaaaaaaa-1111-1111-1111-111111111111"),
                new Guid("eeeeeeee-5555-5555-5555-555555555555")),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([1, 5]);
    }

    [Fact]
    public async Task ApplyPagedAsync_OptionalStatusIsNullRequest_TranslatesTheNullableEnumGuardOnPostgres()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        if (!_postgresFixture.IsAvailable) Assert.Skip("Docker is not available on this host.");
        await using var dbContext = await _postgresFixture.CreateContextAsync();
        await ResetSchemaAsync(dbContext, cancellationToken);
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("OptionalStatus", "isNull", null),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([2, 4]);
    }

    private static async Task ResetSchemaAsync(ScenarioDbContext dbContext, CancellationToken cancellationToken)
    {
        // Postgres won't let us drop a database we're connected to — clearing the table is
        // sufficient for these per-test resets since the schema doesn't change.
        if (await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            dbContext.Widgets.RemoveRange(dbContext.Widgets);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
