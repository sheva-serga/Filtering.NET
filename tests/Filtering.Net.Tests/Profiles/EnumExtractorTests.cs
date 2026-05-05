using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.Tests.Profiles;

public enum SampleStatus { Active = 1, Closed = 2 }

public class EnumExtractorTests
{
    [Fact]
    public void TryGetValue_FromJsonString_ReturnsParsedEnum()
    {
        // Arrange
        var element = JsonDocument.Parse("\"Active\"").RootElement;

        // Act
        var success = EnumExtractor.TryGetValue<SampleStatus>(element, out var value, out _);

        // Assert
        success.Should().BeTrue();
        value.Should().Be(SampleStatus.Active);
    }

    [Fact]
    public void TryGetValue_FromJsonNumber_ReturnsParsedEnum()
    {
        // Arrange
        var element = JsonDocument.Parse("2").RootElement;

        // Act
        var success = EnumExtractor.TryGetValue<SampleStatus>(element, out var value, out _);

        // Assert
        success.Should().BeTrue();
        value.Should().Be(SampleStatus.Closed);
    }

    [Fact]
    public void TryGetValue_FromUnrecognizedStringValue_ReturnsError()
    {
        // Arrange
        var element = JsonDocument.Parse("\"Bogus\"").RootElement;

        // Act
        var success = EnumExtractor.TryGetValue<SampleStatus>(element, out _, out var error);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("not a valid member");
    }

    [Fact]
    public void TryGetArray_FromJsonArray_ReturnsAllParsedEnums()
    {
        // Arrange
        var element = JsonDocument.Parse("[\"Active\", \"Closed\"]").RootElement;

        // Act
        var success = EnumExtractor.TryGetArray<SampleStatus>(element, out var values, out _);

        // Assert
        success.Should().BeTrue();
        values.Should().Equal([SampleStatus.Active, SampleStatus.Closed]);
    }
}
