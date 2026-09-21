using System.Text.Json;

using AwesomeAssertions;

using Xunit;

namespace Filtering.Net.Tests.Requests;

public class EnumWireFormatTests
{
    private static readonly JsonSerializerOptions WebOptions = new(JsonSerializerDefaults.Web);

    [Theory]
    [InlineData("\"asc\"", SortDir.Asc)]
    [InlineData("\"Desc\"", SortDir.Desc)]
    [InlineData("1", SortDir.Desc)]
    public void Deserialize_SortItemDirectionAsNameOrNumber_BindsTheDirection(string directionJson, SortDir expected)
    {
        // Arrange
        var json = $$"""{ "field": "name", "dir": {{directionJson}} }""";

        // Act
        var sortItem = JsonSerializer.Deserialize<SortItem>(json, WebOptions)!;

        // Assert
        sortItem.Dir.Should().Be(expected);
    }

    [Fact]
    public void Deserialize_SortItemDirectionNumberOutsideTheEnum_BindsSoValidationCanReportIt()
    {
        // Arrange
        var json = """{ "field": "name", "dir": 7 }""";

        // Act
        var sortItem = JsonSerializer.Deserialize<SortItem>(json, WebOptions)!;

        // Assert
        sortItem.Dir.Should().Be((SortDir)7);
    }

    [Fact]
    public void Serialize_SortItem_WritesTheDirectionName()
    {
        // Act
        var json = JsonSerializer.Serialize(new SortItem("name", SortDir.Desc), WebOptions);

        // Assert
        json.Should().Contain("\"dir\":\"Desc\"");
    }

    [Fact]
    public void Serialize_FilterValidationError_WritesTheCodeName()
    {
        // Arrange
        var validationError = new FilterValidationError("where", FilterValidationCode.UnknownField, "Unknown field 'x'.");

        // Act
        var json = JsonSerializer.Serialize(validationError, WebOptions);

        // Assert
        json.Should().Contain("\"code\":\"UnknownField\"");
    }

    [Fact]
    public void Serialize_LogicalOp_WritesTheCombinatorName()
    {
        // Act
        var json = JsonSerializer.Serialize(LogicalOp.Not, WebOptions);

        // Assert
        json.Should().Be("\"Not\"");
    }

    [Theory]
    [InlineData(FilterValidationCode.UnknownField, 0)]
    [InlineData(FilterValidationCode.InvalidValueType, 2)]
    [InlineData(FilterValidationCode.InterceptorRejected, 5)]
    [InlineData(FilterValidationCode.InvalidNodeShape, 14)]
    public void FilterValidationCode_NumericValues_StayWhereTheyWereBeforeTheUnusedMembersWereRemoved(FilterValidationCode code, int expected)
    {
        // Act
        var numericValue = (int)code;

        // Assert
        numericValue.Should().Be(expected);
    }
}
