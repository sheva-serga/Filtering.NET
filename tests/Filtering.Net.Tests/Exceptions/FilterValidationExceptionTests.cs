using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.Tests.Exceptions;

public class FilterValidationExceptionTests
{
    [Fact]
    public void Constructor_SingleError_StoresResultAndBuildsMessageWithErrorDetails()
    {
        // Arrange
        var validationResult = new FilterValidationResult([
            new FilterValidationError("where.field", FilterValidationCode.UnknownField, "Field 'x' unknown.")
        ]);

        // Act
        var exception = new FilterValidationException(validationResult);

        // Assert
        exception.Result.Should().BeSameAs(validationResult);
        exception.Message.Should().Contain("1 error");
        exception.Message.Should().Contain("where.field");
        exception.Message.Should().Contain("Field 'x' unknown.");
    }

    [Fact]
    public void Constructor_MultipleErrors_BuildsMessageWithPluralErrorCount()
    {
        // Arrange
        var validationResult = new FilterValidationResult([
            new FilterValidationError("a", FilterValidationCode.UnknownField, "x"),
            new FilterValidationError("b", FilterValidationCode.OperatorNotAllowed, "y")
        ]);

        // Act
        var exception = new FilterValidationException(validationResult);

        // Assert
        exception.Message.Should().Contain("2 errors");
    }
}
