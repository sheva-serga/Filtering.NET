using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0023 (PropertyMapSignatureInvalid): a [PropertyMap] method the generated CreateSchema cannot call.</summary>
public class Fn0023Tests
{
    [Fact]
    public void PropertyMapMethodIsNotStatic_FiresFN0023()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string FirstName { get; set; } = ""; public string LastName { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [PropertyMap("FullName")]
                private FilterRule<User, string> MapFullName(FilterRuleBuilder<User, string> builder) =>
                    builder.For(user => user.FirstName + " " + user.LastName)
                           .Operator<string>("eq", (string column, string value) => column == value);
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0023");
    }

    [Fact]
    public void PropertyMapMethodReturnsTheBuilder_FiresFN0023()
    {
        // Arrange — the implicit conversion does not help: MapRule could not infer TEntity/TValue.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string FirstName { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [PropertyMap("FullName")]
                public static FilterRuleBuilder<User, string> MapFullName(FilterRuleBuilder<User, string> builder) =>
                    builder.For(user => user.FirstName);
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0023");
    }

    [Fact]
    public void PropertyMapMethodNamesTheOffendingMethod()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string FirstName { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [PropertyMap("FullName")]
                private FilterRule<User, string> MapFullName(FilterRuleBuilder<User, string> builder) =>
                    builder.For(user => user.FirstName).Operator<string>("eq", (string column, string value) => column == value);
            }
            """;

        // Act
        var diagnostic = DiagnosticTestHelpers.GetDiagnostics(source).First(item => item.Id == "FN0023");

        // Assert
        diagnostic.GetMessage().Should().Contain("MapFullName").And.Contain("instance method");
    }

    [Fact]
    public void WellFormedPropertyMapMethod_DoesNotFireFN0023()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string FirstName { get; set; } = ""; public string LastName { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [PropertyMap("FullName")]
                private static FilterRule<User, string> MapFullName(FilterRuleBuilder<User, string> builder) =>
                    builder.For(user => user.FirstName + " " + user.LastName)
                           .Operator<string>("eq", (string column, string value) => column == value);
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0023");
    }

    [Fact]
    public void PropertyMapShadowedByMap_ReportsOnlyFN0002()
    {
        // Arrange — FN0002 already tells the consumer the override is ignored.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            public partial class UserFilter
            {
                [PropertyMap(nameof(User.Name))]
                private static void OverrideName(FilterRuleBuilder<User, string> rule) { }
            }
            """;

        // Act
        var observedIds = DiagnosticTestHelpers.GetDiagnostics(source).Select(diagnostic => diagnostic.Id).ToList();

        // Assert
        observedIds.Should().Contain("FN0002");
        observedIds.Should().NotContain("FN0023");
    }
}
