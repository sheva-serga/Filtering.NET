using AwesomeAssertions;

using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0024 (NullableValueTypeInPath): a dotted [Map] path reads a member off a Nullable&lt;T&gt;.</summary>
public class Fn0024Tests
{
    private const string NullableIntermediateSource = """
        using System;
        using Filtering.Net;
        namespace TestNs;
        public class Order { public DateTime? Created { get; set; } }
        [GenerateFilter<Order>]
        [Map("Created.Year", Sortable = true)]
        public partial class OrderFilter
        {
        }
        """;

    [Fact]
    public void NullableValueTypeIntermediateSegment_FiresFN0024()
    {
        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(NullableIntermediateSource, "FN0024");
    }

    [Fact]
    public void NullableValueTypeIntermediateSegment_DoesNotEmitAnUncompilableAccessor()
    {
        // Act
        var (_, updatedCompilation) = GeneratorRunner.RunAndUpdate(NullableIntermediateSource, excludeDiAbstractions: false);
        var compileErrors = updatedCompilation
            .GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToList();

        // Assert
        compileErrors.Should().BeEmpty(because: "the mapping is rejected with FN0024 instead of emitting entity.Created.Year");
    }

    [Fact]
    public void NonNullableValueTypeIntermediateSegment_DoesNotFireFN0024()
    {
        // Arrange
        var source = """
            using System;
            using Filtering.Net;
            namespace TestNs;
            public class Order { public DateTime Created { get; set; } }
            [GenerateFilter<Order>]
            [Map("Created.Year", Sortable = true)]
            public partial class OrderFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0024");
    }

    [Fact]
    public void NullableValueTypeLeafSegment_DoesNotFireFN0024()
    {
        // Arrange — the leaf's own nullability is what MapNullable handles, not a broken accessor.
        var source = """
            using System;
            using Filtering.Net;
            namespace TestNs;
            public class Order { public DateTime? Created { get; set; } }
            [GenerateFilter<Order>]
            [Map(nameof(Order.Created), Sortable = true)]
            public partial class OrderFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0024");
    }
}
