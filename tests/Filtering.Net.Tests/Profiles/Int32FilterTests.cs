using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.Tests.Profiles;

public class Int32FilterTests
{
    [Fact]
    public void TryGetValue_FromJsonNumber_ReturnsParsedValue()
    {
        // Arrange
        var element = JsonDocument.Parse("42").RootElement;

        // Act
        var success = Int32Filter.TryGetValue(element, out var value, out var error);

        // Assert
        success.Should().BeTrue();
        value.Should().Be(42);
        error.Should().Be(string.Empty);
    }

    [Fact]
    public void TryGetValue_ValueExceedsIntMaxValue_ReturnsOutOfRangeError()
    {
        // Arrange
        var element = JsonDocument.Parse("9999999999").RootElement; // > int.MaxValue

        // Act
        var success = Int32Filter.TryGetValue(element, out _, out var error);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("out of range for int");
    }

    [Fact]
    public void TryGetArray_FromJsonArray_ReturnsAllInts()
    {
        // Arrange
        var element = JsonDocument.Parse("[1, 2, 3]").RootElement;

        // Act
        var success = Int32Filter.TryGetArray(element, out var values, out _);

        // Assert
        success.Should().BeTrue();
        values.Should().Equal([1, 2, 3]);
    }
}
