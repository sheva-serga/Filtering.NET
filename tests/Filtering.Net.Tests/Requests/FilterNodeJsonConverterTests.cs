using AwesomeAssertions;
using System.Text.Json;
using Xunit;

namespace Filtering.Net.Tests.Requests;

public class FilterNodeJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void Read_LeafShape_ProducesFilterLeaf()
    {
        // Arrange
        var json = """{ "field": "name", "op": "contains", "value": "john" }""";

        // Act
        var node = JsonSerializer.Deserialize<FilterNode>(json, Options);

        // Assert
        node.Should().BeOfType<FilterLeaf>();
        var leaf = (FilterLeaf)node!;
        leaf.Field.Should().Be("name");
        leaf.Operator.Should().Be("contains");
        leaf.Value.GetString().Should().Be("john");
    }

    [Fact]
    public void Read_AndGroup_ProducesFilterGroupAnd()
    {
        // Arrange
        var json = """{ "and": [{ "field": "a", "op": "eq", "value": 1 }, { "field": "b", "op": "eq", "value": 2 }] }""";

        // Act
        var node = JsonSerializer.Deserialize<FilterNode>(json, Options);

        // Assert
        node.Should().BeOfType<FilterGroup>();
        var group = (FilterGroup)node!;
        group.Op.Should().Be(LogicalOp.And);
        group.Children.Should().HaveCount(2);
    }

    [Fact]
    public void Read_OrGroup_ProducesFilterGroupOr()
    {
        // Arrange
        var json = """{ "or": [{ "field": "a", "op": "eq", "value": 1 }] }""";

        // Act + Assert
        ((FilterGroup)JsonSerializer.Deserialize<FilterNode>(json, Options)!).Op.Should().Be(LogicalOp.Or);
    }

    [Fact]
    public void Read_NotGroup_RequiresExactlyOneChild()
    {
        // Arrange
        var jsonValid = """{ "not": [{ "field": "a", "op": "eq", "value": 1 }] }""";
        var jsonInvalid = """{ "not": [{ "field": "a", "op": "eq", "value": 1 }, { "field": "b", "op": "eq", "value": 2 }] }""";

        // Act
        var node = JsonSerializer.Deserialize<FilterNode>(jsonValid, Options);
        var act = () => JsonSerializer.Deserialize<FilterNode>(jsonInvalid, Options);

        // Assert
        ((FilterGroup)node!).Op.Should().Be(LogicalOp.Not);
        ((FilterGroup)node!).Children.Should().HaveCount(1);
        act.Should().Throw<JsonException>().WithMessage("*not*exactly one*");
    }

    [Fact]
    public void Read_BothAndAndOr_Throws()
    {
        // Arrange
        var json = """{ "and": [], "or": [] }""";

        // Act
        var act = () => JsonSerializer.Deserialize<FilterNode>(json, Options);

        // Assert
        act.Should().Throw<JsonException>().WithMessage("*exactly one of*");
    }

    [Fact]
    public void Read_GroupAndLeafShape_Throws()
    {
        // Arrange
        var json = """{ "and": [], "field": "x" }""";

        // Act
        var act = () => JsonSerializer.Deserialize<FilterNode>(json, Options);

        // Assert
        act.Should().Throw<JsonException>().WithMessage("*ambiguous*");
    }

    [Fact]
    public void Read_NoDiscriminator_Throws()
    {
        // Arrange
        var json = """{ "value": 1 }""";

        // Act
        var act = () => JsonSerializer.Deserialize<FilterNode>(json, Options);

        // Assert
        act.Should().Throw<JsonException>().WithMessage("*requires either*");
    }

    [Fact]
    public void Write_LeafProducesExpectedShape()
    {
        // Arrange
        var leaf = new FilterLeaf("name", "contains", JsonDocument.Parse("\"john\"").RootElement);

        // Act
        var json = JsonSerializer.Serialize<FilterNode>(leaf, Options);

        // Assert
        json.Should().Be("""{"field":"name","op":"contains","value":"john"}""");
    }

    [Fact]
    public void Write_GroupProducesExpectedShape()
    {
        // Arrange
        var leaf = new FilterLeaf("a", "eq", JsonDocument.Parse("1").RootElement);
        var group = new FilterGroup(LogicalOp.And, [leaf]);

        // Act
        var json = JsonSerializer.Serialize<FilterNode>(group, Options);

        // Assert
        json.Should().Be("""{"and":[{"field":"a","op":"eq","value":1}]}""");
    }

    [Fact]
    public void RoundTrip_PreservesNestedStructure()
    {
        // Arrange
        var original = new FilterGroup(LogicalOp.And,
        [
            new FilterLeaf("name", "contains", JsonDocument.Parse("\"x\"").RootElement),
            new FilterGroup(LogicalOp.Or,
            [
                new FilterLeaf("status", "eq", JsonDocument.Parse("\"a\"").RootElement),
                new FilterLeaf("status", "eq", JsonDocument.Parse("\"b\"").RootElement)
            ])
        ]);

        // Act
        var json = JsonSerializer.Serialize<FilterNode>(original, Options);
        var roundTripped = JsonSerializer.Deserialize<FilterNode>(json, Options);

        // Assert
        roundTripped.Should().BeOfType<FilterGroup>();
    }
}
