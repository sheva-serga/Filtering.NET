using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0028 (OperatorMemberShapeInvalid): a <c>[FilterOperator]</c> member whose
/// declared type is not an operator template contributes nothing to the emitted profile.</summary>
public class Fn0028Tests
{
    private const string MissingExpressionWrapperSource = """
        using System;
        using System.Linq.Expressions;
        using Filtering.Net;
        namespace TestNs;
        [FilterProfile<string>(BasedOn = typeof(StringFilter))]
        public static class FuzzyProfile
        {
            [FilterOperator("fuzzy")]
            public static Func<string, string, bool> Fuzzy => (column, value) => column.Contains(value);
        }
        public class User { public string Name { get; set; } = ""; }
        [GenerateFilter<User>]
        [Map(nameof(User.Name), Profile = typeof(FuzzyProfile))]
        public partial class UserFilter { }
        """;

    [Fact]
    public void OperatorMemberIsNotAnExpression_FiresFN0028()
    {
        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(MissingExpressionWrapperSource, "FN0028");
    }

    [Fact]
    public void OperatorMemberIsNotAnExpression_IsNotAdvertisedInAllowedOperators()
    {
        // Arrange — the operator reaches no FilterOperator entry in the bridge, so advertising it
        // would only surface as an "unknown operator" validation error at request time.
        var models = GeneratorRunner.ExtractFilterClassModels(MissingExpressionWrapperSource);

        // Act
        var allowedOperators = models.Single().Properties.Single().AllowedOperators;

        // Assert
        allowedOperators.Should().NotContain("fuzzy");
        allowedOperators.Should().Contain("contains", because: "the inherited StringFilter operators are unaffected");
    }

    [Fact]
    public void OperatorMemberWithFourTypeArguments_FiresFN0028()
    {
        // Arrange — an arity the runtime has no FilterOperator factory for.
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>(BasedOn = typeof(StringFilter))]
            public static class BetweenProfile
            {
                [FilterOperator("between")]
                public static Expression<Func<string, string, string, bool>> Between =>
                    (column, low, high) => string.Compare(column, low) >= 0 && string.Compare(column, high) <= 0;
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0028");
    }

    [Fact]
    public void OperatorMemberIsAnExpressionTemplate_DoesNotFireFN0028()
    {
        // Arrange
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>(BasedOn = typeof(StringFilter))]
            public static class FuzzyProfile
            {
                [FilterOperator("fuzzy")]
                public static Expression<Func<string, string, bool>> Fuzzy => (column, value) => column.Contains(value);
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0028");
    }
}
