using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0005 (UnknownOperator): Only/Except references an operator not on the resolved profile.</summary>
public class Fn0005Tests
{
    [Fact]
    public void OnlyReferencesUnknownOperator_FiresFN0005()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name), Only = new[] { "eq", "iAmNotARealOp" })]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0005");
    }

    [Fact]
    public void ExceptReferencesUnknownOperator_FiresFN0005()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name), Except = new[] { "iAmNotARealOp" })]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0005");
    }

    [Fact]
    public void OnlyReferencesValidOperators_DoesNotFireFN0005()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name), Only = new[] { "eq", "ne" })]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0005");
    }

    [Fact]
    public void OnlyNamesOperatorInDifferentCase_DoesNotFireFN0005()
    {
        // Arrange — operator names are case-insensitive at runtime, so the compile-time check must agree.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name), Only = new[] { "EQ", "Contains" })]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0005");
    }

    [Fact]
    public void OnlyAndExceptNamesInDifferentCase_SelectTheProfileSpelledOperators()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; public string Email { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name), Only = new[] { "EQ", "Contains" })]
            [Map(nameof(User.Email), Except = new[] { "ISNULL" })]
            public partial class UserFilter
            {
            }
            """;
        var properties = GeneratorRunner.ExtractFilterClassModels(source).Single().Properties;

        // Act
        var nameOperators = properties.Single(property => property.PropertyName == "Name").AllowedOperators;
        var emailOperators = properties.Single(property => property.PropertyName == "Email").AllowedOperators;

        // Assert
        nameOperators.Should().BeEquivalentTo(["eq", "contains"]);
        emailOperators.Should().NotContain("isNull").And.Contain("eq");
    }
}
