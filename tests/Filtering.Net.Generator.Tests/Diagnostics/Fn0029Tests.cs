namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN0029 (InterceptorSignatureInvalid): an <c>[InterceptValue]</c> method the
/// generated <c>CreateSchema</c> cannot pass to Intercept / InterceptArray / InterceptRaw.</summary>
public class Fn0029Tests
{
    [Fact]
    public void InterceptorIsNotStatic_FiresFN0029()
    {
        // Arrange — the method group is spliced into a static method, so this is CS0120 in
        // generated source unless the generator rejects it first.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Email { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Email))]
            public partial class UserFilter
            {
                [InterceptValue(nameof(User.Email))]
                private string Normalize(InterceptContext context, string value) => value.Trim();
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0029");
    }

    [Fact]
    public void InterceptorMissingContextParameter_FiresFN0029()
    {
        // Arrange — one parameter means the interceptor is dropped and silently never runs.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Email { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Email))]
            public partial class UserFilter
            {
                [InterceptValue(nameof(User.Email))]
                private static string Normalize(string value) => value.Trim();
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0029");
    }

    [Fact]
    public void InterceptorReturnsADifferentTypeThanItTakes_FiresFN0029()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Email { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Email))]
            public partial class UserFilter
            {
                [InterceptValue(nameof(User.Email))]
                private static int Normalize(InterceptContext context, string value) => value.Length;
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0029");
    }

    [Fact]
    public void RawInterceptorWithoutJsonElement_FiresFN0029()
    {
        // Arrange — Raw = true routes to InterceptRaw, whose value parameter is a JsonElement.
        var source = """
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Email { get; set; } = ""; }
            [GenerateFilter<User>]
            [Map(nameof(User.Email))]
            public partial class UserFilter
            {
                [InterceptValue(nameof(User.Email), Raw = true)]
                private static string Normalize(InterceptContext context, string value) => value.Trim();
            }
            """;

        // Act
        // (no separate act step — AssertDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertDiagnostic(source, "FN0029");
    }

    [Fact]
    public void WellFormedScalarArrayAndRawInterceptors_DoNotFireFN0029()
    {
        // Arrange
        var source = """
            using System.Linq;
            using System.Text.Json;
            using Filtering.Net;
            namespace TestNs;
            public class User { public string Email { get; set; } = ""; public string Name { get; set; } = ""; public int Age { get; set; } }
            [GenerateFilter<User>]
            [Map(nameof(User.Email))]
            [Map(nameof(User.Name))]
            [Map(nameof(User.Age))]
            public partial class UserFilter
            {
                [InterceptValue(nameof(User.Email))]
                private static string Normalize(InterceptContext context, string value) => value.Trim();

                [InterceptValue(nameof(User.Name))]
                private static string[] TrimNames(InterceptContext context, string[] values) =>
                    values.Select(value => value.Trim()).ToArray();

                [InterceptValue(nameof(User.Age), Raw = true)]
                private static int ParseAge(InterceptContext context, JsonElement element) => element.GetInt32();
            }
            """;

        // Act
        // (no separate act step — AssertNoDiagnostic is the verification)

        // Assert
        DiagnosticTestHelpers.AssertNoDiagnostic(source, "FN0029");
    }
}
