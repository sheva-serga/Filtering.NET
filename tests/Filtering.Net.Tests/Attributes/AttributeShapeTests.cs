using AwesomeAssertions;
using Xunit;

namespace Filtering.Net.Tests.Attributes;

public class AttributeShapeTests
{
    [Fact]
    public void GenerateFilterAttribute_Constructed_AssignsEntityType()
    {
        // Arrange
        var attribute = new GenerateFilterAttribute<string>();

        // Act + Assert
        attribute.EntityType.Should().Be<string>();
    }

    [Fact]
    public void MapAttribute_ConstructedWithPropertyNameOnly_LeavesNonRequiredPropertiesAtDefaults()
    {
        // Arrange
        var attribute = new MapAttribute("Name");

        // Act + Assert
        attribute.PropertyName.Should().Be("Name");
        attribute.Profile.Should().BeNull();
        attribute.Only.Should().BeNull();
        attribute.Except.Should().BeNull();
        attribute.Alias.Should().BeNull();
        attribute.Sortable.Should().BeFalse();
        attribute.DefaultSortDirection.Should().Be(SortDir.Asc);
    }

    [Fact]
    public void MapAttribute_ConstructedWithFullObjectInitializer_StoresAllValues()
    {
        // Arrange
        var attribute = new MapAttribute("Created")
        {
            Profile = typeof(string),
            Only = ["eq"],
            Sortable = true,
            DefaultSortDirection = SortDir.Desc,
            Alias = "c"
        };

        // Act + Assert
        attribute.Profile.Should().Be<string>();
        attribute.Only.Should().Equal("eq");
        attribute.Sortable.Should().BeTrue();
        attribute.DefaultSortDirection.Should().Be(SortDir.Desc);
        attribute.Alias.Should().Be("c");
    }

    [Fact]
    public void InterceptValueAttribute_ConstructedWithPropertyName_DefaultsToTypedMode()
    {
        // Arrange
        var attribute = new InterceptValueAttribute("Email");

        // Act + Assert
        attribute.PropertyName.Should().Be("Email");
        attribute.Raw.Should().BeFalse();
    }

    [Fact]
    public void PageSettingsAttribute_InitializedWithLimits_StoresLimits()
    {
        // Arrange
        var attribute = new PageSettingsAttribute { DefaultPageSize = 25, MaxPageSize = 200 };

        // Act + Assert
        attribute.DefaultPageSize.Should().Be(25);
        attribute.MaxPageSize.Should().Be(200);
    }

    [Fact]
    public void FilterProfileAttribute_InitializedWithBasedOn_StoresBaseProfile()
    {
        // Arrange
        var attribute = new FilterProfileAttribute<string> { BasedOn = typeof(string) };

        // Act + Assert
        attribute.BasedOn.Should().Be<string>();
    }

    [Fact]
    public void FilterOperatorAttribute_ConstructedWithName_StoresName()
    {
        // Arrange
        var attribute = new FilterOperatorAttribute("withinDays");

        // Act + Assert
        attribute.Name.Should().Be("withinDays");
    }

    [Fact]
    public void FilterValidatorAttribute_ConstructedWithOperator_StoresOperatorName()
    {
        // Arrange
        var attribute = new FilterValidatorAttribute("eq");

        // Act + Assert
        attribute.OperatorName.Should().Be("eq");
    }

    [Fact]
    public void PropertyMapAttribute_ConstructedWithPropertyName_StoresPropertyName()
    {
        // Arrange
        var attribute = new PropertyMapAttribute("Tags");

        // Act + Assert
        attribute.PropertyName.Should().Be("Tags");
    }

    [Fact]
    public void FilterDefaultsAttribute_InitializedWithLimits_StoresLimits()
    {
        // Arrange
        var attribute = new FilterDefaultsAttribute { DefaultPageSize = 50, MaxPageSize = 200, MaxNestingDepth = 10, MaxLeafConditions = 50 };

        // Act + Assert
        attribute.DefaultPageSize.Should().Be(50);
        attribute.MaxPageSize.Should().Be(200);
    }
}
