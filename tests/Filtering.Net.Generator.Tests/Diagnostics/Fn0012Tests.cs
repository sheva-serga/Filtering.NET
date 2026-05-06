namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0012Tests
{
    [Fact]
    public void BasedOnReferencesNonProfile_FiresFN0012()
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
        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0012");
    }

    [Fact]
    public void BasedOnReferencesAnotherProfile_DoesNotFireFN0012()
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
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0012");
    }

    [Fact]
    public void NoBasedOn_DoesNotFireFN0012()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            [FilterProfile<string>]
            public static class StandaloneProfile { }
            """;

        // Act
        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0012");
    }
}
