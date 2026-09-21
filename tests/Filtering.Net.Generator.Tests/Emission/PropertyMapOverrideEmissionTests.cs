using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>Snapshot + compile tests for <c>[PropertyMap]</c> override emission. The generator does
/// not parse the consumer's <c>builder.For(...).Operator(...)</c> chain: it emits one
/// <c>FilterProperty.MapRule</c> schema entry that calls the consumer's builder method once at
/// construction, and the rule's predicates run as written. The <c>.Operator(...)</c> calls are read
/// only to learn operator names and value types (typed-value detection and FN1008).</summary>
public class PropertyMapOverrideEmissionTests
{
    private const string TagsConsumerSource = """
        using System.Collections.Generic;
        using System.Linq;
        using Filtering.Net;
        namespace Sample
        {
            public class User
            {
                public List<string> Tags { get; set; } = new();
            }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [PropertyMap(nameof(User.Tags))]
                private static FilterRule<User, List<string>> MapTags(FilterRuleBuilder<User, List<string>> builder) =>
                    builder.For(user => user.Tags)
                        .Operator<string>("anyEq", (List<string> tags, string value) => tags.Any(tag => tag == value))
                        .Operator<string>("anyContains", (List<string> tags, string value) => tags.Any(tag => tag.Contains(value)));
            }
        }
        """;

    [Fact]
    public async Task TagsCollection_EmitsMapRuleSchemaEntry()
    {
        // Arrange
        var driver = GeneratorRunner.RunDriver(TagsConsumerSource);

        // Act
        // (no separate act step — Verifier.Verify is the verification)

        // Assert
        await Verify(driver).UseDirectory("Snapshots");
    }

    [Fact]
    public void TagsCollection_Compiles()
    {
        // Arrange
        // (source is declared as TagsConsumerSource above)

        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(TagsConsumerSource);
    }

    private const string FullNameOverrideSource = """
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
    public void PropertyMapOverride_ConsultsConsumerSuppliedResolver()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(FullNameOverrideSource);
        var trackingResolver = new TrackingResolver(new DefaultJsonTypeInfoResolver());
        var filterInstance = ActivateFilterWithResolver(assembly, "Sample.UserFilter", trackingResolver);

        var leafJson = JsonDocument.Parse("\"Alice Smith\"").RootElement;
        var whereNode = new FilterLeaf("FullName", "eq", leafJson);
        var queryable = BuildUserQueryable(assembly, [("Alice", "Smith"), ("Bob", "Jones")]);

        // Act
        var results = InvokeApplyFilter(assembly, "Sample.UserFilter", filterInstance, queryable, whereNode);

        // Assert
        results.Should().HaveCount(1);
        trackingResolver.RequestedTypes.Should().Contain(requestedType => requestedType == typeof(string));
    }

    private const string StatementBodiedOverrideSource = """
        using Filtering.Net;
        namespace Sample;

        public sealed class NameQuery { public string Term { get; set; } = ""; }

        public sealed class User { public string FirstName { get; set; } = ""; public string LastName { get; set; } = ""; }

        [GenerateFilter<User>]
        public partial class UserFilter
        {
            [PropertyMap("FullName")]
            public static FilterRule<User, string> MapFullName(FilterRuleBuilder<User, string> builder)
            {
                var rule = builder.For(user => user.FirstName + " " + user.LastName);
                rule.Operator<NameQuery>("matches", (string column, NameQuery value) => column.Contains(value.Term));
                return rule;
            }
        }
        """;

    [Fact]
    public void StatementBodiedRule_Compiles()
    {
        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(StatementBodiedOverrideSource);
    }

    [Fact]
    public void StatementBodiedRule_ThreadsTheSerializerOptionsThrough()
    {
        // Arrange — the typed-value operator lives outside the returned expression's chain, so a
        // purely syntactic scan of the return statement would miss it and emit a filter class that
        // throws FilterConfigurationException on construction with no way to supply a resolver.
        var assembly = RuntimeLoader.LoadGeneratedAssembly(StatementBodiedOverrideSource);
        var trackingResolver = new TrackingResolver(new DefaultJsonTypeInfoResolver());
        var filterInstance = ActivateFilterWithResolver(assembly, "Sample.UserFilter", trackingResolver);

        var leafJson = JsonDocument.Parse("""{"Term":"Alice"}""").RootElement;
        var whereNode = new FilterLeaf("FullName", "matches", leafJson);
        var queryable = BuildUserQueryable(assembly, [("Alice", "Smith"), ("Bob", "Jones")]);

        // Act
        var results = InvokeApplyFilter(assembly, "Sample.UserFilter", filterInstance, queryable, whereNode);

        // Assert
        results.Should().HaveCount(1);
        trackingResolver.RequestedTypes.Should().Contain(requestedType => requestedType.Name == "NameQuery");
    }

    private const string NullableValueOverrideSource = """
        using Filtering.Net;
        namespace Sample;

