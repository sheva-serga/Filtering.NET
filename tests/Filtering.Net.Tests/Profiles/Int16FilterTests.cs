using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.Tests.Profiles;

public class Int16FilterTests
{
    [Fact]
    public void TryGetValue_FromJsonNumber_ReturnsParsedValue()
    {
        // Arrange
        var element = JsonDocument.Parse("32000").RootElement;

        // Act
        var success = Int16Filter.TryGetValue(element, out var value, out _);

        // Assert
        success.Should().BeTrue();
        value.Should().Be((short)32000);
    }

    [Fact]
    public void TryGetArray_FromJsonArray_ReturnsAllShorts()
    {
        // Arrange
        var element = JsonDocument.Parse("[1, 2, 3]").RootElement;

        // Act
        var success = Int16Filter.TryGetArray(element, out var values, out _);

        // Assert
        success.Should().BeTrue();
        values.Should().Equal([1, 2, 3]);
    }
}
