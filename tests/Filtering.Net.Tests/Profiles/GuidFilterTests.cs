using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.Tests.Profiles;

public class GuidFilterTests
{
    [Fact]
    public void TryGetArray_FromJsonArray_ReturnsAllGuids()
    {
        // Arrange
        var element = JsonDocument.Parse("[\"11111111-1111-1111-1111-111111111111\", \"22222222-2222-2222-2222-222222222222\"]").RootElement;

        // Act
        var success = GuidFilter.TryGetArray(element, out var values, out var error);

        // Assert
        success.Should().BeTrue();
        values.Should().Equal([new Guid("11111111-1111-1111-1111-111111111111"), new Guid("22222222-2222-2222-2222-222222222222")]);
        error.Should().Be(string.Empty);
    }

    [Fact]
    public void TryGetArray_FromNonArrayJson_ReturnsTheSameTypeErrorAsEveryOtherProfile()
    {
        // Arrange
        var element = JsonDocument.Parse("\"not-an-array\"").RootElement;

        // Act
        var success = GuidFilter.TryGetArray(element, out _, out var guidError);
        StringFilter.TryGetArray(element, out _, out var stringError);
        Int32Filter.TryGetArray(element, out _, out var intError);

        // Assert
        success.Should().BeFalse();
        guidError.Should().Be(stringError).And.Be(intError);
    }

    [Fact]
    public void TryGetArray_WithAnUnparsableElement_ReportsTheElementIndex()
    {
        // Arrange
        var element = JsonDocument.Parse("[\"11111111-1111-1111-1111-111111111111\", \"nope\"]").RootElement;

        // Act
        var success = GuidFilter.TryGetArray(element, out _, out var error);

        // Assert
        success.Should().BeFalse();
        error.Should().Contain("Array element [1]").And.Contain("is not a valid Guid");
    }
}
