using Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.EntityFrameworkCore.Tests.Scenarios;

/// <summary>End-to-end SQLite scenarios exercising the StringFilter primitive profile against
/// the generated WidgetFilter. Verifies eq/contains/in/isNull operators all produce the rows the
/// SQL expression-tree should match.</summary>
[Collection(nameof(SqliteCollection))]
public class StringFilterScenarios(SqliteFixture sqliteFixture)
{
    private readonly SqliteFixture _sqliteFixture = sqliteFixture;

    [Fact]
    public async Task ApplyPagedAsync_NameEqRequest_FindsExactMatch()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("Name", "eq", "Beta"),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Name).Should().BeEquivalentTo(["Beta"]);
    }

    [Fact]
    public async Task ApplyPagedAsync_NameContainsRequest_FindsSubstringMatches()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
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
    public async Task ApplyPagedAsync_NameInRequest_MatchesAnyOfList()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.InLeaf("Name", "Alpha", "Gamma", "Epsilon"),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Name).Should().BeEquivalentTo(["Alpha", "Gamma", "Epsilon"]);
    }
}
