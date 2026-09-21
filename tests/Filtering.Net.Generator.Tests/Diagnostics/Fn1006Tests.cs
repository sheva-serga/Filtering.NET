namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN1006 (NullableNavInPath): a dotted [Map] path crosses a nullable reference-typed navigation.</summary>
public class Fn1006Tests
{
    [Fact]
    public void NullableNavigationInDottedPath_FiresFN1006()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department? Department { get; set; } }
            [GenerateFilter<User>]
            [Map("Department.Name")]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN1006");
    }

    [Fact]
    public void NonNullableNavigationInDottedPath_DoesNotFireFN1006()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<User>]
            [Map("Department.Name")]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN1006");
    }

    [Fact]
    public void NullableNavigationInMapNested_DoesNotFireFN1006()
    {
        // Arrange — an optional reference navigation is the normal EF shape and [MapNested] offers no
        // place to put a null guard (the lifted properties belong to the nested filter class), so the
        // rule stays scoped to dotted [Map] paths, which can be rewritten as a [PropertyMap] rule.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department? Department { get; set; } }
            [GenerateFilter<Department>]
            [Map(nameof(Department.Name))]
            public partial class DepartmentFilter
            {
            }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN1006");
    }

    [Fact]
    public void NonNullableNavigationInMapNested_DoesNotFireFN1006()
    {
        // Arrange
        var source = """
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
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN1006");
    }

    [Fact]
    public void DirectPropertyNoNavigation_DoesNotFireFN1006()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN1006");
    }
}
