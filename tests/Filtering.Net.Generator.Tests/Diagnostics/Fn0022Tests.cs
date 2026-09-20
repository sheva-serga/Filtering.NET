namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0022Tests
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
        [MapNested(nameof(User.Department), MaxDepth = MAX_DEPTH)]
        public partial class UserFilter
        {
        }
        """;

    [Fact]
    public void NegativeMaxDepth_FiresFN0022()
    {
        // Arrange
        var source = SourceTemplate.Replace("MAX_DEPTH", "-1");

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0022");
    }

    [Fact]
    public void PositiveMaxDepth_DoesNotFireFN0022()
    {
        // Arrange
        var source = SourceTemplate.Replace("MAX_DEPTH", "3");

        // Act
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0022");
    }
}
