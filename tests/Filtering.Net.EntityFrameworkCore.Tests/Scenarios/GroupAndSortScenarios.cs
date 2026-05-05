using Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.EntityFrameworkCore.Tests.Scenarios;

/// <summary>End-to-end SQLite scenarios for group combinators (And/Or/Not), sort directives,
/// and pagination boundary cases.</summary>
[Collection(nameof(SqliteCollection))]
public class GroupAndSortScenarios(SqliteFixture sqliteFixture)
{
    private readonly SqliteFixture _sqliteFixture = sqliteFixture;

    [Fact]
    public async Task ApplyPagedAsync_AndGroupRequest_AllChildrenMustMatch()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.And(
                FilterRequestBuilder.Leaf("IsActive", "eq", true),
                FilterRequestBuilder.Leaf("Quantity", "gte", 30)),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([3, 4]);
    }

    [Fact]
    public async Task ApplyPagedAsync_OrGroupRequest_AnyChildMatches()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Or(
                FilterRequestBuilder.Leaf("Name", "eq", "Alpha"),
                FilterRequestBuilder.Leaf("Quantity", "eq", 50)),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([1, 5]);
    }

    [Fact]
    public async Task ApplyPagedAsync_NotGroupRequest_NegatesChild()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Not(
                FilterRequestBuilder.Leaf("IsActive", "eq", true)),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([2, 5]);
    }

    [Fact]
    public async Task ApplyPagedAsync_SortAscRequest_OrdersAscending()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Sort = [new SortItem("Name", SortDir.Asc)],
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task ApplyPagedAsync_SortDescRequest_OrdersDescending()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Sort = [new SortItem("Quantity", SortDir.Desc)],
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Quantity).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task ApplyPagedAsync_MultiFieldSortRequest_OrdersByEachInOrder()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Sort =
            [
                new SortItem("Status", SortDir.Asc),
                new SortItem("Quantity", SortDir.Desc),
            ],
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        // Status sorted lexicographically (Active, Archived, Pending), then Quantity desc inside.
        pageResult.Items.Select(widget => widget.Id).Should().Equal(4, 1, 3, 5, 2);
    }

    [Fact]
    public async Task ApplyPagedAsync_FirstPageRequest_ReturnsFirstSlice()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Sort = [new SortItem("Id", SortDir.Asc)],
            Page = 1,
            PageSize = 2,
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().Equal(1, 2);
        pageResult.TotalCount.Should().Be(5);
        pageResult.HasNext.Should().BeTrue();
        pageResult.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyPagedAsync_LastPageRequest_ReturnsRemainder()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Sort = [new SortItem("Id", SortDir.Asc)],
            Page = 3,
            PageSize = 2,
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().Equal(5);
        pageResult.HasNext.Should().BeFalse();
        pageResult.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyPagedAsync_PageBeyondEndRequest_ReturnsEmpty()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Sort = [new SortItem("Id", SortDir.Asc)],
            Page = 99,
            PageSize = 10,
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Should().BeEmpty();
        pageResult.TotalCount.Should().Be(5);
    }
}
