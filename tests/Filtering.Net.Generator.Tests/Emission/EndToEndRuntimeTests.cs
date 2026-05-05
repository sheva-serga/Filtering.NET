using System.Reflection;
using System.Text.Json;

using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>
/// End-to-end proof that the generator produces a working <see cref="IFilterDefinition{TEntity}"/>
/// implementation. Compiles a tiny consumer assembly via Roslyn (consumer source + generator
/// output), loads it into the current AppDomain, then drives the generated UserFilter through
/// each <c>Validate</c> overload + <c>ApplyFilter</c> + <c>ApplySorting</c>.
/// </summary>
public class EndToEndRuntimeTests
{
    private const string ConsumerSource = """
        using Filtering.Net;
        namespace Sample;
        public class User { public string Name { get; set; } = ""; public int Age { get; set; } }
        [GenerateFilter<User>]
        public partial class UserFilter
        {
            [Map(nameof(User.Name), Sortable = true)]
            private static partial void MapName();

            [Map(nameof(User.Age), Sortable = true)]
            private static partial void MapAge();
        }
        """;

    [Fact]
    public void GeneratedUserFilter_ImplementsAllInterfaceMembers()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(ConsumerSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;

        // Act
        var implementedInterfaces = userFilterType.GetInterfaces().Select(t => t.FullName).ToList();
        var methodNames = userFilterType.GetMethods().Select(m => m.Name).ToList();
        var validateOverloadCount = userFilterType.GetMethods()
            .Count(m => m.Name == "Validate" && m.GetParameters().Length is 1 or 2);

        // Assert
        userFilterType.Should().NotBeNull();
        implementedInterfaces.Should().Contain(name => name!.StartsWith("Filtering.Net.IFilterDefinition`1"));
        methodNames.Should().Contain("Validate");
        methodNames.Should().Contain("ApplyFilter");
        methodNames.Should().Contain("ApplySorting");
        // All four Validate overloads exist.
        validateOverloadCount.Should().Be(4);
    }

    [Fact]
    public void ValidateNode_RejectsUnknownField()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(ConsumerSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        // Build a FilterLeaf for a field that the generator didn't map.
        var unknownLeaf = new FilterLeaf("MysteryField", "eq", JsonDocument.Parse("\"x\"").RootElement);
        var validateMethod = userFilterType.GetMethods()
            .First(m => m.Name == "Validate" && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType == typeof(FilterNode));

        // Act
        var validationResult = (FilterValidationResult)validateMethod.Invoke(instance, [unknownLeaf])!;

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().ContainSingle(e => e.Code == FilterValidationCode.UnknownField);
    }

    [Fact]
    public void ValidateSort_RejectsUnknownSortField()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(ConsumerSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        var sortItems = new List<SortItem> { new SortItem("Mystery", SortDir.Asc) };
        var validateMethod = userFilterType.GetMethods()
            .First(m => m.Name == "Validate" && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType == typeof(IReadOnlyList<SortItem>));

        // Act
        var validationResult = (FilterValidationResult)validateMethod.Invoke(instance, [sortItems])!;

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().ContainSingle(e => e.Code == FilterValidationCode.NotSortable);
    }

