namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0013 (InvalidBaseProfile): [FilterProfile(BasedOn = typeof(X))] X is not itself marked [FilterProfile].</summary>
public class Fn0013Tests
{
    [Fact]
    public void BasedOnReferencesNonProfile_FiresFN0013()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class NotAProfile { }
            [FilterProfile<string>(BasedOn = typeof(NotAProfile))]
            public static class CustomProfile { }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0013");
    }

    [Fact]
    public void BasedOnReferencesAnotherProfile_DoesNotFireFN0013()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>]
            public static class BaseProfile { }
            [FilterProfile<string>(BasedOn = typeof(BaseProfile))]
            public static class DerivedProfile { }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0013");
    }

    [Fact]
    public void NoBasedOn_DoesNotFireFN0013()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>]
            public static class StandaloneProfile { }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0013");
    }
}
