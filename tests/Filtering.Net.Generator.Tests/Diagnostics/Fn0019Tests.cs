using AwesomeAssertions;

using Filtering.Net.Generator.Tests.Resolution;

namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0019Tests
{
    [Fact]
    public void NavigationIsCollection_FiresFN0019()
    {
        // Arrange
        var source = """
            using System.Collections.Generic;
            using Filtering.Net;
            namespace TestNs;
            public class Post { public string Title { get; set; } = ""; }
            public class User { public List<Post> Posts { get; set; } = new(); }
            [GenerateFilter<Post>] public partial class PostFilter { }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Posts))]
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
    public void NavigationDeclaredAsIEnumerable_FiresFN0019()
    {
        // Arrange — IEnumerable<T>'s own AllInterfaces only carries the non-generic IEnumerable,
        // so the interface walk alone would classify this as an unresolvable reference navigation.
        var source = """
            using System.Collections.Generic;
            using Filtering.Net;
            namespace TestNs;
            public class Post { public string Title { get; set; } = ""; }
            public class User { public IEnumerable<Post> Posts { get; set; } = new List<Post>(); }
            [GenerateFilter<Post>] public partial class PostFilter { }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Posts))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        var observedIds = result.Diagnostics.Select(diagnostic => diagnostic.Id).ToList();
        observedIds.Should().Contain("FN0019");
        observedIds.Should().NotContain("FN0017");
    }

    [Fact]
    public void SingleReferenceNavigation_DoesNotFireFN0019()
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
    public void NestedCollectionUnsupported_ReportsCollectionNavigationAsAdditionalLocation()
    {
        // Arrange
        var source = """
            using System.Collections.Generic;
            using Filtering.Net;
            namespace TestNs;
            public class Post { public string Title { get; set; } = ""; }
            public class User { public List<Post> Posts { get; set; } = new(); }
            [GenerateFilter<Post>] public partial class PostFilter { }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Posts))]
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
