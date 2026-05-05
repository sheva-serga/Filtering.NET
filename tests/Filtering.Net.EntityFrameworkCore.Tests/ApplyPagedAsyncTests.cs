using AwesomeAssertions;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using Xunit;

namespace Filtering.Net.EntityFrameworkCore.Tests;

/// <summary>
/// Tests <see cref="FilteringEntityFrameworkExtensions.ApplyPagedAsync{T}"/> against a
/// SQLite in-memory database, using a hand-written <see cref="IFilterDefinition{TEntity}"/>
/// passthrough so we exercise the EF Core async path without depending on the source generator.
/// </summary>
public class ApplyPagedAsyncTests
{
    [Fact]
    public async Task ApplyPagedAsync_NoFilter_ReturnsAllRowsWithCorrectTotal()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = await CreateSeededDbContextAsync(rowCount: 7, cancellationToken);
        var passthroughDefinition = new PassthroughFilterDefinition();
        var request = new FilterRequest();

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable()
            .ApplyPagedAsync(passthroughDefinition, request, cancellationToken);

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
        var passthroughDefinition = new PassthroughFilterDefinition();
        var request = new FilterRequest { Page = 2, PageSize = 10 };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable()
            .ApplyPagedAsync(passthroughDefinition, request, cancellationToken);

        // Assert
        pageResult.TotalCount.Should().Be(25);
        pageResult.Items.Should().HaveCount(10);
        pageResult.Page.Should().Be(2);
        pageResult.PageSize.Should().Be(10);
        pageResult.HasNext.Should().BeTrue();
        pageResult.HasPrevious.Should().BeTrue();
    }

    [Fact]
    public async Task ApplyPagedAsync_Sort_OrdersBySpecifiedField()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = await CreateSeededDbContextAsync(rowCount: 5, cancellationToken);
        var passthroughDefinition = new PassthroughFilterDefinition();
        var request = new FilterRequest
        {
            Sort = [new SortItem("Id", SortDir.Desc)],
        };

        // Act
        var pageResult = await dbContext.Widgets.AsQueryable()
            .ApplyPagedAsync(passthroughDefinition, request, cancellationToken);

        // Assert
        pageResult.Items.Select(widget => widget.Id).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task ApplyPagedAsync_InvalidRequest_ThrowsFilterValidationException()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var dbContext = await CreateSeededDbContextAsync(rowCount: 3, cancellationToken);
        var failingDefinition = new AlwaysFailingFilterDefinition();
        var request = new FilterRequest();

        // Act
        var act = () => dbContext.Widgets.AsQueryable().ApplyPagedAsync(failingDefinition, request, cancellationToken);

        // Assert
        await act.Should().ThrowAsync<FilterValidationException>();
    }

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

    /// <summary>
    /// Passthrough <see cref="IFilterDefinition{TEntity}"/> implementation: never reports a
    /// validation error, ignores filter expressions, and translates a single Id-based sort.
    /// Lets the test exercise <see cref="FilteringEntityFrameworkExtensions.ApplyPagedAsync{T}"/>
    /// without depending on the source generator output.
    /// </summary>
    private sealed class PassthroughFilterDefinition : IFilterDefinition<Widget>
    {
        public FilterValidationResult Validate(FilterNode? where) => FilterValidationResult.Success;
        public FilterValidationResult Validate(IReadOnlyList<SortItem>? sortItems) => FilterValidationResult.Success;
        public FilterValidationResult Validate(int? page, int? pageSize) => FilterValidationResult.Success;
        public FilterValidationResult Validate(FilterRequest request) => FilterValidationResult.Success;
        public IQueryable<Widget> ApplyFilter(IQueryable<Widget> query, FilterNode? where) => query;

        public IQueryable<Widget> ApplySorting(
            IQueryable<Widget> query,
            IReadOnlyList<SortItem>? sortItems,
            int? page = null,
            int? pageSize = null)
        {
            var sortedQuery = query;
            if (sortItems is { Count: > 0 } && sortItems[0].Field == "Id")
            {
                sortedQuery = sortItems[0].Dir == SortDir.Desc
                    ? sortedQuery.OrderByDescending(widget => widget.Id)
                    : sortedQuery.OrderBy(widget => widget.Id);
            }
            else
            {
                sortedQuery = sortedQuery.OrderBy(widget => widget.Id);
            }

            if (page is not null && pageSize is not null)
            {
                var skipCount = (Math.Max(1, page.Value) - 1) * pageSize.Value;
                sortedQuery = sortedQuery.Skip(skipCount).Take(pageSize.Value);
            }
            return sortedQuery;
        }
    }

    /// <summary>Validation always fails — used to confirm <c>ApplyPagedAsync</c> rethrows as
    /// <see cref="FilterValidationException"/>.</summary>
    private sealed class AlwaysFailingFilterDefinition : IFilterDefinition<Widget>
    {
        private static readonly FilterValidationResult FailingResult =
            new([new FilterValidationError("$", FilterValidationCode.UnknownField, "boom")]);

        public FilterValidationResult Validate(FilterNode? where) => FailingResult;
        public FilterValidationResult Validate(IReadOnlyList<SortItem>? sortItems) => FailingResult;
        public FilterValidationResult Validate(int? page, int? pageSize) => FailingResult;
        public FilterValidationResult Validate(FilterRequest request) => FailingResult;
        public IQueryable<Widget> ApplyFilter(IQueryable<Widget> query, FilterNode? where) => query;
        public IQueryable<Widget> ApplySorting(IQueryable<Widget> query, IReadOnlyList<SortItem>? sortItems, int? page = null, int? pageSize = null) => query;
    }
}
