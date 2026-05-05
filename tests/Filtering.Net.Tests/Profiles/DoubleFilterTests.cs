using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.Tests.Profiles;

public class DoubleFilterTests
{
    [Fact]
    public void TryGetValue_FromJsonNumber_ReturnsParsedValue()
    {
        // Arrange
        var element = JsonDocument.Parse("3.14").RootElement;

        // Act
        var success = DoubleFilter.TryGetValue(element, out var value, out _);

        // Assert
        success.Should().BeTrue();
        value.Should().Be(3.14);
    }

    [Fact]
    public void TryGetArray_FromJsonArray_ReturnsAllDoubles()
    {
        // Arrange
        var element = JsonDocument.Parse("[1.0, 2.0]").RootElement;

        // Act
        var success = DoubleFilter.TryGetArray(element, out var values, out _);

        // Assert
        success.Should().BeTrue();
        values.Should().Equal([1.0, 2.0]);
    }
}
