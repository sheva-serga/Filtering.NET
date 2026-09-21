namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0025 (NestedPrefixBlank): [MapNested(Prefix = "")] / whitespace prefix.</summary>
public class Fn0025Tests
{
    private const string SourceTemplate = """
        using Filtering.Net;
        namespace TestNs;
        public class Department { public string Name { get; set; } = ""; }
        public class User { public Department Department { get; set; } = new(); }
        [GenerateFilter<Department>]
        [Map(nameof(Department.Name))]
        public partial class DepartmentFilter
        {
        }
        [GenerateFilter<User>]
        [MapNested(nameof(User.Department)PREFIX_ARGUMENT)]
        public partial class UserFilter
        {
        }
        """;

    [Theory]
    [InlineData(", Prefix = \"\"")]
    [InlineData(", Prefix = \" \"")]
    public void BlankPrefix_FiresFN0025(string prefixArgument)
    {
        // Arrange
        var source = SourceTemplate.Replace("PREFIX_ARGUMENT", prefixArgument);

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0025");
    }

    [Fact]
    public void AbsentPrefix_DoesNotFireFN0025()
    {
        // Arrange
        var source = SourceTemplate.Replace("PREFIX_ARGUMENT", string.Empty);

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0025");
    }

    [Fact]
    public void NonBlankPrefix_DoesNotFireFN0025()
    {
        // Arrange
        var source = SourceTemplate.Replace("PREFIX_ARGUMENT", ", Prefix = \"dept\"");

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0025");
    }
}
