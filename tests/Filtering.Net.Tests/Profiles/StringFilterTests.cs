using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.Tests.Profiles;

public class StringFilterTests
{
    [Fact]
    public void TryGetArray_FromJsonArray_ReturnsAllStrings()
    {
        // Arrange
        var element = JsonDocument.Parse("[\"a\", \"b\"]").RootElement;

        // Act
        var success = StringFilter.TryGetArray(element, out var values, out var error);

        // Assert
        success.Should().BeTrue();
        values.Should().Equal(["a", "b"]);
        error.Should().Be(string.Empty);
    }

    [Fact]
    public void TryGetArray_WithANonStringElement_ReportsTheElementIndex()
    {
        // Arrange
        var element = JsonDocument.Parse("[\"a\", 3]").RootElement;

        // Act
        var success = StringFilter.TryGetArray(element, out _, out var error);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("Array element [1]").And.Contain("Expected JSON String");
    }
}
