using AwesomeAssertions;

using Filtering.Net.Generator.Tests.Resolution;

namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0018Tests
{
    [Fact]
    public void NavigationDoesNotExistOnHostEntity_FiresFN0018()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Department { public string Name { get; set; } = ""; }
            public class User { public string Email { get; set; } = ""; }
            [GenerateFilter<Department>] public partial class DepartmentFilter { }
            [GenerateFilter<User>]
            [MapNested("Department")]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "UserFilter");

        // Assert
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Id == "FN0018");
    }

    [Fact]
    public void ValidReferenceNavigation_DoesNotFireFN0018()
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
        result.Diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "FN0018");
    }

    [Fact]
    public void NavigationInheritedFromBaseEntity_DoesNotFireFN0018()
    {
        // Arrange — audit navigations on a shared base entity are a common EF pattern, and a dotted
        // [Map] already resolves through the base chain.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            public abstract class AuditedEntity { public User CreatedBy { get; set; } = new(); }
            public class Order : AuditedEntity { public int Id { get; set; } }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            public partial class UserFilter
            {
            }
            [GenerateFilter<Order>]
            [MapNested("CreatedBy")]
            public partial class OrderFilter
            {
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "OrderFilter");

        // Assert
        result.Diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "FN0018");
        result.Model.NestedMappings[0].ResolvedTargetClassFqn.Should().Be("TestNs.UserFilter");
    }

    [Fact]
    public void EntityIsANestedType_DoesNotFireFN0018()
    {
        // Arrange — the entity type is carried as a symbol, not re-looked-up from a display string
        // that GetTypeByMetadataName would reject for a nested type.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Brand { public string Name { get; set; } = ""; }
            public static class Catalog { public class Product { public Brand Brand { get; set; } = new(); } }
            [GenerateFilter<Brand>]
            [Map(nameof(Brand.Name))]
            public partial class BrandFilter
            {
            }
            [GenerateFilter<Catalog.Product>]
            [MapNested("Brand")]
            public partial class ProductFilter
            {
            }
            """;

        // Act
        var result = ResolutionTestHelpers.Resolve(source, "ProductFilter");

        // Assert
        result.Diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "FN0018");
        result.Model.NestedMappings[0].ResolvedTargetClassFqn.Should().Be("TestNs.BrandFilter");
    }

    [Fact]
    public void NestedNavigationInvalid_PrimitiveNav_ReportsNavigationPropertyAsAdditionalLocation()
    {
        // Arrange — Email is a primitive (string) so the navigation exists but isn't a reference type;
        // the property declaration is the lone additional location.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Email { get; set; } = ""; }
            [GenerateFilter<User>]
            [MapNested(nameof(User.Email))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertDiagnosticHasAdditionalLocations is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnosticHasAdditionalLocations(source, "FN0018", expectedAdditionalCount: 1);
    }
}
