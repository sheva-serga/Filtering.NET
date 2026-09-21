using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0026 (NestedPathFilterUnknown): a [MapNested] Only/Except entry the nested filter does not map.</summary>
public class Fn0026Tests
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
        [MapNested(nameof(User.Department)PATH_FILTER)]
        public partial class UserFilter
        {
        }
        """;

    [Theory]
    [InlineData(", Only = new[] { \"Naem\" }")]
    [InlineData(", Except = new[] { \"Naem\" }")]
    public void UnknownPathInOnlyOrExcept_FiresFN0026(string pathFilter)
    {
        // Arrange
        var source = SourceTemplate.Replace("PATH_FILTER", pathFilter);

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0026");
    }

    [Theory]
    [InlineData(", Only = new[] { \"Name\" }")]
    [InlineData(", Except = new[] { \"Name\" }")]
    [InlineData("")]
    public void KnownPathInOnlyOrExcept_DoesNotFireFN0026(string pathFilter)
    {
        // Arrange
        var source = SourceTemplate.Replace("PATH_FILTER", pathFilter);

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0026");
    }

    [Fact]
    public void DottedPath_DoesNotFireFN0026()
    {
        // Arrange — a dotted entry names a path a further [MapNested] contributes, which a bounded
        // nesting may legitimately drop; only dotless entries can be checked.
        var source = SourceTemplate.Replace("PATH_FILTER", ", Except = new[] { \"Company.Name\" }");

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0026");
    }

    [Fact]
    public void PropertyMapRuleOnNestedFilter_DoesNotFireFN0026()
    {
        // Arrange — rules are lifted alongside [Map] properties, so Only may legitimately name one.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; public string Code { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>]
            [Map(nameof(Department.Name))]
            public partial class DepartmentFilter
            {
                [PropertyMap("Slug")]
                private static FilterRule<Department, string> MapSlug(FilterRuleBuilder<Department, string> builder) =>
                    builder.For(department => department.Code)
                           .Operator<string>("eq", (string column, string value) => column == value);
            }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Department), Only = new[] { "Slug" })]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var observedIds = DiagnosticTestHelpers.GetDiagnostics(source).Select(diagnostic => diagnostic.Id);

        // Assert
        observedIds.Should().NotContain("FN0026");
    }
}
