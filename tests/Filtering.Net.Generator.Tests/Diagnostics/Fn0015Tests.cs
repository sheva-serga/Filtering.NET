using AwesomeAssertions;

using Filtering.Net.Generator.Tests.Resolution;

namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0015Tests
{
    [Fact]
    public void Generic_TFilterHasNoGenerateFilterAttribute_FiresFN0015()
    {
        // Arrange
        // FakeFilter has no [GenerateFilter<>], so it never appears as a host extraction result.
        // The resolver's explicit-filter-class lookup misses and FN0015 fires.
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
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Id == "FN0015");
    }

    [Fact]
    public void Generic_TFilterIsRealFilterClass_DoesNotFireFN0015()
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
        result.Diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "FN0015");
    }

    [Fact]
    public void Generic_TFilterIsAFilterClassForAnotherEntity_FiresFN0015NamingTheNavigationEntity()
    {
        // Arrange — OfficeFilter is a genuine [GenerateFilter<Office>] partial in this compilation;
        // it is simply pinned to the wrong navigation. The message has to point at the entity the
        // navigation actually has, not at a missing assembly reference.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Office { public string City { get; set; } = ""; }
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [GenerateFilter<Office>]
            [Map(nameof(Office.City))]
            public partial class OfficeFilter { }
            [GenerateFilter<User>]
            [MapNested<OfficeFilter>(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var message = DiagnosticTestHelpers.GetDiagnostics(source)
            .First(diagnostic => diagnostic.Id == "FN0015")
            .GetMessage();

        // Assert
        message.Should().Contain("navigation 'Department'");
        message.Should().Contain("TestNs.Department");
        message.Should().NotContain("references a filter class declared in another assembly");
    }

    [Fact]
    public void Generic_TFilterDeclaredInThisCompilation_MessageNamesTheNavigationAndEntity()
    {
        // Arrange — the rule also covers an in-source type that is not a filter class, so the
        // message must not send the reader looking for a missing assembly reference.
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
        var message = DiagnosticTestHelpers.GetDiagnostics(source)
            .First(diagnostic => diagnostic.Id == "FN0015")
            .GetMessage();

        // Assert
        message.Should().Contain("navigation 'Department'");
        message.Should().Contain("TestNs.Department");
        message.Should().NotContain("references a filter class declared in another assembly");
    }

    [Fact]
    public void NestedFilterClassUnusable_ReportsExplicitFilterClassAsAdditionalLocation()
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
        DiagnosticTestHelpers.AssertDiagnosticHasAdditionalLocations(source, "FN0015", expectedAdditionalCount: 1);
    }
}
