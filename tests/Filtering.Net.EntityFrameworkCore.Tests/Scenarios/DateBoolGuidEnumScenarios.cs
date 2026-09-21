using AwesomeAssertions;

using Filtering.Net.EntityFrameworkCore.Tests.Fixtures;

using Xunit;

namespace Filtering.Net.EntityFrameworkCore.Tests.Scenarios;

/// <summary>End-to-end SQLite scenarios for the DateTimeFilter, BoolFilter, GuidFilter and
/// auto-emitted per-enum profiles. One representative test per operator/profile pair to keep
/// the matrix tractable.</summary>
[Collection(nameof(SqliteCollection))]
public class DateBoolGuidEnumScenarios(SqliteFixture sqliteFixture)
{
    private readonly SqliteFixture _sqliteFixture = sqliteFixture;

    [Fact]
    public async Task ApplyPagedAsync_CreatedAtGtRequest_ReturnsFutureRows()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var cutoff = new DateTime(2025, 1, 3, 0, 0, 0, DateTimeKind.Utc);
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("CreatedAt", "gt", cutoff),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([4, 5]);
    }

    [Fact]
    public async Task ApplyPagedAsync_CreatedAtLtRequest_ReturnsPastRows()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        // Cutoff sits between row 2 (CreatedAt = 2025-01-02) and row 3 (CreatedAt = 2025-01-03),
        // so the strict-less-than comparator must include rows 1 + 2 and exclude row 3.
        var cutoff = new DateTime(2025, 1, 2, 12, 0, 0, DateTimeKind.Utc);
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("CreatedAt", "lt", cutoff),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public async Task ApplyPagedAsync_IsActiveEqTrueRequest_ReturnsActiveRows()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("IsActive", "eq", true),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([1, 3, 4]);
    }

    [Fact]
    public async Task ApplyPagedAsync_StatusEqArchivedRequest_ReturnsArchivedOnly()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("Status", "eq", "Archived"),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([3]);
    }

    [Fact]
    public async Task ApplyPagedAsync_StatusInMultipleValuesRequest_ReturnsTwoStatuses()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
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
    public async Task ApplyPagedAsync_ExternalIdEqRequest_FindsExactGuid()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var targetGuid = new Guid("33333333-3333-3333-3333-333333333333");
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("ExternalId", "eq", targetGuid),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([3]);
    }

    [Fact]
    public async Task ApplyPagedAsync_ExternalIdInMultipleGuidsRequest_FindsTwoGuids()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.InLeaf("ExternalId",
                new Guid("11111111-1111-1111-1111-111111111111"),
                new Guid("44444444-4444-4444-4444-444444444444")),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([1, 4]);
    }

    [Fact]
    public async Task ApplyPagedAsync_OptionalStatusEqRequest_TranslatesTheLiftedEnumComparison()
    {
        // Arrange — a nullable enum column goes through MapNullable, so the comparison is lifted
        // before it reaches the provider.
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("OptionalStatus", "eq", "Active"),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([1, 5]);
    }

    [Fact]
    public async Task ApplyPagedAsync_OptionalStatusIsNullRequest_ReturnsNullRowsOnly()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
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

    [Fact]
    public async Task ApplyPagedAsync_OptionalStatusInRequest_TranslatesTheNullableArrayContains()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.InLeaf("OptionalStatus", "Archived", "Pending"),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([3]);
    }

    [Fact]
    public async Task ApplyPagedAsync_OptionalExternalIdEqRequest_TranslatesTheLiftedGuidComparison()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
        await WidgetSeed.SeedAsync(dbContext);
        var widgetFilter = new WidgetFilter();
        var request = new FilterRequest
        {
            Where = FilterRequestBuilder.Leaf("OptionalExternalId", "eq", new Guid("cccccccc-3333-3333-3333-333333333333")),
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetFilter, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeEquivalentTo([3]);
    }

    [Fact]
    public async Task ApplyPagedAsync_OptionalExternalIdInRequest_TranslatesTheNullableGuidArrayContains()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await _sqliteFixture.ResetAsync();
        await using var dbContext = await _sqliteFixture.CreateContextAsync();
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
}
