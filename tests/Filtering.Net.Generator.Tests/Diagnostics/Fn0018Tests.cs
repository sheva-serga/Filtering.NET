using AwesomeAssertions;

using Filtering.Net.Generator.Tests.Resolution;

namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0018Tests
{
    [Fact]
    public void AutoResolve_NoCandidateFilters_FiresFN0018()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Id == "FN0018");
    }

    [Fact]
    public void AutoResolve_HasCandidate_DoesNotFireFN0018()
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
        result.Diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "FN0018");
    }

    [Fact]
    public void NestedTargetNotFound_ReportsNavigationPropertyAsAdditionalLocation()
    {
        // Arrange — Department has no [GenerateFilter<>] partner so resolution finds zero candidates;
        // the navigation property declaration is the lone additional location.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertDiagnosticHasAdditionalLocations is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnosticHasAdditionalLocations(source, "FN0018", expectedAdditionalCount: 1);
    }
}
