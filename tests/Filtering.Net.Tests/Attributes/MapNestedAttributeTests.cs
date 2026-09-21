using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.Tests.Attributes;

public class MapNestedAttributeTests
{
    [Fact]
    public void Ctor_StoresNavigationPropertyName()
    {
        // Arrange / Act
        var attribute = new MapNestedAttribute("Department");

        // Assert
        attribute.NavigationPropertyName.Should().Be("Department");
        attribute.Prefix.Should().BeNull();
        attribute.Only.Should().BeNull();
        attribute.Except.Should().BeNull();
        attribute.DisableSorting.Should().BeFalse();
        // Zero is the documented "unbounded" value that FilterNestingContext.TryEnter relies on.
        attribute.MaxDepth.Should().Be(0);
    }

    [Fact]
    public void InitProperties_SetIndependently()
    {
        // Arrange / Act
        var attribute = new MapNestedAttribute("Department")
        {
            Prefix = "dept",
            Only = new[] { "id", "name" },
            Except = new[] { "internal" },
            DisableSorting = true,
            MaxDepth = 3,
        };

        // Assert
        attribute.Prefix.Should().Be("dept");
        attribute.Only.Should().BeEquivalentTo(new[] { "id", "name" });
        attribute.Except.Should().BeEquivalentTo(new[] { "internal" });
        attribute.DisableSorting.Should().BeTrue();
        attribute.MaxDepth.Should().Be(3);
    }

    [Fact]
    public void GenericArity_Ctor_StoresNavigationPropertyNameAndLeavesTheRestAtDefaults()
    {
        // Arrange / Act
        var attribute = new MapNestedAttribute<MapNestedAttributeTests>("Department");

        // Assert
        attribute.NavigationPropertyName.Should().Be("Department");
        attribute.Prefix.Should().BeNull();
        attribute.Only.Should().BeNull();
        attribute.Except.Should().BeNull();
        attribute.DisableSorting.Should().BeFalse();
        attribute.MaxDepth.Should().Be(0);
    }

    [Fact]
    public void GenericArity_InitProperties_SetIndependently()
    {
        // Arrange / Act — the generic arity carries the same knobs; dropping one here would be a
        // compile error for consumers who use it.
        var attribute = new MapNestedAttribute<MapNestedAttributeTests>("Department")
        {
            Prefix = "dept",
            Only = new[] { "id", "name" },
            Except = new[] { "internal" },
            DisableSorting = true,
            MaxDepth = 3,
        };

        // Assert
        attribute.Prefix.Should().Be("dept");
        attribute.Only.Should().BeEquivalentTo(new[] { "id", "name" });
        attribute.Except.Should().BeEquivalentTo(new[] { "internal" });
        attribute.DisableSorting.Should().BeTrue();
        attribute.MaxDepth.Should().Be(3);
    }
}