        public sealed class User { public string? MiddleName { get; set; } }

        [GenerateFilter<User>]
        public partial class UserFilter
        {
            [PropertyMap(nameof(User.MiddleName))]
            public static FilterRule<User, string?> MapMiddleName(FilterRuleBuilder<User, string?> builder) =>
                builder.For(user => user.MiddleName)
                       .Operator("isEmpty", (string? column) => column == null);
        }
        """;

    [Fact]
    public void NullableValueRule_Compiles()
    {
        // Act
        // (no separate act step — CompileVerifier.AssertCompilesCleanly is the verification)

        // Assert
        CompileVerifier.AssertCompilesCleanly(NullableValueOverrideSource);
    }

    [Fact]
    public void NullableValueRule_EmitsNullabilityWarningFreeCode()
    {
        // Arrange — FilterRuleBuilder<,> is invariant in TValue, so dropping the '?' from the emitted
        // construction is CS8620 in a file the consumer cannot edit.
        var (_, updatedCompilation) = GeneratorRunner.RunAndUpdate(NullableValueOverrideSource, excludeDiAbstractions: false);

        // Act
        var generatedFilePaths = updatedCompilation.SyntaxTrees
            .Select(tree => tree.FilePath)
            .Where(filePath => filePath.EndsWith("Sample.UserFilter.g.cs", StringComparison.Ordinal))
            .ToList();
        var generatedFileDiagnostics = updatedCompilation
            .GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(diagnostic => generatedFilePaths.Contains(diagnostic.Location.SourceTree?.FilePath ?? string.Empty))
            .ToList();

        // Assert
        generatedFileDiagnostics.Should().BeEmpty();
    }

    private static object ActivateFilterWithResolver(Assembly assembly, string filterTypeName, IJsonTypeInfoResolver resolver)
    {
        var filterType = assembly.GetType(filterTypeName)!;
        var resolverCtor = filterType.GetConstructor([typeof(IJsonTypeInfoResolver)])!;
        return resolverCtor.Invoke([resolver]);
    }

    private static object BuildUserQueryable(Assembly assembly, (string FirstName, string LastName)[] users)
    {
        var userType = assembly.GetType("Sample.User")!;
        var listType = typeof(List<>).MakeGenericType(userType);
        var typedList = Activator.CreateInstance(listType)!;
        var addMethod = listType.GetMethod("Add")!;
        var firstNameProperty = userType.GetProperty("FirstName")!;
        var lastNameProperty = userType.GetProperty("LastName")!;

        foreach (var (firstName, lastName) in users)
        {
            var userInstance = Activator.CreateInstance(userType)!;
            firstNameProperty.SetValue(userInstance, firstName);
            lastNameProperty.SetValue(userInstance, lastName);
            addMethod.Invoke(typedList, [userInstance]);
        }

        return typeof(Queryable)
            .GetMethods()
            .First(queryableMethod => queryableMethod.Name == "AsQueryable" && queryableMethod.IsGenericMethod)
            .MakeGenericMethod(userType)
            .Invoke(null, [typedList])!;
    }

    private static List<object> InvokeApplyFilter(
        Assembly assembly,
        string filterTypeName,
        object filterInstance,
        object queryable,
        FilterNode whereNode)
    {
        var filterType = assembly.GetType(filterTypeName)!;
        var applyFilterMethod = filterType.GetMethod("ApplyFilter")!;
        var filteredQuery = applyFilterMethod.Invoke(filterInstance, [queryable, whereNode])!;

        var materializedResults = new List<object>();
        foreach (var resultItem in (System.Collections.IEnumerable)filteredQuery)
        {
            materializedResults.Add(resultItem);
        }
        return materializedResults;
    }

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
