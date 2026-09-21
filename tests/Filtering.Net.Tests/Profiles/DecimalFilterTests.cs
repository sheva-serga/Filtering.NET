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
    public void TryGetValue_FromExponentString_MatchesTheEquivalentJsonNumber()
    {
        // Arrange — clients that send decimals as strings to avoid float rounding still use scientific notation.
        var stringElement = JsonDocument.Parse("\"1E+5\"").RootElement;
        var numberElement = JsonDocument.Parse("1e5").RootElement;

        // Act
        var stringSuccess = DecimalFilter.TryGetValue(stringElement, out var fromString, out var error);
        DecimalFilter.TryGetValue(numberElement, out var fromNumber, out _);

        // Assert
        stringSuccess.Should().BeTrue(because: error);
        fromString.Should().Be(fromNumber);
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
