using AwesomeAssertions;
using Xunit;

namespace Filtering.Net.Tests.Attributes;

public class FilterValueDiagnosticsAttributeTests
{
    [Fact]
    public void Constructor_Default_LeavesWarnUnregisteredFalse()
    {
        // Arrange
        var attribute = new FilterValueDiagnosticsAttribute();

        // Act + Assert
        attribute.WarnUnregistered.Should().BeFalse();
    }

    [Fact]
    public void WarnUnregistered_InitializedToTrue_PersistsValue()
    {
        // Arrange
        var attribute = new FilterValueDiagnosticsAttribute { WarnUnregistered = true };

        // Act + Assert
        attribute.WarnUnregistered.Should().BeTrue();
    }

    [Fact]
    public void AttributeUsage_DeclaredOnType_RestrictsToAssemblySingleNonInherited()
    {
        // Arrange
        var usage = typeof(FilterValueDiagnosticsAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .Single();

        // Act + Assert
        usage.ValidOn.Should().Be(AttributeTargets.Assembly);
        usage.AllowMultiple.Should().BeFalse();
        usage.Inherited.Should().BeFalse();
    }
}
