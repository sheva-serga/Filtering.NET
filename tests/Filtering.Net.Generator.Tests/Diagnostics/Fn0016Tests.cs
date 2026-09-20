using System.Linq;

using AwesomeAssertions;

using Filtering.Net.Generator.Tests.Resolution;

namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0016Tests
{
    [Fact]
    public void DirectSelfReferenceViaNav_FiresFN0016()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; public User Manager { get; set; } = new(); }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name))] private static partial void MapName();
                [MapNested(nameof(User.Manager))] private static partial void MapManager();
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Select(diagnostic => diagnostic.Id).Should().Contain("FN0016");
    }

    [Fact]
    public void IndirectCycleAcrossTwoFilters_FiresFN0016()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; public User Head { get; set; } = new(); }
            public class User { public string Name { get; set; } = ""; public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>]
            public partial class DepartmentFilter
            {
                [Map(nameof(Department.Name))] private static partial void MapName();
                [MapNested(nameof(Department.Head))] private static partial void MapHead();
            }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name))] private static partial void MapName();
                [MapNested(nameof(User.Department))] private static partial void MapDept();
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Select(diagnostic => diagnostic.Id).Should().Contain("FN0016");
    }

    [Fact]
    public void SelfReferenceWithMaxDepth_DoesNotFireFN0016()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Employee { public string Name { get; set; } = ""; public Employee? Manager { get; set; } }
            [GenerateFilter<Employee>]
            public partial class EmployeeFilter
            {
                [Map(nameof(Employee.Name))] private static partial void MapName();
                [MapNested(nameof(Employee.Manager), MaxDepth = 2)] private static partial void MapManager();
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "EmployeeFilter");

        // Assert
        result.Diagnostics.Select(diagnostic => diagnostic.Id).Should().NotContain("FN0016");
        result.Model.Properties.Select(property => property.PropertyName)
            .Should().Equal("Name", "Manager.Name", "Manager.Manager.Name");
    }

    [Fact]
    public void IndirectCycleBoundedOnOneNesting_DoesNotFireFN0016()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; public User Head { get; set; } = new(); }
            public class User { public string Name { get; set; } = ""; public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>]
            public partial class DepartmentFilter
            {
                [Map(nameof(Department.Name))] private static partial void MapName();
                [MapNested(nameof(Department.Head), MaxDepth = 1)] private static partial void MapHead();
            }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name))] private static partial void MapName();
                [MapNested(nameof(User.Department))] private static partial void MapDept();
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Select(diagnostic => diagnostic.Id).Should().NotContain("FN0016");
        result.Model.Properties.Select(property => property.PropertyName).Should().Equal(
            "Name", "Department.Name", "Department.Head.Name", "Department.Head.Department.Name");
    }

    [Fact]
    public void NoCycle_DoesNotFireFN0016()
    {
        // Arrange
        var source = """
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
                [MapNested(nameof(User.Department))] private static partial void MapDept();
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Select(diagnostic => diagnostic.Id).Should().NotContain("FN0016");
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
            public partial class DepartmentFilter
            {
                [Map(nameof(Department.Name))] private static partial void MapName();
                [MapNested(nameof(Department.Head))] private static partial void MapHead();
            }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name))] private static partial void MapName();
                [MapNested(nameof(User.Department))] private static partial void MapDept();
            }
            """;

        // Act
        // (no separate act step — AssertDiagnosticHasAdditionalLocations is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnosticHasAdditionalLocations(source, "FN0016", expectedAdditionalCount: 1);
    }
}
