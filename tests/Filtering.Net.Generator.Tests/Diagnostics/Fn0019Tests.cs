using AwesomeAssertions;

using Filtering.Net.Generator.Tests.Resolution;

namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0019Tests
{
    [Fact]
    public void AutoResolve_TwoCandidateFilters_FiresFN0019()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>] public partial class DepartmentFilterA { }
            [GenerateFilter<Department>] public partial class DepartmentFilterB { }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [MapNested(nameof(User.Department))]
                private static partial void MapDepartment();
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Id == "FN0019");
    }

    [Fact]
    public void Generic_DisambiguatesAmbiguity_DoesNotFireFN0019()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>] public partial class DepartmentFilterA { }
            [GenerateFilter<Department>] public partial class DepartmentFilterB { }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [MapNested<DepartmentFilterA>(nameof(User.Department))]
                private static partial void MapDepartment();
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "FN0019");
    }
}