    [Fact]
    public void ValidatePage_RejectsNegativePage()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(ConsumerSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        var validateMethod = userFilterType.GetMethods()
            .First(m => m.Name == "Validate" && m.GetParameters().Length == 2);

        // Act
        var validationResult = (FilterValidationResult)validateMethod.Invoke(instance, [0, 50])!;

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.Code == FilterValidationCode.PageInvalid);
    }

    [Fact]
    public void ValidateRequest_AggregatesErrorsFromAllSubValidations()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(ConsumerSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        var request = new FilterRequest
        {
            Where = new FilterLeaf("MysteryField", "eq", JsonDocument.Parse("\"x\"").RootElement),
            Sort = [new SortItem("AnotherMystery", SortDir.Desc)],
            Page = -1,
            PageSize = 10,
        };
        var validateMethod = userFilterType.GetMethods()
            .First(m => m.Name == "Validate" && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType == typeof(FilterRequest));

        // Act
        var validationResult = (FilterValidationResult)validateMethod.Invoke(instance, [request])!;

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.Code == FilterValidationCode.UnknownField);
        validationResult.Errors.Should().Contain(e => e.Code == FilterValidationCode.NotSortable);
        validationResult.Errors.Should().Contain(e => e.Code == FilterValidationCode.PageInvalid);
    }

    [Fact]
    public void ApplyFilter_ProducesExpectedResults()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(ConsumerSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var userType = assembly.GetType("Sample.User")!;
        var instance = Activator.CreateInstance(userFilterType)!;

        // Build an in-memory IQueryable<Sample.User> via reflection.
        var users = new List<object>
        {
            CreateUser(userType, "Alice", 30),
            CreateUser(userType, "Bob", 25),
            CreateUser(userType, "Charlie", 40),
        };
        var listType = typeof(List<>).MakeGenericType(userType);
        var typedList = Activator.CreateInstance(listType)!;
        var addMethod = listType.GetMethod("Add")!;
        foreach (var user in users) addMethod.Invoke(typedList, [user]);
        var asQueryable = typeof(Queryable).GetMethods()
            .First(m => m.Name == "AsQueryable" && m.IsGenericMethod)
            .MakeGenericMethod(userType)
            .Invoke(null, [typedList])!;
        var applyFilterMethod = userFilterType.GetMethod("ApplyFilter")!;

        // Act — ApplyFilter(query, FilterLeaf("Age", "gt", 28)).
        var filter = new FilterLeaf("Age", "gt", JsonDocument.Parse("28").RootElement);
        var filteredQuery = applyFilterMethod.Invoke(instance, [asQueryable, (object?)filter])!;

        // Materialise: iterate the resulting IEnumerable.
        var materialisedResults = new List<object>();
        foreach (var resultItem in (System.Collections.IEnumerable)filteredQuery)
        {
            materialisedResults.Add(resultItem);
        }

        // Assert
        materialisedResults.Should().HaveCount(2); // Alice (30) and Charlie (40)
        var nameProperty = userType.GetProperty("Name")!;
        var resultNames = materialisedResults.Select(u => (string)nameProperty.GetValue(u)!).ToList();
        resultNames.Should().BeEquivalentTo(["Alice", "Charlie"]);
    }

    private const string SimpleEmailFilterSource = """
        using Filtering.Net;
        namespace Sample;
        public class User { public string Email { get; set; } = string.Empty; }
        [GenerateFilter<User>]
        public partial class UserFilter
        {
            [Map(nameof(User.Email), Only = new[] { "eq" })]
            private static partial void MapEmail();
        }
        """;

    // Typed-value source: forces the generator to emit the IJsonTypeInfoResolver-accepting ctor
    // and _serializerOptions field. Element-only filter classes emit neither.
    private const string TypedValueEmailFilterSource = """
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
        public class User { public string Email { get; set; } = string.Empty; }
        [GenerateFilter<User>]
        public partial class UserFilter
        {
            [Map(nameof(User.Email), Profile = typeof(StringFilterPlus), Only = new[] { "fuzzy" })]
            private static partial void MapEmail();
        }
        """;

    [Fact]
    public void ParameterlessCtor_ProducesWorkingFilter()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(SimpleEmailFilterSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        var request = new FilterRequest
        {
            Where = new FilterLeaf("Email", "eq", JsonDocument.Parse("\"test@example.com\"").RootElement),
        };
        var validateMethod = userFilterType.GetMethods()
            .First(m => m.Name == "Validate" && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType == typeof(FilterRequest));

        // Act
        var validationResult = (FilterValidationResult)validateMethod.Invoke(instance, [request])!;

        // Assert
        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ResolverCtor_StoresSuppliedResolverInSerializerOptions()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(TypedValueEmailFilterSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var resolverInterfaceType = typeof(System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver);
        var resolverCtor = userFilterType.GetConstructor([resolverInterfaceType])!;
        var suppliedResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver();

        // Act
        var instance = resolverCtor.Invoke([suppliedResolver]);
        var serializerOptionsField = userFilterType.GetField(
            "_serializerOptions",
            BindingFlags.NonPublic | BindingFlags.Instance)!;
        var actualOptions = (System.Text.Json.JsonSerializerOptions)serializerOptionsField.GetValue(instance)!;

        // Assert
        resolverCtor.Should().NotBeNull("the IJsonTypeInfoResolver-accepting constructor must be emitted");
        serializerOptionsField.Should().NotBeNull("the _serializerOptions field must be emitted");
        actualOptions.TypeInfoResolver.Should().BeSameAs(suppliedResolver);
    }

    private static object CreateUser(Type userType, string name, int age)
    {
        var user = Activator.CreateInstance(userType)!;
        userType.GetProperty("Name")!.SetValue(user, name);
        userType.GetProperty("Age")!.SetValue(user, age);
        return user;
    }
}
