using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.Tests.Profiles;

public class Int64FilterTests
{
    [Fact]
    public void TryGetValue_FromJsonNumber_ReturnsParsedValue()
    {
        // Arrange
        var element = JsonDocument.Parse("9999999999").RootElement;

        // Act
        var success = Int64Filter.TryGetValue(element, out var value, out _);

        // Assert
        success.Should().BeTrue();
        value.Should().Be(9999999999L);
    }

    [Fact]
    public void TryGetArray_FromJsonArray_ReturnsAllLongs()
    {
        // Arrange
        var element = JsonDocument.Parse("[1, 2, 3]").RootElement;

        // Act
        var success = Int64Filter.TryGetArray(element, out var values, out _);

        // Assert
        success.Should().BeTrue();
        values.Should().Equal([1L, 2L, 3L]);
    }
}
