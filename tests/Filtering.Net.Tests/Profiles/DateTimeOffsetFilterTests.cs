using System.Globalization;
using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.Tests.Profiles;

public class DateTimeOffsetFilterTests
{
    [Fact]
    public void TryGetValue_FromIso8601String_ReturnsParsedDateTimeOffset()
    {
        // Arrange
        var element = JsonDocument.Parse("\"2026-01-15T12:00:00Z\"").RootElement;
        var expected = DateTimeOffset.Parse(
            "2026-01-15T12:00:00Z",
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);

        // Act
        var success = DateTimeOffsetFilter.TryGetValue(element, out var value, out _);

        // Assert
        success.Should().BeTrue();
        value.Should().Be(expected);
    }
}
