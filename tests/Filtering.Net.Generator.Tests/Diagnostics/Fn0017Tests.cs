using System.Linq;

using AwesomeAssertions;

using Filtering.Net.Generator.Tests.Resolution;

namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0017Tests
{
    [Fact]
    public void DirectSelfReferenceViaNav_FiresFN0017()
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
        result.Diagnostics.Select(diagnostic => diagnostic.Id).Should().Contain("FN0017");
    }

    [Fact]
    public void IndirectCycleAcrossTwoFilters_FiresFN0017()
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
        result.Diagnostics.Select(diagnostic => diagnostic.Id).Should().Contain("FN0017");
    }

    [Fact]
    public void NoCycle_DoesNotFireFN0017()
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
        result.Diagnostics.Select(diagnostic => diagnostic.Id).Should().NotContain("FN0017");
    }
}
