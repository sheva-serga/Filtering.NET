using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.Tests.Profiles;

public class TimeOnlyFilterTests
{
    [Fact]
    public void TryGetValue_FromIso8601TimeString_ReturnsParsedTimeOnly()
    {
        // Arrange
        var element = JsonDocument.Parse("\"13:45:30\"").RootElement;

        // Act
        var success = TimeOnlyFilter.TryGetValue(element, out var value, out var error);

        // Assert
        success.Should().BeTrue();
        value.Should().Be(new TimeOnly(13, 45, 30));
        error.Should().Be(string.Empty);
    }

    [Fact]
    public void TryGetValue_FromJsonNumber_ReturnsTypeError()
    {
        // Arrange
        var element = JsonDocument.Parse("134530").RootElement;

        // Act
        var success = TimeOnlyFilter.TryGetValue(element, out _, out var error);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("Expected JSON String for TimeOnly");
    }

    [Fact]
    public void TryGetValue_FromUnparsableString_ReturnsFormatError()
    {
        // Arrange
        var element = JsonDocument.Parse("\"not-a-time\"").RootElement;

        // Act
        var success = TimeOnlyFilter.TryGetValue(element, out _, out var error);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("is not a valid ISO 8601 TimeOnly");
    }

    [Fact]
    public void Profile_Operators_CoverTheDeclaredComparisonSet()
    {
        // Act
        var operatorNames = TimeOnlyFilter.Profile.Operators.Keys;

        // Assert
        operatorNames.Should().BeEquivalentTo(["eq", "ne", "gt", "gte", "lt", "lte", "isNull"]);
    }
}
