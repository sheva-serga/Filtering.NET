using Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

using AwesomeAssertions;

using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Filtering.Net.EntityFrameworkCore.Tests.Scenarios;

/// <summary>End-to-end SQLite scenarios for <c>[ConvertWith&lt;TConverter&gt;]</c>: the
/// filter side declares a custom EF Core <see cref="WidgetStatusConverter"/> and the
/// model side registers the same converter, so the predicate round-trips through both
/// JSON deserialization and SQL translation.</summary>
[Collection(nameof(SqliteCollection))]
public class ValueConverterScenarios(SqliteFixture sqliteFixture)
{
    private readonly SqliteFixture _sqliteFixture = sqliteFixture;

    [Fact]
    public async Task ApplyPagedAsync_StatusEqViaConverterRequest_ReturnsActiveRows()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilterWithExplicitStatusConverter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("Status", "eq", "Active"),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([1, 4]);
    }

    [Fact]
    public async Task ApplyPagedAsync_StatusInViaConverterRequest_ReturnsPendingAndArchivedRows()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilterWithExplicitStatusConverter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.InLeaf("Status", "Pending", "Archived"),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([2, 3, 5]);
    }

    [Fact]
    public async Task ApplyFilter_StatusEqViaConverterRequest_RendersSqlWithStatusColumn()
    {
        // Arrange — ToQueryString proves EF translated the converter-backed predicate
        // into a SQL string comparison rather than falling back to client evaluation.
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilterWithExplicitStatusConverter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("Status", "eq", "Archived"),
        };

        // Act
        var filteredQuery = widgetFilter.ApplyFilter(dbContext.Widgets.AsQueryable(), request.Where);
        var renderedSql = filteredQuery.ToQueryString();

        // Assert
        renderedSql.Should().Contain("WHERE");
        renderedSql.Should().Contain("\"Status\"");
    }
}
