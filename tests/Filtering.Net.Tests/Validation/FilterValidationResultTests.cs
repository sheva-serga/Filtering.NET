using AwesomeAssertions;
using Xunit;

namespace Filtering.Net.Tests.Validation;

public class FilterValidationResultTests
{
    [Fact]
    public void Success_StaticSingleton_IsValidWithNoErrors()
    {
        // Act
        var isValid = FilterValidationResult.Success.IsValid;
        var errors = FilterValidationResult.Success.Errors;

        // Assert
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_WithNonEmptyErrorList_IsNotValidAndExposesErrors()
    {
        // Arrange
        var errors = new[]
        {
            new FilterValidationError("path", FilterValidationCode.UnknownField, "msg")
        };

        // Act
        var result = new FilterValidationResult(errors);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().BeEquivalentTo(errors);
    }

    [Fact]
    public void Constructor_WithEmptyErrorList_IsValid()
    {
        // Act
        var result = new FilterValidationResult([]);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
