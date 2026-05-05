using AwesomeAssertions;
using Xunit;

namespace Filtering.Net.EntityFrameworkCore.Tests;

/// <summary>Smoke tests for the computed members on <see cref="PageResult{TItem}"/>.</summary>
public class PageResultTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(25, 10, 3)]
    public void TotalPages_RoundsUp(int totalCount, int pageSize, int expectedTotalPages)
    {
        // Arrange
        var pageResult = new PageResult<string>([], totalCount, Page: 1, pageSize);

        // Act + Assert
        pageResult.TotalPages.Should().Be(expectedTotalPages);
    }

    [Fact]
    public void TotalPages_WhenPageSizeIsZero_ReturnsOne()
    {
        // Arrange
        var pageResult = new PageResult<string>([], TotalCount: 5, Page: 1, PageSize: 0);

        // Act
        var totalPages = pageResult.TotalPages;

        // Assert
        totalPages.Should().Be(1);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(99, true)]
    public void HasPrevious_WhenPageGreaterThanOne_ReturnsTrue(int page, bool expectedHasPrevious)
    {
        // Arrange
        var pageResult = new PageResult<string>([], TotalCount: 100, page, PageSize: 10);

        // Act + Assert
        pageResult.HasPrevious.Should().Be(expectedHasPrevious);
    }

    public static TheoryData<int, int, int, bool> HasNext_Cases => new()
    {
        { 1, 100, 10, true },
        { 9, 100, 10, true },
        { 10, 100, 10, false },
        { 11, 100, 10, false },
    };

    [Theory]
    [MemberData(nameof(HasNext_Cases))]
    public void HasNext_WhenMorePagesAvailable_ReturnsTrue(int page, int totalCount, int pageSize, bool expectedHasNext)
    {
        // Arrange
        var pageResult = new PageResult<string>([], totalCount, page, pageSize);

        // Act
        var hasNext = pageResult.HasNext;

        // Assert
        hasNext.Should().Be(expectedHasNext);
    }
}
