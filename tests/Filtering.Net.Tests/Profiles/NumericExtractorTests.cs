using System.Globalization;
using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.Tests.Profiles;

public class NumericExtractorTests
{
    [Fact]
    public void TryGetValue_FromJsonNumber_ReturnsParsedValue()
    {
        // Arrange
        var element = JsonDocument.Parse("42").RootElement;

        // Act
        var success = NumericExtractor.TryGetValue(
            element,
            (JsonElement e, out int v) => e.TryGetInt32(out v),
            (string s, out int v) => int.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out v),
            "int",
            out var value,
            out var error);

        // Assert
        success.Should().BeTrue();
        value.Should().Be(42);
        error.Should().Be(string.Empty);
    }

    [Fact]
    public void TryGetValue_FromJsonString_ReturnsParsedValue()
    {
        // Arrange
        var element = JsonDocument.Parse("\"42\"").RootElement;

        // Act
        var success = NumericExtractor.TryGetValue(
            element,
            (JsonElement e, out int v) => e.TryGetInt32(out v),
            (string s, out int v) => int.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out v),
            "int",
            out var value,
            out _);

        // Assert
        success.Should().BeTrue();
        value.Should().Be(42);
    }

    [Fact]
    public void TryGetValue_FromJsonBoolean_ReturnsTypeError()
    {
        // Arrange
        var element = JsonDocument.Parse("true").RootElement;

        // Act
        var success = NumericExtractor.TryGetValue(
            element,
            (JsonElement e, out int v) => e.TryGetInt32(out v),
            (string s, out int v) => int.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out v),
            "int",
            out _,
            out var error);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("Expected JSON Number or String for int");
    }

    [Fact]
    public void TryGetArray_FromJsonArray_ReturnsAllElements()
    {
        // Arrange
        var element = JsonDocument.Parse("[1, 2, 3]").RootElement;

        // Act
        var success = NumericExtractor.TryGetArray(
            element,
            (JsonElement el, out int v, out string err) =>
                NumericExtractor.TryGetValue(
                    el,
                    (JsonElement e2, out int x) => e2.TryGetInt32(out x),
                    (string s, out int x) => int.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out x),
                    "int", out v, out err),
            out var values,
            out var error);

        // Assert
        success.Should().BeTrue();
        values.Should().Equal([1, 2, 3]);
        error.Should().Be(string.Empty);
    }

    [Fact]
    public void TryGetArray_FromNonArrayJson_ReturnsTypeError()
    {
        // Arrange
        var element = JsonDocument.Parse("42").RootElement;

        // Act
        var success = NumericExtractor.TryGetArray(
            element,
            (JsonElement el, out int v, out string err) => { v = 0; err = ""; return true; },
            out _,
            out var error);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("Expected JSON Array");
    }
}
