using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>Snapshot tests for custom profile inheritance and custom
/// operator lambda inlining. Verifies the generator merges <c>BasedOn</c> chains and
/// emits user-declared <c>[FilterOperator]</c> bodies inline.</summary>
public class CustomProfileEmissionTests
{
    [Fact]
    public async Task ProfileInheritsStringFilter_AddsCustomFuzzyOperator()
    {
        // Arrange
        var consumerSource = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace Sample;
            public class User { public string Name { get; set; } = ""; }
            [FilterProfile<string>(BasedOn = typeof(StringFilter))]
            public static class FuzzyStringFilter
            {
                [FilterOperator("fuzzy")]
                public static Expression<Func<string, string, bool>> Fuzzy => (column, value) => column.Contains(value);
            }
            [GenerateFilter<User>]
            public partial class UserFilter
            {
                [Map(nameof(User.Name), Profile = typeof(FuzzyStringFilter))]
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
    public async Task ProfileInheritsDateTimeFilter_AddsWithinDaysOperator()
    {
        // Arrange
        var consumerSource = """
            using System;
            using System.Linq.Expressions;
            using Filtering.Net;
            namespace Sample;
            public class Audit { public DateTime CreatedAt { get; set; } }
            [FilterProfile<DateTime>(BasedOn = typeof(DateTimeFilter))]
            public static class RecencyFilter
            {
                [FilterOperator("withinDays")]
                public static Expression<Func<DateTime, int, bool>> WithinDays
                    => (date, days) => date >= DateTime.UtcNow.AddDays(-days);
            }
            [GenerateFilter<Audit>]
            public partial class AuditFilter
            {
                [Map(nameof(Audit.CreatedAt), Profile = typeof(RecencyFilter), Sortable = true)]
                private static partial void MapCreatedAt();
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
    public void CustomOperator_ConsultsConsumerSuppliedResolver()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(RegexOperatorSource);
        var trackingResolver = new TrackingResolver(new DefaultJsonTypeInfoResolver());
        var filterInstance = ActivateFilterWithResolver(assembly, "Sample.UserFilter", trackingResolver);

        var leafJson = JsonDocument.Parse("""{"Pattern":"^alice@"}""").RootElement;
        var request = new FilterRequest { Where = new FilterLeaf("Email", "regex", leafJson) };
        var users = BuildUserQueryable(assembly, ["alice@example.com", "bob@example.com"]);

        // Act
        var results = InvokeApplyFilter(assembly, "Sample.UserFilter", filterInstance, users, request.Where!);

        // Assert
        results.Should().HaveCount(1);
        GetEmailProperty(assembly, results[0]).Should().Be("alice@example.com");
        trackingResolver.RequestedTypes.Should().Contain(requestedType => requestedType.Name == "RegexFilterValue");
    }

    [Fact]
    public void CustomOperator_UnregisteredType_ThrowsFilterDispatchException_PreservingInnerException()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(RegexOperatorSource);
        var emptyResolver = new EmptyResolver();
        var filterInstance = ActivateFilterWithResolver(assembly, "Sample.UserFilter", emptyResolver);

        var leafJson = JsonDocument.Parse("""{"Pattern":"^alice@"}""").RootElement;
        var request = new FilterRequest { Where = new FilterLeaf("Email", "regex", leafJson) };
        var users = BuildUserQueryable(assembly, ["alice@example.com"]);

        // Act
        var thrownException = Assert.Throws<TargetInvocationException>(
            () => InvokeApplyFilter(assembly, "Sample.UserFilter", filterInstance, users, request.Where!));

        // Assert — unwrap the reflective wrapper to reach the Filtering.Net exception.
        var filterDispatchException = thrownException.InnerException.Should().BeOfType<FilterDispatchException>().Subject;
        filterDispatchException.InnerException.Should().NotBeNull();
        filterDispatchException.InnerException.Should().BeOfType<NotSupportedException>();
    }

    private static object ActivateFilterWithResolver(Assembly assembly, string filterTypeName, IJsonTypeInfoResolver resolver)
    {
        var filterType = assembly.GetType(filterTypeName)!;
        var resolverCtor = filterType.GetConstructor([typeof(IJsonTypeInfoResolver)])!;
        return resolverCtor.Invoke([resolver]);
    }

    private static object BuildUserQueryable(Assembly assembly, string[] emailAddresses)
    {
        var userType = assembly.GetType("Sample.User")!;
        var listType = typeof(List<>).MakeGenericType(userType);
        var typedList = Activator.CreateInstance(listType)!;
        var addMethod = listType.GetMethod("Add")!;
        var emailProperty = userType.GetProperty("Email")!;

        foreach (var emailAddress in emailAddresses)
        {
            var userInstance = Activator.CreateInstance(userType)!;
            emailProperty.SetValue(userInstance, emailAddress);
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

    private static string GetEmailProperty(Assembly assembly, object userInstance)
    {
        var userType = assembly.GetType("Sample.User")!;
        return (string)userType.GetProperty("Email")!.GetValue(userInstance)!;
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

    private sealed class EmptyResolver : IJsonTypeInfoResolver
    {
        public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options) => null;
    }
}
