namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0011Tests
{
    [Fact]
    public void BasedOnReferencesNonProfile_FiresFN0011()
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
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0011");
    }

    [Fact]
    public void BasedOnReferencesAnotherProfile_DoesNotFireFN0011()
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
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0011");
    }

    [Fact]
    public void NoBasedOn_DoesNotFireFN0011()
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
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0011");
    }

    [Fact]
    public void InvalidBaseProfile_ReportsBasedOnTypeAsAdditionalLocation()
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
        DiagnosticTestHelpers.AssertDiagnosticHasAdditionalLocations(source, "FN0011", expectedAdditionalCount: 1);
    }
}
