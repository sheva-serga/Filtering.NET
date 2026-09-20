namespace Filtering.Net.Generator.Tests.Diagnostics;

public class Fn0010Tests
{
    [Fact]
    public void BasedOnReferencesNonProfile_FiresFN0010()
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
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0010");
    }

    [Fact]
    public void BasedOnReferencesAnotherProfile_DoesNotFireFN0010()
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
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0010");
    }

    [Fact]
    public void NoBasedOn_DoesNotFireFN0010()
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
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0010");
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
        DiagnosticTestHelpers.AssertDiagnosticHasAdditionalLocations(source, "FN0010", expectedAdditionalCount: 1);
    }
}
