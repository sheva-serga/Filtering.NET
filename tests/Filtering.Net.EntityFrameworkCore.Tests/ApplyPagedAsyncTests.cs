using AwesomeAssertions;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Filtering.Net.EntityFrameworkCore.Tests;

/// <summary>
/// Tests <see cref="FilteringEntityFrameworkExtensions.ApplyPagedAsync{T}"/> against a
/// SQLite in-memory database, driving a hand-built <see cref="FilterDefinition{TEntity}"/> so the
/// EF Core async path is exercised over the real engine without depending on the source generator.
/// </summary>
public class ApplyPagedAsyncTests
{
    [Fact]
    public async Task ApplyPagedAsync_NoFilter_ReturnsAllRowsWithCorrectTotal()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = await CreateSeededDbContextAsync(rowCount: 7, cancellationToken);
        var widgetDefinition = CreateWidgetDefinition();
        var request = new FilterRequest();

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable()
            .ApplyPagedAsync(widgetDefinition, request, cancellationToken);

        // Assert
        pageResult.TotalCount.Should().Be(7);
        pageResult.Items.Should().HaveCount(7);
        pageResult.Page.Should().Be(1);
    }

    [Fact]
    public async Task ApplyPagedAsync_Pagination_ReturnsOnlyRequestedPageSlice()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = await CreateSeededDbContextAsync(rowCount: 25, cancellationToken);
        var widgetDefinition = CreateWidgetDefinition();
        var request = new FilterRequest { Sort = [new SortItem("Id")], Page = 2, PageSize = 10 };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable()
            .ApplyPagedAsync(widgetDefinition, request, cancellationToken);

        // Assert
        pageResult.TotalCount.Should().Be(25);
        pageResult.Items.Should().HaveCount(10);
        pageResult.Page.Should().Be(2);
        pageResult.PageSize.Should().Be(10);
        pageResult.HasNext.Should().BeTrue();
        pageResult.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyPagedAsync_PageWithoutPageSize_ReportsTheEngineResolvedPageSize()
    {
        // Arrange — 25 rows at the definition's DefaultPageSize of 10 makes page 3 the last, partial
        // page. Reporting the materialised row count instead would claim PageSize 5 and 5 total pages.
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = await CreateSeededDbContextAsync(rowCount: 25, cancellationToken);
        var widgetDefinition = CreateWidgetDefinition(new FilterSettings(DefaultPageSize: 10));
        var request = new FilterRequest { Sort = [new SortItem("Id")], Page = 3 };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable()
            .ApplyPagedAsync(widgetDefinition, request, cancellationToken);

        // Assert
        pageResult.Items.Should().HaveCount(5);
        pageResult.PageSize.Should().Be(10);
        pageResult.TotalPages.Should().Be(3);
        pageResult.HasNext.Should().BeFalse();
        pageResult.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyPagedAsync_PageBeyondTheLastPage_ReportsTheSameTotalPagesAsAnInRangePage()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = await CreateSeededDbContextAsync(rowCount: 25, cancellationToken);
        var widgetDefinition = CreateWidgetDefinition(new FilterSettings(DefaultPageSize: 10));
        var request = new FilterRequest { Sort = [new SortItem("Id")], Page = 9 };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable()
            .ApplyPagedAsync(widgetDefinition, request, cancellationToken);

        // Assert — an empty slice must not collapse PageSize to 0 and TotalPages to 1.
        pageResult.Items.Should().BeEmpty();
        pageResult.PageSize.Should().Be(10);
        pageResult.TotalPages.Should().Be(3);
        pageResult.HasNext.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyPagedAsync_PageSizeWithoutPage_ReturnsTheFirstPage()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = await CreateSeededDbContextAsync(rowCount: 25, cancellationToken);
        var widgetDefinition = CreateWidgetDefinition();
        var request = new FilterRequest { Sort = [new SortItem("Id")], PageSize = 10 };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable()
            .ApplyPagedAsync(widgetDefinition, request, cancellationToken);

        // Assert
        pageResult.Page.Should().Be(1);
        pageResult.PageSize.Should().Be(10);
        pageResult.Items.Select(widget => widget.Id).Should().Equal(Enumerable.Range(1, 10));
        pageResult.TotalPages.Should().Be(3);
        pageResult.HasPrevious.Should().BeFalse();
        pageResult.HasNext.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyPagedAsync_PageSizeAboveTheConfiguredMaximum_FailsValidation()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = await CreateSeededDbContextAsync(rowCount: 25, cancellationToken);
        var widgetDefinition = CreateWidgetDefinition(new FilterSettings(DefaultPageSize: 10, MaxPageSize: 20));
        var request = new FilterRequest { PageSize = 200 };

        // Act
        var applyOversizedPage = () => dbContext.Widgets.AsQueryable()
            .ApplyPagedAsync(widgetDefinition, request, cancellationToken);

        // Assert
        var thrownException = await applyOversizedPage.Should().ThrowAsync<FilterValidationException>();
        thrownException.Which.Result.Errors.Should().ContainSingle(error => error.Code == FilterValidationCode.PageSizeTooLarge);
    }

    [Fact]
    public async Task ApplyPagedAsync_NeitherPageNorPageSize_ReportsTheWholeSetAsOnePage()
    {
        // Arrange — nothing was paged, so the reported coordinates must describe a single page
        // covering every matched row rather than the definition's default page size.
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = await CreateSeededDbContextAsync(rowCount: 7, cancellationToken);
        var widgetDefinition = CreateWidgetDefinition(new FilterSettings(DefaultPageSize: 2));
        var request = new FilterRequest { Sort = [new SortItem("Id")] };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable()
            .ApplyPagedAsync(widgetDefinition, request, cancellationToken);

        // Assert
        pageResult.Items.Should().HaveCount(7);
        pageResult.Page.Should().Be(1);
        pageResult.PageSize.Should().Be(7);
        pageResult.TotalPages.Should().Be(1);
        pageResult.HasNext.Should().BeFalse();
        pageResult.HasPrevious.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyPagedAsync_NoRowsAndNoPaging_ReportsOneEmptyPage()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = await CreateSeededDbContextAsync(rowCount: 0, cancellationToken);
        var widgetDefinition = CreateWidgetDefinition();
        var request = new FilterRequest();

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable()
            .ApplyPagedAsync(widgetDefinition, request, cancellationToken);

        // Assert
        pageResult.TotalCount.Should().Be(0);
        pageResult.Page.Should().Be(1);
        pageResult.PageSize.Should().Be(0);
        pageResult.TotalPages.Should().Be(1);
        pageResult.HasNext.Should().BeFalse();
    }

    [Fact]
    public async Task ApplyPagedAsync_Sort_OrdersBySpecifiedField()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = await CreateSeededDbContextAsync(rowCount: 5, cancellationToken);
        var widgetDefinition = CreateWidgetDefinition();
        var request = new FilterRequest
        {
            Sort = [new SortItem("Id", SortDir.Desc)],
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable()
            .ApplyPagedAsync(widgetDefinition, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task ApplyPagedAsync_InvalidRequest_ThrowsFilterValidationException()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = await CreateSeededDbContextAsync(rowCount: 3, cancellationToken);
        var widgetDefinition = CreateWidgetDefinition();
        var request = new FilterRequest
        {
            Where = new FilterLeaf("Mystery", "eq", System.Text.Json.JsonDocument.Parse("\"x\"").RootElement),
        };

        // Act
        var applyInvalidRequest = () => dbContext.Widgets.AsQueryable().ApplyPagedAsync(widgetDefinition, request, cancellationToken);

        // Assert
        await applyInvalidRequest.Should().ThrowAsync<FilterValidationException>();
    }

    private static FilterDefinition<Widget> CreateWidgetDefinition(FilterSettings? settings = null) =>
        new(new FilterSchemaBuilder<Widget>(settings ?? new FilterSettings())
            .Add(FilterProperty.Map<Widget, int>("Id", widget => widget.Id, Int32Filter.Profile).Sortable().Build())
            .Add(FilterProperty.Map<Widget, string>("Name", widget => widget.Name, StringFilter.Profile).Sortable().Build())
            .Build());

    private static async Task<TestDbContext> CreateSeededDbContextAsync(int rowCount, CancellationToken cancellationToken = default)
    {
        var sqliteConnection = new SqliteConnection("DataSource=:memory:");
        await sqliteConnection.OpenAsync(cancellationToken);
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(sqliteConnection)
            .Options;
        var dbContext = new TestDbContext(options, sqliteConnection);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        for (var index = 1; index <= rowCount; index++)
        {
            dbContext.Widgets.Add(new Widget { Id = index, Name = $"Widget-{index}" });
        }
        await dbContext.SaveChangesAsync(cancellationToken);
        return dbContext;
    }

    /// <summary>Trivial entity used by these tests only.</summary>
    public sealed class Widget
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>DbContext that owns its SQLite connection so disposal closes it.</summary>
    public sealed class TestDbContext(DbContextOptions<ApplyPagedAsyncTests.TestDbContext> options, SqliteConnection ownedConnection) : DbContext(options)
    {
        private readonly SqliteConnection _ownedConnection = ownedConnection;

        public DbSet<Widget> Widgets => Set<Widget>();

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            await _ownedConnection.DisposeAsync();
        }
    }
}
