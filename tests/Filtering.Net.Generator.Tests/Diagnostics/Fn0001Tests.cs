using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0001 (DuplicateMap): two [Map] declarations target the same property.</summary>
public class Fn0001Tests
{
    [Fact]
    public void TwoMapsForSameProperty_FiresFN0001()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            [Map(nameof(User.Name))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0001");
    }

    [Fact]
    public void DistinctProperties_DoesNotFireFN0001()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; public int Age { get; set; } }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            [Map(nameof(User.Age))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0001");
    }

    [Fact]
    public void TwoMapNestedSamePrefix_FiresFN0001WithBothLocations()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public int Id { get; set; } }
            public class User { public Department Department { get; set; } = new(); }
            [Map(nameof(Department.Id))]
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
            }
            [Map(nameof(Department.Id))]
            [GenerateFilter<Department>] public partial class AdminDepartmentFilter
            {
            }
            [GenerateFilter<User>]
            [MapNested<DepartmentFilter>(nameof(User.Department))]
            [MapNested<AdminDepartmentFilter>(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var diagnostics = DiagnosticTestHelpers.GetDiagnostics(source);

        // Assert
        var fn0001 = diagnostics.FirstOrDefault(diagnostic => diagnostic.Id == "FN0001");
        fn0001.Should().NotBeNull();
        fn0001!.AdditionalLocations.Should().NotBeEmpty();
    }

    [Fact]
    public void MapAndMapNestedFlattenedCollision_FiresFN0001()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public Department Department { get; set; } = new(); }
            [Map(nameof(Department.Name))]
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
            }
            [GenerateFilter<User>]
            [Map("Department.Name")]
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0001");
    }

    [Fact]
    public void TwoMapNestedCustomPrefixCollision_FiresFN0001()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public int Id { get; set; } }
            public class Office { public int Id { get; set; } }
            public class User { public Department Department { get; set; } = new(); public Office Office { get; set; } = new(); }
            [Map(nameof(Department.Id))]
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
            }
            [Map(nameof(Office.Id))]
            [GenerateFilter<Office>] public partial class OfficeFilter
            {
            }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Department))]
            [MapNested(nameof(User.Office), Prefix = "department")]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0001");
    }

    [Fact]
    public void NoCollision_DoesNotFireFN0001()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public int Id { get; set; } }
            public class User { public Department Department { get; set; } = new(); }
            [Map(nameof(Department.Id))]
            [GenerateFilter<Department>] public partial class DepartmentFilter
            {
            }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Department))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0001");
    }

    [Fact]
    public void TwoSortableMapsForSameProperty_FiresFN0001NotFN0002()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name), Sortable = true)]
            [Map(nameof(User.Name), Sortable = true)]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var diagnostics = DiagnosticTestHelpers.GetDiagnostics(source);

        // Assert
        var observedIds = diagnostics.Select(diagnostic => diagnostic.Id).ToList();
        observedIds.Should().Contain("FN0001");
        observedIds.Should().NotContain("FN0002");
    }

    [Fact]
    public void DuplicateMap_ReportsPriorMapAsAdditionalLocation()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            [Map(nameof(User.Name))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertDiagnosticHasAdditionalLocations is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnosticHasAdditionalLocations(source, "FN0001", expectedAdditionalCount: 1);
    }
}
