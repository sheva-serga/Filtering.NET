using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0022 (ProfileTypeNotAProfile): [Map(Profile = typeof(X))] where X carries no [FilterProfile].</summary>
public class Fn0022Tests
{
    [Fact]
    public void ProfileIsNotAFilterProfile_FiresFN0022()
    {
        // Arrange — typeof(User) is a plausible copy/paste of the entity type.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name), Profile = typeof(User))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0022");
    }

    [Fact]
    public void ProfileIsNotAFilterProfile_DoesNotEmitAReferenceToAMissingBridge()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name), Profile = typeof(User))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        var (runResult, _) = GeneratorRunner.RunAndUpdate(source, excludeDiAbstractions: false);
        var emittedSources = runResult.GeneratedTrees.Select(tree => tree.ToString());

        // Assert
        emittedSources.Should().AllSatisfy(emitted => emitted.Should().NotContain("TestNs_UserProfile"));
    }

    [Fact]
    public void ProfileIsABuiltInFilterProfile_DoesNotFireFN0022()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name), Profile = typeof(StringFilter))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0022");
    }

    [Fact]
    public void ProfileIsAUserDeclaredFilterProfile_DoesNotFireFN0022()
    {
        // Arrange
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [FilterProfile<string>(BasedOn = typeof(StringFilter))]
            public static class NameFilter
            {
                [FilterOperator("fuzzy")]
                public static Expression<Func<string, string, bool>> Fuzzy => (column, value) => column.Contains(value);
            }
            [GenerateFilter<User>]
            [Map(nameof(User.Name), Profile = typeof(NameFilter))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0022");
    }

    [Fact]
    public void ProfileNestedTwoLevelsDeep_DoesNotFireFN0022()
    {
        // Arrange — the profile index has to descend through every level of type nesting.
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            public static class Outer
            {
                public static class Inner
                {
                    [FilterProfile<string>(BasedOn = typeof(StringFilter))]
                    public static class TextProfile
                    {
                        [FilterOperator("isBlank")]
                        public static Expression<Func<string, bool>> IsBlank => column => column == "";
                    }
                }
            }
            [GenerateFilter<User>]
            [Map(nameof(User.Name), Profile = typeof(Outer.Inner.TextProfile))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0022");
    }
}
