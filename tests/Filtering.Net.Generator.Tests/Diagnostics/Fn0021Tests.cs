using AwesomeAssertions;

using Filtering.Net.Generator.Tests.Resolution;

namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0021Tests
{
    [Fact]
    public void NavigationDoesNotExistOnHostEntity_FiresFN0021()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public string Email { get; set; } = ""; }
            [GenerateFilter<Department>] public partial class DepartmentFilter { }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [MapNested("Department")]
                private static partial void MapDepartment();
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Id == "FN0021");
    }

    [Fact]
    public void ValidReferenceNavigation_DoesNotFireFN0021()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>] public partial class DepartmentFilter { }
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
        result.Diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "FN0021");
    }
}
