using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0021Tests
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
    public void NegativeMaxDepth_FiresFN0021()
    {
        // Arrange
        var source = SourceTemplate.Replace("MAX_DEPTH", "-1");

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0021");
    }

    [Fact]
    public void PositiveMaxDepth_DoesNotFireFN0021()
    {
        // Arrange
        var source = SourceTemplate.Replace("MAX_DEPTH", "3");

        // Act
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0021");
    }

    [Fact]
    public void MaxDepthAboveCeiling_FiresFN0021()
    {
        // Arrange
        var source = SourceTemplate.Replace("MAX_DEPTH", "int.MaxValue");

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0021");
    }

    [Fact]
    public void MaxDepthAboveCeilingOnSelfReference_DoesNotExpandTheCycle()
    {
        // Arrange — an unbounded-looking MaxDepth on a self-referencing navigation would otherwise
        // recurse one frame per allowed level until the analyzer process died.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Employee { public string Name { get; set; } = ""; public Employee Manager { get; set; } = new(); }
            [GenerateFilter<Employee>]
            [Map(nameof(Employee.Name))]
            [MapNested(nameof(Employee.Manager), MaxDepth = int.MaxValue)]
            public partial class EmployeeFilter
            {
            }
            """;

        // Act
        var observedIds = DiagnosticTestHelpers.GetDiagnostics(source).Select(diagnostic => diagnostic.Id).ToList();

        // Assert
        observedIds.Should().Contain("FN0021");
    }
}
