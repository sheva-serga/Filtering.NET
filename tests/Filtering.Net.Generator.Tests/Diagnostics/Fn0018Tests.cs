using AwesomeAssertions;

using Filtering.Net.Generator.Tests.Resolution;

namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0018Tests
{
    [Fact]
    public void Generic_TFilterHasNoGenerateFilterAttribute_FiresFN0018()
    {
        // Arrange
        // FakeFilter has no [GenerateFilter<>], so it never appears as a host extraction result.
        // The resolver's explicit-filter-class lookup misses and FN0018 fires.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            public class FakeFilter { }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [MapNested<FakeFilter>(nameof(User.Department))]
                private static partial void MapDepartment();
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Id == "FN0018");
    }

    [Fact]
    public void Generic_TFilterIsRealFilterClass_DoesNotFireFN0018()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Department>] public partial class DeptFilter { }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [MapNested<DeptFilter>(nameof(User.Department))]
                private static partial void MapDepartment();
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "FN0018");
    }
}
