using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>Snapshot tests for the Validate(FilterNode?) emission — per-property
/// validators, JSON value-extraction helpers, and interceptor dry-run integration.</summary>
public class ValidateNodeEmissionTests
{
    [Fact]
    public async Task NumericFilter_EmitsValidateWithNumericExtractors()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;
            public class User { public int Age { get; set; } }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Age))]
                private static partial void MapAge();
            }
            """;
        var driver = GeneratorRunner.RunDriver(consumerSource);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        await Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public async Task StringWithInterceptor_EmitsInterceptorDryRun()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;
            public class User { public string Email { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Email))]
                private static partial void MapEmail();

                [InterceptValue(nameof(User.Email))]
                private static string InterceptEmail(InterceptContext context, string value)
                    => value.Trim().ToLowerInvariant();
            }
            """;
        var driver = GeneratorRunner.RunDriver(consumerSource);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        await Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public async Task AliasOnString_EmitsBothFieldKeysInDispatcher()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name), Alias = "displayName")]
                private static partial void MapName();
            }
            """;
        var driver = GeneratorRunner.RunDriver(consumerSource);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        await Verify(driver).UseDirectory("Snapshots");
    }

    private const string RegexOperatorSource = """
        using System;
        using System.Linq.Expressions;
        using Filtering.Net;

        namespace Sample;

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
            [Map(nameof(User.Email), Profile = typeof(StringWithRegexProfile), Only = new[] { "regex" })]
            private static partial void MapEmail();
        }
        """;

    [Fact]
    public void Validate_OnTypedValueProperty_WithMalformedValue_ReportsTypeError()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(RegexOperatorSource);
        var filterType = assembly.GetType("Sample.UserFilter")!;
        var resolverCtor = filterType.GetConstructor([typeof(IJsonTypeInfoResolver)])!;
        var filterInstance = resolverCtor.Invoke([new DefaultJsonTypeInfoResolver()]);

        // A JSON number where a RegexFilterValue object is expected — malformed for the custom type.
        var leafJson = JsonDocument.Parse("42").RootElement;
        var request = new FilterRequest { Where = new FilterLeaf("Email", "regex", leafJson) };

        var validateMethod = filterType.GetMethods()
            .First(m => m.Name == "Validate"
                        && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType == typeof(FilterRequest));

        // Act — validate accumulates errors, must NOT throw.
        var validationResult = (FilterValidationResult)validateMethod.Invoke(filterInstance, [request])!;

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.Code == FilterValidationCode.InvalidValueType);
    }
}
