using AwesomeAssertions;

using Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

using Xunit;

namespace Filtering.Net.EntityFrameworkCore.Tests.Scenarios;

[Collection(nameof(SqliteCollection))]
public class PropertyMapSortScenarios(SqliteFixture sqliteFixture)
{
    private readonly SqliteFixture _sqliteFixture = sqliteFixture;

    [Fact]
    public async Task ApplyPagedAsync_SortByComputedRuleAlias_OrdersByTheComputedValue()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetComputedSortFilter();
        var request = new FilterRequest
        {
            Sort = [new SortItem("countOrZero", SortDir.Desc), new SortItem("Id", SortDir.Asc)],
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().Equal(5, 3, 1, 2, 4);
    }
}
