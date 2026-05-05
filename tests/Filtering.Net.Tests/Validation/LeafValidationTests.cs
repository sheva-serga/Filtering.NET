using System.Text.Json;
using AwesomeAssertions;
using Xunit;

namespace Filtering.Net.Tests.Validation;

public class LeafValidationTests
{
    private static readonly string[] StringScalarOps = ["EQ", "NE", "CONTAINS", "STARTSWITH", "ENDSWITH"];
    private static readonly string[] StringArrayOps = ["IN"];
    private static readonly string[] StringNoneOps = ["ISNULL"];

    [Fact]
    public void ValidateMappedLeaf_ScalarOp_WithValidValue_ProducesNoError()
    {
        var leaf = LeafFromJson("eq", "\"alice\"");
        var errors = new List<FilterValidationError>();

        LeafValidation.ValidateMappedLeaf<string>(
            leaf, "where", errors, "Name",
            StringScalarOps, StringArrayOps, StringNoneOps,
            StringFilter.TryGetValue, StringFilter.TryGetArray);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateMappedLeaf_ScalarOp_WithWrongJsonKind_AddsTypeError()
    {
        var leaf = LeafFromJson("eq", "42");
        var errors = new List<FilterValidationError>();

        LeafValidation.ValidateMappedLeaf<string>(
            leaf, "where", errors, "Name",
            StringScalarOps, StringArrayOps, StringNoneOps,
            StringFilter.TryGetValue, StringFilter.TryGetArray);

        errors.Should().ContainSingle()
            .Which.Code.Should().Be(FilterValidationCode.InvalidValueType);
        errors[0].Path.Should().Be("where.value");
    }

    [Fact]
    public void ValidateMappedLeaf_ArrayOp_WithValidArray_ProducesNoError()
    {
        var leaf = LeafFromJson("in", "[\"a\",\"b\"]");
        var errors = new List<FilterValidationError>();

        LeafValidation.ValidateMappedLeaf<string>(
            leaf, "where", errors, "Name",
            StringScalarOps, StringArrayOps, StringNoneOps,
            StringFilter.TryGetValue, StringFilter.TryGetArray);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateMappedLeaf_ArrayOp_WithNonArrayJson_AddsTypeError()
    {
        var leaf = LeafFromJson("in", "\"not-an-array\"");
        var errors = new List<FilterValidationError>();

        LeafValidation.ValidateMappedLeaf<string>(
            leaf, "where", errors, "Name",
            StringScalarOps, StringArrayOps, StringNoneOps,
            StringFilter.TryGetValue, StringFilter.TryGetArray);

        errors.Should().ContainSingle()
            .Which.Code.Should().Be(FilterValidationCode.InvalidValueType);
    }

    [Fact]
    public void ValidateMappedLeaf_NoneOp_WithNullValue_ProducesNoError()
    {
        var leaf = LeafFromJson("isNull", "null");
        var errors = new List<FilterValidationError>();

        LeafValidation.ValidateMappedLeaf<string>(
            leaf, "where", errors, "Name",
            StringScalarOps, StringArrayOps, StringNoneOps,
            StringFilter.TryGetValue, StringFilter.TryGetArray);

        errors.Should().BeEmpty();
    }

    [Fact]
    public void ValidateMappedLeaf_NoneOp_WithValuePresent_AddsNoValueError()
    {
        var leaf = LeafFromJson("isNull", "\"unexpected\"");
        var errors = new List<FilterValidationError>();

        LeafValidation.ValidateMappedLeaf<string>(
            leaf, "where", errors, "Name",
            StringScalarOps, StringArrayOps, StringNoneOps,
            StringFilter.TryGetValue, StringFilter.TryGetArray);

        errors.Should().ContainSingle()
            .Which.Message.Should().Contain("takes no value");
    }

    [Fact]
    public void ValidateMappedLeaf_UnknownOperator_AddsOperatorErrorCarryingPropertyName()
    {
        var leaf = LeafFromJson("matches", "\"x\"");
        var errors = new List<FilterValidationError>();

        LeafValidation.ValidateMappedLeaf<string>(
            leaf, "where", errors, "Name",
            StringScalarOps, StringArrayOps, StringNoneOps,
            StringFilter.TryGetValue, StringFilter.TryGetArray);

        errors.Should().ContainSingle()
            .Which.Code.Should().Be(FilterValidationCode.OperatorNotAllowed);
        errors[0].Message.Should().Contain("'Name'");
    }

    [Fact]
    public void ValidateMappedLeaf_NullArrayExtractorWithEmptyArrayOps_FallsThroughToOperatorError()
    {
        var leaf = LeafFromJson("in", "[\"a\"]");
        var errors = new List<FilterValidationError>();

        LeafValidation.ValidateMappedLeaf<bool>(
            leaf, "where", errors, "IsActive",
            ["EQ"], [], ["ISNULL"],
            BoolFilter.TryGetValue, arrayExtractor: null);

        errors.Should().ContainSingle()
            .Which.Code.Should().Be(FilterValidationCode.OperatorNotAllowed);
    }

    [Fact]
    public void ValidateMappedLeaf_OperatorOutsideAllowedSubset_AddsOperatorError()
    {
        // Profile supports "contains" but the property's allowed subset doesn't include it.
        var leaf = LeafFromJson("contains", "\"ali\"");
        var errors = new List<FilterValidationError>();

        LeafValidation.ValidateMappedLeaf<string>(
            leaf, "where", errors, "Name",
            allowedScalarOps: ["EQ", "NE"],
            allowedArrayOps: [],
            allowedNoneOps: [],
            StringFilter.TryGetValue, StringFilter.TryGetArray);

        errors.Should().ContainSingle()
            .Which.Code.Should().Be(FilterValidationCode.OperatorNotAllowed);
    }

    [Fact]
    public void ValidateMappedLeaf_NumericExtractor_AcceptsJsonNumber()
    {
        var leaf = LeafFromJson("gt", "30");
        var errors = new List<FilterValidationError>();

        LeafValidation.ValidateMappedLeaf<int>(
            leaf, "where", errors, "Age",
            ["EQ", "NE", "GT", "GTE", "LT", "LTE"], ["IN"], ["ISNULL"],
            Int32Filter.TryGetValue, Int32Filter.TryGetArray);

        errors.Should().BeEmpty();
    }

    private static FilterLeaf LeafFromJson(string @operator, string valueJson)
    {
        using var doc = JsonDocument.Parse(valueJson);
        return new FilterLeaf("Name", @operator, doc.RootElement.Clone());
    }
}
