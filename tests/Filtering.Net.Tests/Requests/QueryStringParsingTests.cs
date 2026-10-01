using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.Tests.Requests;

public class QueryStringParsingTests
{
    [Fact]
    public void SortItemTryParse_FieldOnly_ReturnsItemWithoutDirection()
    {
        // Act
        var parsed = SortItem.TryParse("createdAt", null, out var sortItem);

        // Assert
        parsed.Should().BeTrue();
        sortItem.Should().Be(new SortItem("createdAt"));
    }

    [Theory]
    [InlineData("name:asc", SortDir.Asc)]
    [InlineData("name:DESC", SortDir.Desc)]
    [InlineData("name:Desc", SortDir.Desc)]
    public void SortItemTryParse_DirectionSuffixInAnyCase_ReturnsDirection(string value, SortDir expectedDirection)
    {
        // Act
        var parsed = SortItem.TryParse(value, null, out var sortItem);

        // Assert
        parsed.Should().BeTrue();
        sortItem.Should().Be(new SortItem("name", expectedDirection));
    }

    [Fact]
    public void SortItemTryParse_ColonInsideField_SplitsOnLastColon()
    {
        // Act
        var parsed = SortItem.TryParse("meta:key:desc", null, out var sortItem);

        // Assert
        parsed.Should().BeTrue();
        sortItem.Should().Be(new SortItem("meta:key", SortDir.Desc));
    }

    [Theory]
    [InlineData("a:b")]
    [InlineData("name:up")]
    [InlineData(":desc")]
    [InlineData("  :asc")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void SortItemTryParse_InvalidItem_ReturnsFalse(string? value)
    {
        // Act
        var parsed = SortItem.TryParse(value, null, out var sortItem);

        // Assert
        parsed.Should().BeFalse();
        sortItem.Should().BeNull();
    }

    [Fact]
    public void FilterNodeTryParse_Leaf_ReturnsLeaf()
    {
        // Act
        var parsed = FilterNode.TryParse("""{"field":"age","op":"gte","value":18}""", null, out var filterNode);

        // Assert
        parsed.Should().BeTrue();
        var leaf = filterNode.Should().BeOfType<FilterLeaf>().Subject;
        leaf.Field.Should().Be("age");
        leaf.Operator.Should().Be("gte");
        leaf.Value.GetRawText().Should().Be("18");
    }

    [Fact]
    public void FilterNodeTryParse_NestedGroup_ReturnsGroupTree()
    {
        // Act
        var parsed = FilterNode.TryParse(
            """{"or":[{"and":[{"field":"a","op":"eq","value":1}]},{"not":[{"field":"b","op":"isNull"}]}]}""",
            null,
            out var filterNode);

        // Assert
        parsed.Should().BeTrue();
        var orGroup = filterNode.Should().BeOfType<FilterGroup>().Subject;
        orGroup.Op.Should().Be(LogicalOp.Or);
        orGroup.Children.Should().HaveCount(2);
        orGroup.Children[1].Should().BeOfType<FilterGroup>().Which.Op.Should().Be(LogicalOp.Not);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("not json")]
    [InlineData("5")]
    [InlineData("[]")]
    [InlineData("\"x\"")]
    [InlineData("null")]
    [InlineData("""{"field":"a","op":"isNull"} x""")]
    [InlineData("""{"field":"a","op":"isNull"}{"field":"b","op":"isNull"}""")]
    [InlineData("""{"and":[],"field":"a"}""")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void FilterNodeTryParse_InvalidInput_ReturnsFalse(string? value)
    {
        // Act
        var parsed = FilterNode.TryParse(value, null, out var filterNode);

        // Assert
        parsed.Should().BeFalse();
        filterNode.Should().BeNull();
    }

    [Fact]
    public void FilterQueryToRequest_AllParts_CopiesEveryPart()
    {
        // Arrange
        _ = FilterNode.TryParse("""{"field":"a","op":"isNull"}""", null, out var where);
        var filterQuery = new FilterQuery(where, [new SortItem("a", SortDir.Desc)], 2, 25);

        // Act
        var filterRequest = filterQuery.ToRequest();

        // Assert
        filterRequest.Where.Should().BeSameAs(where);
        filterRequest.Sort.Should().Equal(new SortItem("a", SortDir.Desc));
        filterRequest.Page.Should().Be(2);
        filterRequest.PageSize.Should().Be(25);
    }

    [Fact]
    public void FilterQueryToRequest_NoParts_ReturnsEmptyRequest()
    {
        // Act
        var filterRequest = new FilterQuery(null, null, null, null).ToRequest();

        // Assert
        filterRequest.Should().Be(new FilterRequest());
    }

    [Fact]
    public void FilterQueryToRequest_EmptySort_ReturnsNullSort()
    {
        // Act
        var filterRequest = new FilterQuery(null, [], null, null).ToRequest();

        // Assert
        filterRequest.Sort.Should().BeNull();
    }
}
