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
            public partial class UserFilter
            {
                [Map("Department.Name")]
                private static partial void MapDeptName();
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
            public partial class UserFilter
            {
                [Map("Department.Name")]
                private static partial void MapDeptName();
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
            public partial class UserFilter
            {
                [Map(nameof(User.Name))]
                private static partial void MapName();
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN1006");
    }
}
