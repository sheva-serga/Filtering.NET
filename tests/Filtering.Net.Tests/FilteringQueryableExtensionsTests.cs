using AwesomeAssertions;
using Xunit;

namespace Filtering.Net.Tests;

public class FilteringQueryableExtensionsTests
{
    public record TestEntity(int Id, string Name);

    private sealed class FakeNameFilter : IFilterDefinition<TestEntity>
    {
        public FilterValidationResult Validate(FilterNode? where) => FilterValidationResult.Success;
        public FilterValidationResult Validate(IReadOnlyList<SortItem>? sortItems) => FilterValidationResult.Success;
        public FilterValidationResult Validate(int? page, int? pageSize) => FilterValidationResult.Success;
        public FilterValidationResult Validate(FilterRequest request) => FilterValidationResult.Success;

        public IQueryable<TestEntity> ApplyFilter(IQueryable<TestEntity> query, FilterNode? where)
        {
            if (where is FilterLeaf leaf && leaf.Field == "name" && leaf.Operator == "eq")
            {
                var nameValue = leaf.Value.GetString();
                return query.Where(entity => entity.Name == nameValue);
            }
            return query;
        }

        public IQueryable<TestEntity> ApplySorting(IQueryable<TestEntity> query, IReadOnlyList<SortItem>? sortItems, int? page = null, int? pageSize = null)
        {
            var sortedQuery = (sortItems is { Count: > 0 } && sortItems[0].Field == "name")
                ? query.OrderBy(entity => entity.Name)
                : query;
            return (page is not null || pageSize is not null)
                ? sortedQuery.Skip(((page ?? 1) - 1) * (pageSize ?? 50)).Take(pageSize ?? 50)
                : sortedQuery;
        }
    }

    private sealed class AlwaysFailValidator : IFilterDefinition<TestEntity>
    {
        public FilterValidationResult Validate(FilterNode? where) => FilterValidationResult.Success;
        public FilterValidationResult Validate(IReadOnlyList<SortItem>? sortItems) => FilterValidationResult.Success;
        public FilterValidationResult Validate(int? page, int? pageSize) => FilterValidationResult.Success;
        public FilterValidationResult Validate(FilterRequest request) =>
            new([new FilterValidationError("path", FilterValidationCode.UnknownField, "nope")]);
        public IQueryable<TestEntity> ApplyFilter(IQueryable<TestEntity> query, FilterNode? where) => query;
        public IQueryable<TestEntity> ApplySorting(IQueryable<TestEntity> query, IReadOnlyList<SortItem>? sortItems, int? page = null, int? pageSize = null) => query;
    }

    [Fact]
    public void Apply_FullRequest_FiltersSortsAndPages()
    {
        // Arrange
        var data = new[] { new TestEntity(1, "Charlie"), new TestEntity(2, "Bob"), new TestEntity(3, "Alice") };
        var request = new FilterRequest
        {
            Sort = [new SortItem("name")],
            Page = 1,
            PageSize = 2
        };

        // Act
        var result = data.AsQueryable().Apply(new FakeNameFilter(), request).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Alice");
    }

    [Fact]
    public void Apply_InvalidRequest_ThrowsFilterValidationException()
    {
        // Arrange
        var data = new[] { new TestEntity(1, "x") }.AsQueryable();
        var request = new FilterRequest();

        // Act
        var act = () => data.Apply(new AlwaysFailValidator(), request);

        // Assert
        act.Should().Throw<FilterValidationException>()
            .Which.Result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Apply_NoSortNoPage_ReturnsFilteredOnly()
    {
        // Arrange
        var data = new[] { new TestEntity(1, "Bob"), new TestEntity(2, "Alice") };
        var request = new FilterRequest();

        // Act
        var result = data.AsQueryable().Apply(new FakeNameFilter(), request).ToList();

        // Assert
        result.Should().HaveCount(2);
    }
}
