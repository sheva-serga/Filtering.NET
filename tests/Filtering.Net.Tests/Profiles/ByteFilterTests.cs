using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.Tests.Profiles;

public class ByteFilterTests
{
    [Fact]
    public void TryGetValue_FromJsonNumber_ReturnsParsedValue()
    {
        // Arrange
        var element = JsonDocument.Parse("200").RootElement;

        // Act
        var success = ByteFilter.TryGetValue(element, out var value, out _);

        // Assert
        success.Should().BeTrue();
        value.Should().Be((byte)200);
    }

    [Fact]
    public void TryGetArray_FromJsonArray_ReturnsAllBytes()
    {
        // Arrange
        var element = JsonDocument.Parse("[1, 2, 3]").RootElement;

        // Act
        var success = ByteFilter.TryGetArray(element, out var values, out _);

        // Assert
        success.Should().BeTrue();
        values.Should().Equal([1, 2, 3]);
    }
}
