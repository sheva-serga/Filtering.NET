using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0027 (FilterClassPlacementInvalid): the generated half is a top-level partial
/// in the class's namespace, so a nested or generic <c>[GenerateFilter]</c> class cannot be served.</summary>
public class Fn0027Tests
{
    private const string NestedFilterClassSource = """
        using Filtering.Net;
        namespace TestNs;
        public class Invoice { public string Number { get; set; } = ""; }
        public partial class Billing
        {
            [GenerateFilter<Invoice>]
            [Map(nameof(Invoice.Number))]
            public partial class ItemFilter { }
        }
        """;

    [Fact]
    public void NestedFilterClass_FiresFN0027()
    {
        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(NestedFilterClassSource, "FN0027");
    }

    [Fact]
    public void NestedFilterClass_EmitsNoPhantomTopLevelClass()
    {
        // Arrange
        var runResult = GeneratorRunner.RunDriver(NestedFilterClassSource, excludeDiAbstractions: false).GetRunResult();

        // Act
        var generatedFileNames = runResult.GeneratedTrees.Select(tree => Path.GetFileName(tree.FilePath)).ToList();

        // Assert
        generatedFileNames.Should().NotContain(fileName => fileName.Contains("ItemFilter", StringComparison.Ordinal),
            because: "a top-level ItemFilter partial would compile on its own and leave the user's nested class orphaned");
    }

    [Fact]
    public void TwoNestedFilterClassesWithTheSameSimpleName_DoNotCollideOnHintName()
    {
        // Arrange — identical hint names would throw inside AddSource and fail the generator (CS8785).
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class Invoice { public string Number { get; set; } = ""; }
            public class Parcel { public string Code { get; set; } = ""; }
            public partial class Billing
            {
                [GenerateFilter<Invoice>]
                [Map(nameof(Invoice.Number))]
                public partial class ItemFilter { }
            }
            public partial class Shipping
            {
                [GenerateFilter<Parcel>]
                [Map(nameof(Parcel.Code))]
                public partial class ItemFilter { }
            }
            """;

        // Act
        var diagnostics = DiagnosticTestHelpers.GetDiagnostics(source);

        // Assert
        diagnostics.Where(diagnostic => diagnostic.Id == "FN0027").Should().HaveCount(2);
        diagnostics.Should().NotContain(diagnostic => diagnostic.Id == "CS8785");
    }

    [Fact]
    public void GenericFilterClass_FiresFN0027()
    {
        // Arrange — the emitted non-generic partial and the user's generic class coexist legally,
        // so nothing would join them and the user's class keeps no base type.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            public partial class UserFilter<TTag> { }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0027");
    }

    [Fact]
    public void TopLevelNonGenericFilterClass_DoesNotFireFN0027()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name))]
            public partial class UserFilter { }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0027");
    }
}
