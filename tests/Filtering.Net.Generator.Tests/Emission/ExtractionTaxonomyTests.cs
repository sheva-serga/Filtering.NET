using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>One test per row of the extraction taxonomy in
/// <c>docs/superpowers/specs/2026-05-04-aot-filter-value-deserialization-design.md §3</c>.
/// Pins down the JsonElement-to-typed-value contract per
/// (declaration kind × profile kind × operator shape) and catches regressions.
///
/// Each <c>Row*</c> test follows the same shape:
/// <list type="number">
///   <item>declare a small filter source above the test,</item>
///   <item>build an in-memory queryable of <c>Sample.User</c> rows from a property dictionary,</item>
///   <item>invoke the generated <c>ApplyFilter</c> with one <see cref="FilterLeaf"/>,</item>
///   <item>assert the matched rows (and, for typed-value rows, that the resolver was queried).</item>
/// </list>
/// All reflection boilerplate lives in the helpers at the bottom of this file.</summary>
public class ExtractionTaxonomyTests
{
    // -------------------------------------------------------------------------
    // Row 1 — [Map] + built-in profile + scalar operator
    // Element-only: profile.TryGetValue → typed value → predicate
    // -------------------------------------------------------------------------

    private const string Row1Source = """
        using Filtering.Net;
        namespace Sample;
        public sealed class User { public string Email { get; set; } = ""; }
        [GenerateFilter<User>]
        public partial class UserFilter
        {
            [Map(nameof(User.Email), Only = new[] { "eq" })]
            private static partial void MapEmail();
        }
        """;

    [Fact]
    public void Row1_BuiltInScalar_ExtractsViaProfileTryGetValue()
    {
        // Arrange
        var (assembly, filter) = LoadAndActivate(Row1Source);
        var queryable = BuildQueryable(assembly,
            new() { ["Email"] = "alice@example.com" },
            new() { ["Email"] = "bob@example.com" });

        // Act
        var results = ApplyFilter(filter, queryable, Leaf("Email", "eq", "\"alice@example.com\""));

        // Assert
        results.Should().ContainSingle();
        GetProp<string>(results[0], "Email").Should().Be("alice@example.com");
    }

    // -------------------------------------------------------------------------
    // Row 2 — [Map] + built-in profile + array operator ("in")
    // Element-only: profile.TryGetArray → typed[] value → In predicate
    // -------------------------------------------------------------------------

    private const string Row2Source = """
        using Filtering.Net;
        namespace Sample;
        public sealed class User { public int Age { get; set; } }
        [GenerateFilter<User>]
        public partial class UserFilter
        {
            [Map(nameof(User.Age), Only = new[] { "in" })]
            private static partial void MapAge();
        }
        """;

    [Fact]
    public void Row2_BuiltInArray_ExtractsViaProfileTryGetArray()
    {
        // Arrange
        var (assembly, filter) = LoadAndActivate(Row2Source);
        var queryable = BuildQueryable(assembly,
            new() { ["Age"] = 25 },
            new() { ["Age"] = 30 },
            new() { ["Age"] = 35 });

        // Act
        var results = ApplyFilter(filter, queryable, Leaf("Age", "in", "[25, 35]"));

        // Assert
        results.Select(user => GetProp<int>(user, "Age")).Should().BeEquivalentTo([25, 35]);
    }

    // -------------------------------------------------------------------------
    // Row 3 — [Map] + built-in profile + unary operator ("isNull")
    // No-extraction arm: predicate applied directly, no value parsing
    // -------------------------------------------------------------------------

    // isNull is declared as Expression<Func<string, bool>> on StringFilter (non-nullable
    // string column) — the property type here is plain string (nullable-annotated reference
    // types are still System.String at the CLR level; the generator resolves them to StringFilter).
    private const string Row3Source = """
        using Filtering.Net;
        namespace Sample;
        public sealed class User { public string Email { get; set; } = ""; }
        [GenerateFilter<User>]
        public partial class UserFilter
        {
            [Map(nameof(User.Email), Only = new[] { "isNull" })]
            private static partial void MapEmail();
        }
        """;

