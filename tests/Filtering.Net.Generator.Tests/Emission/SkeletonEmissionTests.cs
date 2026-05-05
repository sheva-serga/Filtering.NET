namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>Snapshot tests for the class skeleton — partial-class declaration,
/// IFilterDefinition&lt;TEntity&gt; implementation, page-size constants, and placeholder method
/// bodies.</summary>
public class SkeletonEmissionTests
{
    [Fact]
    public async Task SimpleStringFilter_EmitsSkeleton()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;
            public class User { public string Name { get; set; } = ""; }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name))]
                private static partial void MapName();
            }
            """;
        var driver = GeneratorRunner.RunDriver(consumerSource);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        await Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public async Task ElementOnlyFilterClass_EmitsNoExplicitConstructors()
    {
        // Arrange — element-only (built-in StringFilter, no custom operators, no PropertyMap overrides).
        // The skeleton must NOT emit the _serializerOptions field, the [RequiresUnreferencedCode]
        // parameterless ctor, or the IJsonTypeInfoResolver-accepting ctor — none of those have
        // anything to do here, and the [RequiresUnreferencedCode] annotation would force consumers
        // to either eat IL2026 warnings under PublishAot or supply a resolver they don't need.
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;
            public class Order { public string Reference { get; set; } = ""; }
            [GenerateFilter<Order>]
            public partial class OrderFilter
            {
                [Map(nameof(Order.Reference))]
                private static partial void MapReference();
            }
            """;
        var driver = GeneratorRunner.RunDriver(consumerSource);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        await Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public async Task TypedValueFilterClass_EmitsDualConstructorsAndSerializerOptionsField()
    {
        // Arrange — typed-value (custom operator with non-null value type on a non-built-in profile).
        // The skeleton MUST emit the _serializerOptions field plus both constructors so consumers
        // can route a JsonSerializerContext through typed-value JSON deserialization.
        var consumerSource = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace Sample;
            [FilterProfile<string>(BasedOn = typeof(StringFilter))]
            public static class StringFilterPlus
            {
                [FilterOperator("fuzzy")]
                public static Expression<Func<string, string, bool>> Fuzzy => (column, value) => column.Contains(value);
            }
            public class Order { public string Reference { get; set; } = ""; }
            [GenerateFilter<Order>]
            public partial class OrderFilter
            {
                [Map(nameof(Order.Reference), Profile = typeof(StringFilterPlus), Only = new[] { "fuzzy" })]
                private static partial void MapReference();
            }
            """;
        var driver = GeneratorRunner.RunDriver(consumerSource);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        await Verify(driver).UseDirectory("Snapshots");
    }
}
