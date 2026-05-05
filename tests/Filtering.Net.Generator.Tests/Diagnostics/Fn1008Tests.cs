using AwesomeAssertions;
using Microsoft.CodeAnalysis;

namespace Filtering.Net.Generator.Tests.Diagnostics;

/// <summary>Tests for FN1008 (FilterValueTypeUnregistered): fires when WarnUnregistered is opted in
/// and a typed-value type is not registered in any visible JsonSerializerContext.</summary>
public class Fn1008Tests
{
    [Fact]
    public void FilterValueTypeUnregisteredDescriptor_Id_IsFn1008()
    {
        // Arrange
        var descriptor = DiagnosticDescriptors.FilterValueTypeUnregistered;

        // Act
        var actualId = descriptor.Id;

        // Assert
        actualId.Should().Be("FN1008");
    }

    [Fact]
    public void FilterValueTypeUnregisteredDescriptor_DefaultSeverity_IsWarning()
    {
        // Arrange
        var descriptor = DiagnosticDescriptors.FilterValueTypeUnregistered;

        // Act
        var actualSeverity = descriptor.DefaultSeverity;

        // Assert
        actualSeverity.Should().Be(DiagnosticSeverity.Warning);
    }

    [Fact]
    public void GetDiagnostics_WithNoOptIn_DoesNotFireFN1008()
    {
        // Arrange
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace TestNs;
            public sealed record RegexFilterValue(string Pattern);
            [FilterProfile<string>(BasedOn = typeof(StringFilter))]
            public static class StringWithRegexProfile
            {
                [FilterOperator("regex")]
                public static Expression<Func<string, RegexFilterValue, bool>> Regex =>
                    (column, value) => System.Text.RegularExpressions.Regex.IsMatch(column, value.Pattern);
            }
            public sealed class User { public string Email { get; set; } = string.Empty; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Email), Profile = typeof(StringWithRegexProfile))]
                private static partial void MapEmail();
            }
            """;

        // Act
        var diagnostics = DiagnosticTestHelpers.GetDiagnostics(source);

        // Assert
        diagnostics.Should().NotContain(d => d.Id == "FN1008",
            because: "FN1008 should not fire when the assembly does not opt in via [assembly: FilterValueDiagnostics(WarnUnregistered = true)]");
    }

    [Fact]
    public void GetDiagnostics_WithOptIn_AndTypedValueTypeUnregistered_FiresFN1008WithTypeName()
    {
        // Arrange
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            [assembly: FilterValueDiagnostics(WarnUnregistered = true)]
            namespace TestNs;
            public sealed record RegexFilterValue(string Pattern);
            [FilterProfile<string>(BasedOn = typeof(StringFilter))]
            public static class StringWithRegexProfile
            {
                [FilterOperator("regex")]
                public static Expression<Func<string, RegexFilterValue, bool>> Regex =>
                    (column, value) => System.Text.RegularExpressions.Regex.IsMatch(column, value.Pattern);
            }
            public sealed class User { public string Email { get; set; } = string.Empty; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Email), Profile = typeof(StringWithRegexProfile))]
                private static partial void MapEmail();
            }
            """;

        // Act
        var diagnostics = DiagnosticTestHelpers.GetDiagnostics(source);

        // Assert
        var fn1008Diagnostics = diagnostics.Where(d => d.Id == "FN1008").ToList();
        fn1008Diagnostics.Should().ContainSingle(
            because: "exactly one unregistered typed-value type (RegexFilterValue) should produce one FN1008");
        fn1008Diagnostics[0].GetMessage().Should().Contain("RegexFilterValue",
            because: "the diagnostic message should identify the unregistered value type by name");
    }

    [Fact]
    public void GetDiagnostics_WithOptIn_AndTypedValueTypeRegistered_DoesNotFireFN1008()
    {
        // Arrange
        var source = """
            using System;
            using System.Linq.Expressions;
            using System.Text.Json.Serialization;
            using Filtering.Net;
            [assembly: FilterValueDiagnostics(WarnUnregistered = true)]
            namespace TestNs;
            public sealed record RegexFilterValue(string Pattern);
            [JsonSerializable(typeof(RegexFilterValue))]
            internal partial class AppJsonContext : JsonSerializerContext;
            [FilterProfile<string>(BasedOn = typeof(StringFilter))]
            public static class StringWithRegexProfile
            {
                [FilterOperator("regex")]
                public static Expression<Func<string, RegexFilterValue, bool>> Regex =>
                    (column, value) => System.Text.RegularExpressions.Regex.IsMatch(column, value.Pattern);
            }
            public sealed class User { public string Email { get; set; } = string.Empty; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Email), Profile = typeof(StringWithRegexProfile))]
                private static partial void MapEmail();
            }
            """;

        // Act
        var diagnostics = DiagnosticTestHelpers.GetDiagnostics(source);

        // Assert
        diagnostics.Should().NotContain(d => d.Id == "FN1008",
            because: "RegexFilterValue is registered in AppJsonContext so FN1008 should not fire");
    }

    [Fact]
    public void GetDiagnostics_WithOptIn_PropertyMapOverrideWithUnregisteredValueType_FiresFN1008()
    {
        // Arrange
        var source = """
            using Filtering.Net;
            [assembly: FilterValueDiagnostics(WarnUnregistered = true)]
            namespace TestNs;
            public sealed record FullNameSearch(string Term);
            public sealed class User
            {
                public string FirstName { get; set; } = "";
                public string LastName { get; set; } = "";
            }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [PropertyMap("FullName")]
                public static FilterRule<User, string> FullName(FilterRuleBuilder<User, string> builder) =>
                    builder.For(u => u.FirstName + " " + u.LastName)
                           .Operator("eq", (string column, FullNameSearch value) => column == value.Term);
            }
            """;

        // Act
        var diagnostics = DiagnosticTestHelpers.GetDiagnostics(source);

        // Assert
        var fn1008Diagnostics = diagnostics.Where(d => d.Id == "FN1008").ToList();
        fn1008Diagnostics.Should().NotBeEmpty(
            because: "FullNameSearch is an unregistered Path B value type used in a [PropertyMap] override operator");
        var mentionsFullNameSearch = fn1008Diagnostics.Any(d => d.GetMessage().IndexOf("FullNameSearch", StringComparison.Ordinal) >= 0);
        mentionsFullNameSearch.Should().BeTrue(
            because: "the FN1008 diagnostic message should identify FullNameSearch as the unregistered type");
    }

    [Fact]
    public void GetDiagnostics_WithOptIn_AndPathBTypeUnregistered_DiagnosticHasSourceLocation()
    {
        // Arrange
        var source = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            [assembly: FilterValueDiagnostics(WarnUnregistered = true)]
            namespace TestNs;
            public sealed record RegexFilterValue(string Pattern);
            [FilterProfile<string>(BasedOn = typeof(StringFilter))]
            public static class StringWithRegexProfile
            {
                [FilterOperator("regex")]
                public static Expression<Func<string, RegexFilterValue, bool>> Regex =>
                    (column, value) => System.Text.RegularExpressions.Regex.IsMatch(column, value.Pattern);
            }
            public sealed class User { public string Email { get; set; } = string.Empty; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Email), Profile = typeof(StringWithRegexProfile))]
                private static partial void MapEmail();
            }
            """;

        // Act
        var diagnostics = DiagnosticTestHelpers.GetDiagnostics(source);

        // Assert
        var fn1008Diagnostic = diagnostics.FirstOrDefault(d => d.Id == "FN1008");
        fn1008Diagnostic.Should().NotBeNull(because: "FN1008 must fire for the unregistered RegexFilterValue type");
        fn1008Diagnostic!.Location.Kind.Should().NotBe(LocationKind.None,
            because: "FN1008 should point at the [FilterOperator] declaration site, not Location.None");
        fn1008Diagnostic.Location.GetLineSpan().IsValid.Should().BeTrue(
            because: "the diagnostic location should carry a valid line span from the declaration site");
    }

    [Fact]
    public void GetDiagnostics_WithOptIn_PathATypesOnly_DoesNotFireFN1008()
    {
        // Arrange
        // string is a primitive Path A type for this property; FN1008 must not mention it.
        var source = """
            using Filtering.Net;
            [assembly: FilterValueDiagnostics(WarnUnregistered = true)]
            namespace TestNs;
            public sealed class User { public string Email { get; set; } = string.Empty; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Email))]
                private static partial void MapEmail();
            }
            """;

        // Act
        var diagnostics = DiagnosticTestHelpers.GetDiagnostics(source);

        // Assert
        diagnostics.Should().NotContain(d => d.Id == "FN1008",
            because: "string is a Path A type resolved via the built-in StringFilter profile and does not require JSON deserialization");
    }
}
