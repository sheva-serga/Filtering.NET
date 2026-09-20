using AwesomeAssertions;

using Filtering.Net.Generator.Tests.Resolution;

namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0015Tests
{
    [Fact]
    public void DirectSelfReferenceViaNav_FiresFN0015()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; public User Manager { get; set; } = new(); }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            [MapNested(nameof(User.Manager))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Select(diagnostic => diagnostic.Id).Should().Contain("FN0015");
    }

    [Fact]
    public void IndirectCycleAcrossTwoFilters_FiresFN0015()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; public User Head { get; set; } = new(); }
            public class User { public string Name { get; set; } = ""; public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>]
            [Map(nameof(Department.Name))]
            [MapNested(nameof(Department.Head))]
            public partial class DepartmentFilter
            {
            }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Select(diagnostic => diagnostic.Id).Should().Contain("FN0015");
    }

    [Fact]
    public void SelfReferenceWithMaxDepth_DoesNotFireFN0015()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Employee { public string Name { get; set; } = ""; public Employee? Manager { get; set; } }
            [GenerateFilter<Employee>]
            [Map(nameof(Employee.Name))]
            [MapNested(nameof(Employee.Manager), MaxDepth = 2)]
            public partial class EmployeeFilter
            {
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "EmployeeFilter");

        // Assert
        result.Diagnostics.Select(diagnostic => diagnostic.Id).Should().NotContain("FN0015");
        result.Model.Properties.Select(property => property.PropertyName)
            .Should().Equal("Name", "Manager.Name", "Manager.Manager.Name");
    }

    [Fact]
    public void IndirectCycleBoundedOnOneNesting_DoesNotFireFN0015()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; public User Head { get; set; } = new(); }
            public class User { public string Name { get; set; } = ""; public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>]
            [Map(nameof(Department.Name))]
            [MapNested(nameof(Department.Head), MaxDepth = 1)]
            public partial class DepartmentFilter
            {
            }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Select(diagnostic => diagnostic.Id).Should().NotContain("FN0015");
        result.Model.Properties.Select(property => property.PropertyName).Should().Equal(
            "Name", "Department.Name", "Department.Head.Name", "Department.Head.Department.Name");
    }

    [Fact]
    public void NoCycle_DoesNotFireFN0015()
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
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Select(diagnostic => diagnostic.Id).Should().NotContain("FN0015");
    }

    [Fact]
    public void NestedCycle_ReportsOuterMapNestedSiteAsAdditionalLocation()
    {
        // Arrange — A→B→A 2-class cycle: outer User.Department [MapNested] is pushed to the
        // DFS stack before Department.Head re-visits UserFilter, so the cycle diagnostic
        // surfaces the outer site as the lone additional location.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; public User Head { get; set; } = new(); }
            public class User { public string Name { get; set; } = ""; public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>]
            [Map(nameof(Department.Name))]
            [MapNested(nameof(Department.Head))]
            public partial class DepartmentFilter
            {
            }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertDiagnosticHasAdditionalLocations is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnosticHasAdditionalLocations(source, "FN0015", expectedAdditionalCount: 1);
    }
}