    [Fact]
    public void Row3_BuiltInUnary_NoExtractionAppliesPredicateDirectly()
    {
        // Arrange — both rows have non-null Email; the isNull arm should match neither.
        var (assembly, filter) = LoadAndActivate(Row3Source);
        var queryable = BuildQueryable(assembly,
            new() { ["Email"] = "alice@example.com" },
            new() { ["Email"] = "bob@example.com" });

        // Act
        var results = ApplyFilter(filter, queryable, Leaf("Email", "isNull", "null"));

        // Assert
        results.Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // Row 4 — [Map] + user [FilterProfile<T>] inheriting built-in (inherited shape)
    // Element-only: inherited TryGetValue from StringFilter → typed value → predicate
    // -------------------------------------------------------------------------

    private const string Row4Source = """
        using Filtering.Net;
        namespace Sample;
        [FilterProfile<string>(BasedOn = typeof(global::Filtering.Net.StringFilter))]
        public static class CustomStringProfile { }
        public sealed class User { public string Email { get; set; } = ""; }
        [GenerateFilter<User>]
        public partial class UserFilter
        {
            [Map(nameof(User.Email), Profile = typeof(CustomStringProfile), Only = new[] { "eq" })]
            private static partial void MapEmail();
        }
        """;

    [Fact]
    public void Row4_UserProfileInheritedScalar_UsesInheritedProfileTryGetValue()
    {
        // Arrange
        var (assembly, filter) = LoadAndActivate(Row4Source);
        var queryable = BuildQueryable(assembly,
            new() { ["Email"] = "alice@example.com" },
            new() { ["Email"] = "bob@example.com" });

        // Act
        var results = ApplyFilter(filter, queryable, Leaf("Email", "eq", "\"alice@example.com\""));

        // Assert
        results.Should().ContainSingle();
        GetProp<string>(results[0], "Email").Should().Be("alice@example.com");
    }

    // -------------------------------------------------------------------------
    // Row 5 — [Map] + user [FilterProfile<T>] + [FilterOperator] with custom record value
    // Typed-value: resolver-routed JsonSerializer.Deserialize<T> → custom typed value → predicate
    // -------------------------------------------------------------------------

    private const string Row5Source = """
        using System;
        using System.Linq.Expressions;
        using Filtering.Net;
        namespace Sample;
        public sealed record RegexFilterValue(string Pattern);
        [FilterProfile<string>(BasedOn = typeof(global::Filtering.Net.StringFilter))]
        public static class StringWithRegexProfile
        {
            [FilterOperator("regex")]
            public static Expression<Func<string, RegexFilterValue, bool>> Regex =>
                (column, value) => System.Text.RegularExpressions.Regex.IsMatch(column, value.Pattern);
        }
        public sealed class User { public string Email { get; set; } = ""; }
        [GenerateFilter<User>]
        public partial class UserFilter
        {
            [Map(nameof(User.Email), Profile = typeof(StringWithRegexProfile), Only = new[] { "regex" })]
            private static partial void MapEmail();
        }
        """;

    [Fact]
    public void Row5_CustomScalar_DeserializesViaResolver()
    {
        // Arrange
        var (assembly, filter, resolver) = LoadAndActivateWithResolver(Row5Source);
        var queryable = BuildQueryable(assembly,
            new() { ["Email"] = "alice@example.com" },
            new() { ["Email"] = "bob@example.com" });

        // Act
        var results = ApplyFilter(filter, queryable, Leaf("Email", "regex", """{"Pattern":"^alice@"}"""));

        // Assert
        results.Should().ContainSingle();
        GetProp<string>(results[0], "Email").Should().Be("alice@example.com");
        resolver.RequestedTypes.Should().Contain(t => t.Name == "RegexFilterValue");
    }

    // -------------------------------------------------------------------------
    // Row 6 — [Map] + user [FilterProfile<T>] + [FilterOperator] with array value (int[])
    // Typed-value: resolver-routed JsonSerializer.Deserialize<int[]> → array → predicate
    // -------------------------------------------------------------------------

    private const string Row6Source = """
        using System;
        using System.Linq;
        using System.Linq.Expressions;
        using Filtering.Net;
        namespace Sample;
        [FilterProfile<int>(BasedOn = typeof(global::Filtering.Net.Int32Filter))]
        public static class CustomIntProfile
        {
            [FilterOperator("inArrays")]
            public static Expression<Func<int, int[], bool>> InArrays =>
                (column, values) => values.Contains(column);
        }
        public sealed class User { public int Age { get; set; } }
        [GenerateFilter<User>]
        public partial class UserFilter
        {
            [Map(nameof(User.Age), Profile = typeof(CustomIntProfile), Only = new[] { "inArrays" })]
            private static partial void MapAge();
        }
        """;

    [Fact]
    public void Row6_CustomArray_DeserializesViaResolver()
    {
        // Arrange
        var (assembly, filter, resolver) = LoadAndActivateWithResolver(Row6Source);
        var queryable = BuildQueryable(assembly,
            new() { ["Age"] = 25 },
            new() { ["Age"] = 30 });

        // Act
        var results = ApplyFilter(filter, queryable, Leaf("Age", "inArrays", "[25]"));

        // Assert
        results.Should().ContainSingle();
        GetProp<int>(results[0], "Age").Should().Be(25);
        resolver.RequestedTypes.Should().Contain(t => t == typeof(int[]));
    }

    // -------------------------------------------------------------------------
    // Row 7 — [Map] + user [FilterProfile<T>] + [FilterOperator] with no value param (custom unary)
    // No-extraction arm: column-only predicate applied directly
    // -------------------------------------------------------------------------

    // TryFindLambdaSyntax only recognises parenthesized lambdas — use (string column) form.
    private const string Row7Source = """
        using System;
        using System.Linq.Expressions;
        using Filtering.Net;
        namespace Sample;
        [FilterProfile<string>(BasedOn = typeof(global::Filtering.Net.StringFilter))]
        public static class CustomStringProfile
        {
            [FilterOperator("isEmpty")]
            public static Expression<Func<string, bool>> IsEmpty => (string column) => column.Length == 0;
        }
        public sealed class User { public string Name { get; set; } = ""; }
        [GenerateFilter<User>]
        public partial class UserFilter
        {
            [Map(nameof(User.Name), Profile = typeof(CustomStringProfile), Only = new[] { "isEmpty" })]
            private static partial void MapName();
        }
        """;

    [Fact]
    public void Row7_CustomUnary_NoExtractionAppliesColumnOnlyPredicate()
    {
        // Arrange
        var (assembly, filter) = LoadAndActivate(Row7Source);
        var queryable = BuildQueryable(assembly,
            new() { ["Name"] = "" },
            new() { ["Name"] = "Alice" });

        // Act
        var results = ApplyFilter(filter, queryable, Leaf("Name", "isEmpty", "null"));

        // Assert
        results.Should().ContainSingle();
        GetProp<string>(results[0], "Name").Should().BeEmpty();
    }

    // -------------------------------------------------------------------------
    // Row 8 — [PropertyMap] override with binary operator (value parameter present)
    // Typed-value: resolver-routed JsonSerializer.Deserialize<string> → value → predicate
    // -------------------------------------------------------------------------

    private const string Row8Source = """
        using Filtering.Net;
        namespace Sample;
        public sealed class User { public string FirstName { get; set; } = ""; public string LastName { get; set; } = ""; }
        [GenerateFilter<User>]
        public partial class UserFilter
        {
            [PropertyMap("FullName")]
            public static FilterRule<User, string> MapFullName(FilterRuleBuilder<User, string> builder) =>
                builder.For(u => u.FirstName + " " + u.LastName)
                       .Operator<string>("eq", (string column, string value) => column == value);
        }
        """;

    [Fact]
    public void Row8_PropertyMapOverrideWithValue_DeserializesViaResolver()
    {
        // Arrange
        var (assembly, filter, resolver) = LoadAndActivateWithResolver(Row8Source);
        var queryable = BuildQueryable(assembly,
            new() { ["FirstName"] = "Alice", ["LastName"] = "Smith" },
            new() { ["FirstName"] = "Bob", ["LastName"] = "Jones" });

        // Act
        var results = ApplyFilter(filter, queryable, Leaf("FullName", "eq", "\"Alice Smith\""));

        // Assert
        results.Should().ContainSingle();
        GetProp<string>(results[0], "FirstName").Should().Be("Alice");
        resolver.RequestedTypes.Should().Contain(t => t == typeof(string));
    }

    // -------------------------------------------------------------------------
    // Row 9 — [PropertyMap] override with unary operator (no value parameter)
    // No-extraction arm: column-only predicate applied directly, no resolver consulted
    // -------------------------------------------------------------------------

    private const string Row9Source = """
        using Filtering.Net;
        namespace Sample;
        public sealed class User { public string? FirstName { get; set; } public string? LastName { get; set; } }
        [GenerateFilter<User>]
        public partial class UserFilter
        {
            [PropertyMap("FullName")]
            public static FilterRule<User, string?> MapFullName(FilterRuleBuilder<User, string?> builder) =>
                builder.For(u => u.FirstName == null ? null : u.FirstName + " " + u.LastName)
                       .Operator("isNull", column => column == null);
        }
        """;

    // PropertyMapOverrideExtractor does not yet recognize the unary `(column => predicate)` lambda
    // shape on `.Operator(...)` chain calls, and FilterRuleBuilder lacks a unary
    // `Operator(string, Expression<Func<TValue, bool>>)` overload. This test pins the intended
    // behavior for a future follow-up that adds end-to-end unary PropertyMap override support.
    [Fact(Skip = "PropertyMap override unary lambda shape not yet supported by extractor")]
    public void Row9_PropertyMapOverrideUnary_NoExtractionAppliesColumnOnlyPredicate()
    {
        // Arrange
        var (assembly, filter, resolver) = LoadAndActivateWithResolver(Row9Source);
        var queryable = BuildQueryable(assembly,
            new() { ["FirstName"] = null },
            new() { ["FirstName"] = "Alice" });

        // Act
        var results = ApplyFilter(filter, queryable, Leaf("FullName", "isNull", "null"));

        // Assert
        results.Should().ContainSingle();
        GetProp<string?>(results[0], "FirstName").Should().BeNull();
        // Unary path: resolver is never queried for a value type.
        resolver.RequestedTypes.Should().BeEmpty();
    }

    // =========================================================================
    // Helpers — all reflection boilerplate lives here.
    // =========================================================================

    /// <summary>Loads the generated assembly and activates the filter through the parameterless
    /// constructor. Use this when the filter is element-only (no typed-value operators).</summary>
    private static (Assembly Assembly, object Filter) LoadAndActivate(
        string consumerSource,
        string filterTypeName = "Sample.UserFilter")
    {
        var assembly = RuntimeLoader.LoadGeneratedAssembly(consumerSource);
        var filter = Activator.CreateInstance(assembly.GetType(filterTypeName)!)!;
        return (assembly, filter);
    }

    /// <summary>Loads the generated assembly and activates the filter through the
    /// <see cref="IJsonTypeInfoResolver"/>-accepting constructor with a fresh tracking resolver.
    /// Use this when the filter has a typed-value operator and the test needs to assert which
    /// types the runtime asked the resolver about.</summary>
    private static (Assembly Assembly, object Filter, TrackingResolver Resolver) LoadAndActivateWithResolver(
        string consumerSource,
        string filterTypeName = "Sample.UserFilter")
    {
        var assembly = RuntimeLoader.LoadGeneratedAssembly(consumerSource);
        var resolver = new TrackingResolver(new DefaultJsonTypeInfoResolver());
        var ctor = assembly.GetType(filterTypeName)!.GetConstructor([typeof(IJsonTypeInfoResolver)])!;
        return (assembly, ctor.Invoke([resolver]), resolver);
    }

    /// <summary>Constructs an <see cref="IQueryable{T}"/> of <c>Sample.User</c> from a sequence of
    /// property dictionaries. Each dictionary describes one row: keys are property names on
    /// <c>Sample.User</c>, values are the value to set.</summary>
    private static object BuildQueryable(Assembly assembly, params Dictionary<string, object?>[] rowSpecs)
    {
        var entityType = assembly.GetType("Sample.User")!;
        var listType = typeof(List<>).MakeGenericType(entityType);
        var list = Activator.CreateInstance(listType)!;
        var addMethod = listType.GetMethod("Add")!;
        foreach (var spec in rowSpecs)
        {
            var instance = Activator.CreateInstance(entityType)!;
            foreach (var (propertyName, propertyValue) in spec)
                entityType.GetProperty(propertyName)!.SetValue(instance, propertyValue);
            addMethod.Invoke(list, [instance]);
        }
        return typeof(Queryable).GetMethods()
            .First(m => m.Name == "AsQueryable" && m.IsGenericMethod)
            .MakeGenericMethod(entityType)
            .Invoke(null, [list])!;
    }

    /// <summary>Invokes the filter's generated <c>ApplyFilter</c> over the queryable and
    /// materialises the resulting <see cref="IEnumerable"/> into a list.</summary>
    private static List<object> ApplyFilter(object filter, object queryable, FilterNode where)
    {
        var filtered = filter.GetType().GetMethod("ApplyFilter")!.Invoke(filter, [queryable, where])!;
        var results = new List<object>();
        foreach (var item in (System.Collections.IEnumerable)filtered)
            results.Add(item);
        return results;
    }

    /// <summary>Builds a <see cref="FilterLeaf"/> from a field name, an operator, and the JSON
    /// text of the value. <c>Leaf("Age", "in", "[25, 35]")</c> reads more naturally than the
    /// <c>JsonDocument.Parse(...).RootElement</c> chain at every call site.</summary>
    private static FilterLeaf Leaf(string field, string @operator, string jsonValue) =>
        new(field, @operator, JsonDocument.Parse(jsonValue).RootElement);

    /// <summary>Reads the named property off a reflectively-loaded entity instance.</summary>
    private static T GetProp<T>(object instance, string propertyName) =>
        (T)instance.GetType().GetProperty(propertyName)!.GetValue(instance)!;

    private sealed class TrackingResolver(IJsonTypeInfoResolver inner) : IJsonTypeInfoResolver
    {
        public List<Type> RequestedTypes { get; } = [];

        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            RequestedTypes.Add(type);
            return inner.GetTypeInfo(type, options);
        }
    }
}
