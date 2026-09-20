using AwesomeAssertions;

using Filtering.Net.Generator.Tests.Resolution;

namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0019Tests
{
    [Fact]
    public void NavigationDoesNotExistOnHostEntity_FiresFN0019()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public string Email { get; set; } = ""; }
            [GenerateFilter<Department>] public partial class DepartmentFilter { }
            [GenerateFilter<User>]
            [MapNested("Department")]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Id == "FN0019");
    }

    [Fact]
    public void ValidReferenceNavigation_DoesNotFireFN0019()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>] public partial class DepartmentFilter { }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "FN0019");
    }

    [Fact]
    public void NestedNavigationInvalid_PrimitiveNav_ReportsNavigationPropertyAsAdditionalLocation()
    {
        // Arrange — Email is a primitive (string) so the navigation exists but isn't a reference type;
        // the property declaration is the lone additional location.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Email { get; set; } = ""; }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Email))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertDiagnosticHasAdditionalLocations is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnosticHasAdditionalLocations(source, "FN0019", expectedAdditionalCount: 1);
    }
}
