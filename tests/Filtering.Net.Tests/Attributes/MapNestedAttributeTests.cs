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
        };

        // Assert
        attribute.Prefix.Should().Be("dept");
        attribute.Only.Should().BeEquivalentTo(new[] { "id", "name" });
        attribute.Except.Should().BeEquivalentTo(new[] { "internal" });
        attribute.DisableSorting.Should().BeTrue();
    }

    [Fact]
    public void GenericArity_StoresNavigationPropertyName()
    {
        // Arrange / Act
        var attribute = new MapNestedAttribute<MapNestedAttributeTests>("Department");

        // Assert
        attribute.NavigationPropertyName.Should().Be("Department");
    }
}
