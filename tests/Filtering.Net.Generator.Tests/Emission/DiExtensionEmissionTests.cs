using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using AwesomeAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>Tests for the assembly-wide <c>AddFiltering</c> DI extension. The first two tests
/// snapshot what the generator emits with / without a Microsoft.Extensions.DependencyInjection
/// reference. The last two compile + load the emitted code and verify the resolver supplied to
/// <c>AddFiltering</c> ends up inside the filter's <c>JsonSerializerOptions.TypeInfoResolver</c>.
/// </summary>
public class DiExtensionEmissionTests
{
    [Fact]
    public Task WithDiReference_EmitsFilteringServiceCollectionExtensions()
    {
        // Arrange
        var consumerSource = """
            using Filtering.Net;
            namespace Sample;
            public class User { public string Name { get; set; } = ""; }
            public class Order { public int Id { get; set; } }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name))]
                private static partial void MapName();
            }
            [GenerateFilter<Order>]
            public partial class OrderFilter
            {
                [Map(nameof(Order.Id))]
                private static partial void MapId();
            }
            """;
        var driver = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: false);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        return Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public void WithoutDiReference_SkipsExtensionEmission()
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

        // Act — force-exclude DI Abstractions so the gating in DiExtensionEmitter suppresses emission.
        var runResult = GeneratorRunner.RunDriver(consumerSource, excludeDiAbstractions: true).GetRunResult();
        var hintNames = runResult.GeneratedTrees.Select(tree => Path.GetFileName(tree.FilePath));

        // Assert
        hintNames.Should().NotContain(name => name.Contains("FilteringServiceCollectionExtensions"));
    }

    // A custom operator with a non-null typed value forces the generator to emit the
    // IJsonTypeInfoResolver-accepting ctor and _serializerOptions field that the AddFiltering
    // tests below reflect on. Element-only filter classes don't emit either.
    private const string TypedValueFilterSource = """
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
        public sealed class User { public string Email { get; set; } = string.Empty; }
        [GenerateFilter<User>]
        public partial class UserFilter
        {
            [Map(nameof(User.Email), Profile = typeof(StringFilterPlus), Only = new[] { "fuzzy" })]
            private static partial void MapEmail();
        }
        """;

    [Fact]
    public void AddFiltering_WithResolverInstance_StoresResolverInFilterSerializerOptions()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(TypedValueFilterSource);
        var suppliedResolver = new DefaultJsonTypeInfoResolver();
        var services = new ServiceCollection();
        var addFiltering = GetAddFilteringExtension(assembly, typeof(IServiceCollection), typeof(IJsonTypeInfoResolver));

        // Act
        addFiltering.Invoke(null, [services, suppliedResolver]);
        var filter = ResolveFilter(services.BuildServiceProvider(), assembly);

        // Assert — the resolver passed to AddFiltering ends up inside the filter's serializer options.
        GetSerializerResolver(filter).Should().BeSameAs(suppliedResolver);
    }

    [Fact]
    public void AddFiltering_WithFactory_InvokesFactoryAtConstructionTime()
    {
        // Arrange — register a resolver in DI, then ask the factory to fetch it.
        var assembly = RuntimeLoader.LoadGeneratedAssembly(TypedValueFilterSource);
        var diRegisteredResolver = new DefaultJsonTypeInfoResolver();
        var services = new ServiceCollection();
        services.AddSingleton<IJsonTypeInfoResolver>(diRegisteredResolver);
        var addFiltering = GetAddFilteringExtension(
            assembly,
            typeof(IServiceCollection),
            typeof(Func<IServiceProvider, IJsonTypeInfoResolver>));
        Func<IServiceProvider, IJsonTypeInfoResolver> resolverFactory =
            sp => sp.GetRequiredService<IJsonTypeInfoResolver>();

        // Act
        addFiltering.Invoke(null, [services, resolverFactory]);
        var filter = ResolveFilter(services.BuildServiceProvider(), assembly);

        // Assert — the DI-registered resolver flows through the factory into the filter.
        GetSerializerResolver(filter).Should().BeSameAs(diRegisteredResolver);
    }

    private static MethodInfo GetAddFilteringExtension(Assembly assembly, params Type[] parameterTypes) =>
        assembly.GetType("Filtering.Net.FilteringServiceCollectionExtensions")!
            .GetMethod("AddFiltering", parameterTypes)!;

    private static object ResolveFilter(IServiceProvider serviceProvider, Assembly assembly)
    {
        var entityType = assembly.GetType("Sample.User")!;
        var filterDefinitionType = typeof(IFilterDefinition<>).MakeGenericType(entityType);
        return serviceProvider.GetRequiredService(filterDefinitionType);
    }

    private static IJsonTypeInfoResolver? GetSerializerResolver(object filterInstance)
    {
        var optionsField = filterInstance.GetType()
            .GetField("_serializerOptions", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var options = (JsonSerializerOptions)optionsField.GetValue(filterInstance)!;
        return options.TypeInfoResolver;
    }
}
