using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.Tests.Profiles;

public class DateOnlyFilterTests
{
    [Fact]
    public void TryGetValue_FromIso8601DateString_ReturnsParsedDateOnly()
    {
        // Arrange
        var element = JsonDocument.Parse("\"2026-01-15\"").RootElement;

        // Act
        var success = DateOnlyFilter.TryGetValue(element, out var value, out var error);

        // Assert
        success.Should().BeTrue();
        value.Should().Be(new DateOnly(2026, 1, 15));
        error.Should().Be(string.Empty);
    }

    [Fact]
    public void TryGetValue_FromJsonNumber_ReturnsTypeError()
    {
        // Arrange
        var element = JsonDocument.Parse("20260115").RootElement;

        // Act
        var success = DateOnlyFilter.TryGetValue(element, out _, out var error);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("Expected JSON String for DateOnly");
    }

    [Fact]
    public void TryGetValue_FromUnparsableString_ReturnsFormatError()
    {
        // Arrange
        var element = JsonDocument.Parse("\"not-a-date\"").RootElement;

        // Act
        var success = DateOnlyFilter.TryGetValue(element, out _, out var error);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("is not a valid ISO 8601 DateOnly");
    }

    [Fact]
    public void Profile_Operators_CoverTheDeclaredComparisonSet()
    {
        // Act
        var operatorNames = DateOnlyFilter.Profile.Operators.Keys;

        // Assert
        operatorNames.Should().BeEquivalentTo(["eq", "ne", "gt", "gte", "lt", "lte", "isNull"]);
    }
}
