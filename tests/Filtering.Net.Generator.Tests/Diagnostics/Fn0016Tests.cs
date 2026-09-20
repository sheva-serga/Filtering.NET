using AwesomeAssertions;

using Filtering.Net.Generator.Tests.Resolution;

namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0016Tests
{
    [Fact]
    public void Generic_TFilterHasNoGenerateFilterAttribute_FiresFN0016()
    {
        // Arrange
        // FakeFilter has no [GenerateFilter<>], so it never appears as a host extraction result.
        // The resolver's explicit-filter-class lookup misses and FN0016 fires.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            public class FakeFilter { }
            [GenerateFilter<User>]
            [MapNested<FakeFilter>(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Id == "FN0016");
    }

    [Fact]
    public void Generic_TFilterIsRealFilterClass_DoesNotFireFN0016()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>] public partial class DeptFilter { }
            [GenerateFilter<User>]
            [MapNested<DeptFilter>(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "FN0016");
    }

    [Fact]
    public void NestedCrossAssembly_ReportsExplicitFilterClassAsAdditionalLocation()
    {
        // Arrange — FakeFilter is in source, so its declaration is reported as an additional location.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            public class FakeFilter { }
            [GenerateFilter<User>]
            [MapNested<FakeFilter>(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertDiagnosticHasAdditionalLocations is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnosticHasAdditionalLocations(source, "FN0016", expectedAdditionalCount: 1);
    }
}
