using AwesomeAssertions;
using System.Text.Json;
using Xunit;

namespace Filtering.Net.Tests.Requests;

public class RequestTypesTests
{
    [Fact]
    public void FilterRequest_DefaultValues_AreNull()
    {
        // Act + Assert
        var request = new FilterRequest();
        request.Where.Should().BeNull();
        request.Sort.Should().BeNull();
        request.Page.Should().BeNull();
        request.PageSize.Should().BeNull();
    }

    [Fact]
    public void FilterLeaf_RequiresFieldOperatorValue()
    {
        // Act + Assert
        var leaf = new FilterLeaf("name", "contains", JsonDocument.Parse("\"john\"").RootElement);
        leaf.Field.Should().Be("name");
        leaf.Operator.Should().Be("contains");
        leaf.Value.GetString().Should().Be("john");
    }

    [Fact]
    public void FilterGroup_HoldsLogicalOpAndChildren()
    {
        // Arrange
        var leaf1 = new FilterLeaf("a", "eq", JsonDocument.Parse("1").RootElement);
        var leaf2 = new FilterLeaf("b", "eq", JsonDocument.Parse("2").RootElement);

        // Act
        var group = new FilterGroup(LogicalOp.And, [leaf1, leaf2]);

        // Assert
        group.Op.Should().Be(LogicalOp.And);
        group.Children.Should().HaveCount(2);
    }

    [Fact]
    public void SortItem_DefaultsToAsc()
    {
        // Act + Assert
        var item = new SortItem("name");
        item.Field.Should().Be("name");
        item.Dir.Should().Be(SortDir.Asc);
    }
}
