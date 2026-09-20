namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0004 (IncompatibleProfile): explicit Profile = typeof(...) doesn't match the property's CLR type.</summary>
public class Fn0004Tests
{
    [Fact]
    public void Int32FilterOnStringProperty_FiresFN0004()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name), Profile = typeof(Int32Filter))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0004");
    }

    [Fact]
    public void StringFilterOnStringProperty_DoesNotFireFN0004()
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
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0004");
    }

    [Fact]
    public void IncompatibleProfile_MetadataProfile_DoesNotCarryAdditionalLocation()
    {
        // Arrange — FN0004 only fires for built-in profile mismatches (IsCompatible defaults to
        // true for source-defined profiles); built-in profiles live in metadata, so their
        // Locations.FirstOrDefault() carries Kind=MetadataFile, which LocationInfo.FromLocation
        // filters out (metadata locations cannot survive a Location.Create round-trip). The
        // additional-location count is therefore 0 in practice — the audit's intent (point at the
        // profile declaration) is unrealisable until the resolver can synthesise a source-side
        // hint for metadata profiles.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Name), Profile = typeof(Int32Filter))]
            public partial class UserFilter
            {
            }
            """;

        // Act
        // (no separate act step — AssertDiagnosticHasAdditionalLocations is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnosticHasAdditionalLocations(source, "FN0004", expectedAdditionalCount: 0);
    }
}
