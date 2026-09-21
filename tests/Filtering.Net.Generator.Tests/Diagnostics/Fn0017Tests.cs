using AwesomeAssertions;

using Filtering.Net.Generator.Tests.Resolution;

namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0017Tests
{
    [Fact]
    public void AutoResolve_NoCandidateFilters_FiresFN0017()
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
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Id == "FN0017");
    }

    [Fact]
    public void AutoResolve_HasCandidate_DoesNotFireFN0017()
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
        result.Diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "FN0017");
    }

    [Fact]
    public void BrokenNestingOnANestedFilter_IsReportedOnceAcrossAllHosts()
    {
        // Arrange — three hosts reach DepartmentFilter, whose own [MapNested] cannot resolve.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Company { public string Name { get; set; } = ""; }
            public class Department { public string Name { get; set; } = ""; public Company Company { get; set; } = new(); }
            public class User { public Department Department { get; set; } = new(); }
            public class Order { public Department Department { get; set; } = new(); }
            public class Ticket { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>]
            [Map(nameof(Department.Name))]
            [MapNested(nameof(Department.Company))]
            public partial class DepartmentFilter
            {
            }
            [GenerateFilter<User>] [MapNested(nameof(User.Department))] public partial class UserFilter { }
            [GenerateFilter<Order>] [MapNested(nameof(Order.Department))] public partial class OrderFilter { }
            [GenerateFilter<Ticket>] [MapNested(nameof(Ticket.Department))] public partial class TicketFilter { }
            """;

        // Act
        var fn0018Diagnostics = DiagnosticTestHelpers.GetDiagnostics(source)
            .Where(diagnostic => diagnostic.Id == "FN0017")
            .ToList();

        // Assert
        fn0018Diagnostics.Should().ContainSingle(
            because: "the nesting is declared on DepartmentFilter, so only its own resolution pass reports it");
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
        DiagnosticTestHelpers.AssertDiagnosticHasAdditionalLocations(source, "FN0017", expectedAdditionalCount: 1);
    }
}
