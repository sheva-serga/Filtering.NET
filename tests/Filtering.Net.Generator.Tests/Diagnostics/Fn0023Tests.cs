namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0023Tests
{
    private const string SourceTemplate = """
        using Filtering.Net;
        namespace TestNs;
        public class Department { public string Name { get; set; } = ""; }
        public class User { public Department Department { get; set; } = new(); }
        [GenerateFilter<Department>]
        public partial class DepartmentFilter
        {
            [Map(nameof(Department.Name))] private static partial void MapName();
        }
        [GenerateFilter<User>]
        public partial class UserFilter
        {
            [MapNested(nameof(User.Department), MaxDepth = MAX_DEPTH)] private static partial void MapDept();
        }
        """;

    [Fact]
    public void NegativeMaxDepth_FiresFN0023()
    {
        // Arrange
        var source = SourceTemplate.Replace("MAX_DEPTH", "-1");

        // Act
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0023");
    }

    [Fact]
    public void PositiveMaxDepth_DoesNotFireFN0023()
    {
        // Arrange
        var source = SourceTemplate.Replace("MAX_DEPTH", "3");

        // Act
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0023");
    }
}
