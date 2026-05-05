using Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.EntityFrameworkCore.Tests.Scenarios;

/// <summary>End-to-end SQLite scenarios exercising the per-type numeric profiles (Int32Filter +
/// nullable int + DecimalFilter). Covers eq/gt/lt/in/isNull including the nullable widening that
/// the generator now emits.</summary>
[Collection(nameof(SqliteCollection))]
public class NumberFilterScenarios(SqliteFixture sqliteFixture)
{
    private readonly SqliteFixture _sqliteFixture = sqliteFixture;

    [Fact]
    public async Task ApplyPagedAsync_QuantityEqRequest_FindsExactRow()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("Quantity", "eq", 30),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([3]);
    }

    [Fact]
    public async Task ApplyPagedAsync_QuantityGtRequest_ReturnsAllRowsAbove()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("Quantity", "gt", 25),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([3, 4, 5]);
    }

    [Fact]
    public async Task ApplyPagedAsync_QuantityLtRequest_ReturnsAllRowsBelow()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("Quantity", "lt", 25),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public async Task ApplyPagedAsync_QuantityInRequest_ReturnsExactSet()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.InLeaf("Quantity", 10, 50),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([1, 5]);
    }

    [Fact]
    public async Task ApplyPagedAsync_OptionalCountIsNullRequest_ReturnsNullRowsOnly()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("OptionalCount", "isNull", null),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([2, 4]);
    }

    [Fact]
    public async Task ApplyPagedAsync_PriceGteRequest_ReturnsRowsAtOrAbove()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("Price", "gte", 29.00m),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([3, 4, 5]);
    }
}
