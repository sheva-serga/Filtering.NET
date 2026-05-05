using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.Tests.Profiles;

public class SingleFilterTests
{
    [Fact]
    public void TryGetValue_FromJsonNumber_ReturnsParsedValue()
    {
        // Arrange
        var element = JsonDocument.Parse("1.5").RootElement;

        // Act
        var success = SingleFilter.TryGetValue(element, out var value, out _);

        // Assert
        success.Should().BeTrue();
        value.Should().Be(1.5f);
    }

    [Fact]
    public void TryGetArray_FromJsonArray_ReturnsAllSingles()
    {
        // Arrange
        var element = JsonDocument.Parse("[1.0, 2.0]").RootElement;

        // Act
        var success = SingleFilter.TryGetArray(element, out var values, out _);

        // Assert
        success.Should().BeTrue();
        values.Should().Equal([1.0f, 2.0f]);
    }
}
