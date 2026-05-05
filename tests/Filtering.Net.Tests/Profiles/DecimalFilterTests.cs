using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.Tests.Profiles;

public class DecimalFilterTests
{
    [Fact]
    public void TryGetValue_FromJsonNumber_ReturnsParsedValue()
    {
        // Arrange
        var element = JsonDocument.Parse("123.45").RootElement;

        // Act
        var success = DecimalFilter.TryGetValue(element, out var value, out _);

        // Assert
        success.Should().BeTrue();
        value.Should().Be(123.45m);
    }

    [Fact]
    public void TryGetArray_FromJsonArray_ReturnsAllDecimals()
    {
        // Arrange
        var element = JsonDocument.Parse("[1.5, 2.5]").RootElement;

        // Act
        var success = DecimalFilter.TryGetArray(element, out var values, out _);

        // Assert
        success.Should().BeTrue();
        values.Should().Equal([1.5m, 2.5m]);
    }
}
