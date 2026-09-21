using AwesomeAssertions;

using Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Filtering.Net.EntityFrameworkCore.Tests.Scenarios;

/// <summary>
/// Runs the WidgetFilter scenarios that are most likely to diverge per provider against a real SQL
/// Server container: nullable-column lifting and the <c>OFFSET</c>/<c>FETCH</c> paging SQL Server
/// only accepts under an <c>ORDER BY</c>. Skipped automatically when Docker is unavailable.
/// </summary>
[Collection(nameof(SqlServerCollection))]
public class SqlServerScenarios(SqlServerFixture sqlServerFixture)
{
    private readonly SqlServerFixture _sqlServerFixture = sqlServerFixture;

    [Fact]
    public async Task ApplyPagedAsync_NameContainsRequest_WorksAgainstSqlServer()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        if (!_sqlServerFixture.IsAvailable) Assert.Skip("Docker is not available on this host.");
        await using var dbContext = await _sqlServerFixture.CreateContextAsync();
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
    public async Task ApplyPagedAsync_OptionalCountEqRequest_LiftsTheNullableComparisonOnSqlServer()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        if (!_sqlServerFixture.IsAvailable) Assert.Skip("Docker is not available on this host.");
        await using var dbContext = await _sqlServerFixture.CreateContextAsync();
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
    public async Task ApplyPagedAsync_OptionalCountInRequest_TranslatesTheNullableArrayContainsOnSqlServer()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        if (!_sqlServerFixture.IsAvailable) Assert.Skip("Docker is not available on this host.");
        await using var dbContext = await _sqlServerFixture.CreateContextAsync();
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
    public async Task ApplyPagedAsync_OptionalExternalIdInRequest_TranslatesTheNullableGuidArrayOnSqlServer()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        if (!_sqlServerFixture.IsAvailable) Assert.Skip("Docker is not available on this host.");
        await using var dbContext = await _sqlServerFixture.CreateContextAsync();
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
    public async Task ApplyPagedAsync_SortedPageRequest_EmitsOffsetFetchOnSqlServer()
    {
        // Arrange — SQL Server rejects OFFSET/FETCH without an ORDER BY, so this is the provider
        // that proves the sort and the paging are composed in the right order.
        var cancellationToken = TestContext.Current.CancellationToken;
        if (!_sqlServerFixture.IsAvailable) Assert.Skip("Docker is not available on this host.");
        await using var dbContext = await _sqlServerFixture.CreateContextAsync();
        await ResetSchemaAsync(dbContext, cancellationToken);
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Sort = [new SortItem("Id", SortDir.Asc)],
            Page = 2,
            PageSize = 2,
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.TotalCount.Should().Be(5);
        pageResult.Items.Select(widget => widget.Id).Should().Equal([3, 4]);
        pageResult.PageSize.Should().Be(2);
        pageResult.TotalPages.Should().Be(3);
    }

    private static async Task ResetSchemaAsync(ScenarioDbContext dbContext, CancellationToken cancellationToken)
    {
        // The container's database persists between tests in the collection; clearing the table is
        // enough because the schema never changes.
        if (await dbContext.Database.CanConnectAsync(cancellationToken))
        {
            dbContext.Widgets.RemoveRange(dbContext.Widgets);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
