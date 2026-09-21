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
        [Map(nameof(User.Name), Sortable = true)]
        [Map(nameof(User.Age), Sortable = true)]
        public partial class UserFilter
        {
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
    public void ApplySorting_TwoSortItems_OrdersByTheFirstKeyThenTheSecond()
    {
        // Arrange — two users share a name, so the second sort key is the only thing that can order
        // them, and the two directions disagree so a single OrderBy cannot produce this sequence.
        var assembly = RuntimeLoader.LoadGeneratedAssembly(ConsumerSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var userType = assembly.GetType("Sample.User")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        var query = BuildQueryable(userType,
        [
            CreateUser(userType, "Bea", 30),
            CreateUser(userType, "Ada", 25),
            CreateUser(userType, "Ada", 40),
        ]);
        var sortItems = new List<SortItem> { new("Name", SortDir.Asc), new("Age", SortDir.Desc) };
        var applySortingMethod = userFilterType.GetMethods().First(m => m.Name == "ApplySorting" && m.GetParameters().Length == 4);
        var ageProperty = userType.GetProperty("Age")!;

        // Act
        var sortedQuery = applySortingMethod.Invoke(instance, [query, sortItems, (int?)null, (int?)null])!;
        var orderedAges = ((System.Collections.IEnumerable)sortedQuery).Cast<object>()
            .Select(user => (int)ageProperty.GetValue(user)!).ToList();

        // Assert
        orderedAges.Should().Equal(40, 25, 30);
    }

    [Fact]
    public void ApplySorting_PageWithoutPageSize_TakesTheDefaultPageSize()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(PageSettingsSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var userType = assembly.GetType("Sample.User")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        var query = BuildQueryable(userType, [.. Enumerable.Range(1, 30).Select(age => CreateUser(userType, $"User{age:00}", age))]);
        var sortItems = new List<SortItem> { new("Name", SortDir.Asc) };
        var applySortingMethod = userFilterType.GetMethods().First(m => m.Name == "ApplySorting" && m.GetParameters().Length == 4);

        // Act
        var pagedQuery = applySortingMethod.Invoke(instance, [query, sortItems, (int?)1, (int?)null])!;
        var pageRowCount = ((System.Collections.IEnumerable)pagedQuery).Cast<object>().Count();

        // Assert — [PageSettings(DefaultPageSize = 25)], not the built-in fallback of 50.
        pageRowCount.Should().Be(25);
    }

    [Fact]
    public void ValidatePage_SizeAboveTheDeclaredMaximum_ReportsPageSizeTooLarge()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(PageSettingsSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        var validateMethod = userFilterType.GetMethods().First(m => m.Name == "Validate" && m.GetParameters().Length == 2);

        // Act
        var withinDeclaredMaximum = (FilterValidationResult)validateMethod.Invoke(instance, [1, 100])!;
        var aboveDeclaredMaximum = (FilterValidationResult)validateMethod.Invoke(instance, [1, 150])!;

        // Assert — [PageSettings(MaxPageSize = 100)]; the built-in fallback of 200 would accept 150.
        withinDeclaredMaximum.IsValid.Should().BeTrue();
        aboveDeclaredMaximum.Errors.Should().ContainSingle(error => error.Code == FilterValidationCode.PageSizeTooLarge);
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

    // [PageSettings] values that differ from the generator's fallbacks (50 / 200), so a dropped
    // attribute shows up as behaviour rather than only as a snapshot diff.
    private const string PageSettingsSource = """
        using Filtering.Net;
        namespace Sample;
        public class User { public string Name { get; set; } = ""; public int Age { get; set; } }
        [GenerateFilter<User>]
        [PageSettings(MaxPageSize = 100, DefaultPageSize = 25)]
        [Map(nameof(User.Name), Sortable = true)]
        [Map(nameof(User.Age), Sortable = true)]
        public partial class UserFilter
        {
        }
        """;

    private const string InterceptorShapesSource = """
        using System.Linq;
        using System.Text.Json;
        using Filtering.Net;
        namespace Sample;
        public class User { public string Name { get; set; } = ""; public int Age { get; set; } }
        [GenerateFilter<User>]
        [Map(nameof(User.Name))]
        [Map(nameof(User.Age))]
        public partial class UserFilter
        {
            [InterceptValue(nameof(User.Name))]
            private static string[] TrimNames(InterceptContext context, string[] values) =>
                values.Select(value => value.Trim()).ToArray();

            [InterceptValue(nameof(User.Age), Raw = true)]
            private static int ParseAge(InterceptContext context, JsonElement element) =>
                element.ValueKind == JsonValueKind.String && element.GetString() == "thirty" ? 30 : element.GetInt32();
        }
        """;

    [Theory]
    [InlineData("Name", "in", "[\" Bob \"]", "Bob")]
    [InlineData("Age", "eq", "\"thirty\"", "Alice")]
    public void ApplyFilter_PrivateArrayAndRawInterceptors_TransformValues(string field, string operatorName, string valueJson, string expectedName)
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(InterceptorShapesSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var userType = assembly.GetType("Sample.User")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        var listType = typeof(List<>).MakeGenericType(userType);
        var typedList = Activator.CreateInstance(listType)!;
        var addMethod = listType.GetMethod("Add")!;
        addMethod.Invoke(typedList, [CreateUser(userType, "Alice", 30)]);
        addMethod.Invoke(typedList, [CreateUser(userType, "Bob", 25)]);
        var asQueryable = typeof(Queryable).GetMethods()
            .First(m => m.Name == "AsQueryable" && m.IsGenericMethod)
            .MakeGenericMethod(userType)
            .Invoke(null, [typedList])!;
        var leaf = new FilterLeaf(field, operatorName, JsonDocument.Parse(valueJson).RootElement);

        // Act
        var validationResult = (FilterValidationResult)userFilterType.GetMethods()
            .First(m => m.Name == "Validate" && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(FilterNode))
            .Invoke(instance, [leaf])!;
        var filteredQuery = userFilterType.GetMethod("ApplyFilter")!.Invoke(instance, [asQueryable, (object?)leaf])!;
        var nameProperty = userType.GetProperty("Name")!;
        var matchedNames = ((System.Collections.IEnumerable)filteredQuery).Cast<object>()
            .Select(user => (string)nameProperty.GetValue(user)!).ToList();

        // Assert
        validationResult.IsValid.Should().BeTrue();
        matchedNames.Should().Equal(expectedName);
    }

    private const string SimpleEmailFilterSource = """
        using Filtering.Net;
        namespace Sample;
        public class User { public string Email { get; set; } = string.Empty; }
        [GenerateFilter<User>]
        [Map(nameof(User.Email), Only = new[] { "eq" })]
        public partial class UserFilter
        {
        }
        """;

    // Typed-value source: forces the generator to emit the IJsonTypeInfoResolver-accepting ctor
    // that feeds the schema's serializer options. Element-only filter classes emit neither.
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
        [Map(nameof(User.Email), Profile = typeof(StringFilterPlus), Only = new[] { "fuzzy" })]
        public partial class UserFilter
        {
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
        var schema = userFilterType.GetProperty("Schema")!.GetValue(instance)!;
        var actualOptions = (System.Text.Json.JsonSerializerOptions)schema.GetType().GetProperty("SerializerOptions")!.GetValue(schema)!;

        // Assert
        resolverCtor.Should().NotBeNull("the IJsonTypeInfoResolver-accepting constructor must be emitted");
        actualOptions.TypeInfoResolver.Should().BeSameAs(suppliedResolver);
    }

    // A BasedOn profile that re-declares an inherited operator: the derived one wins, which the
    // generated bridge expresses with FilterProfile<T>.ExtendWithOverrides.
    private const string ShadowingProfileSource = """
        using System;
        using System.Linq.Expressions;
        using Filtering.Net;
        namespace Sample;
        [FilterProfile<string>(BasedOn = typeof(StringFilter))]
        public static class CaseInsensitiveStringFilter
        {
            [FilterOperator("contains")]
            public static Expression<Func<string, string, bool>> Contains =>
                (column, value) => column.ToLower().Contains(value.ToLower());
        }
        public class User { public string Name { get; set; } = ""; }
        [GenerateFilter<User>]
        [Map(nameof(User.Name), Profile = typeof(CaseInsensitiveStringFilter))]
        public partial class UserFilter
        {
        }
        """;

    [Fact]
    public void ApplyFilter_BasedOnProfileShadowingBaseOperator_UsesTheDerivedOperator()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(ShadowingProfileSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var userType = assembly.GetType("Sample.User")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        var query = BuildQueryable(userType, [CreateNamedUser(userType, "Alice"), CreateNamedUser(userType, "Bob")]);
        // The base StringFilter "contains" is case-sensitive, so a match on "ALIC" proves the
        // derived operator replaced it rather than being rejected or ignored.
        var leaf = new FilterLeaf("Name", "contains", JsonDocument.Parse("\"ALIC\"").RootElement);

        // Act
        var validationResult = (FilterValidationResult)userFilterType.GetMethods()
            .First(m => m.Name == "Validate" && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType == typeof(FilterNode))
            .Invoke(instance, [leaf])!;
        var filteredQuery = userFilterType.GetMethod("ApplyFilter")!.Invoke(instance, [query, (object?)leaf])!;
        var nameProperty = userType.GetProperty("Name")!;
        var matchedNames = ((System.Collections.IEnumerable)filteredQuery).Cast<object>()
            .Select(user => (string)nameProperty.GetValue(user)!).ToList();

        // Assert
        validationResult.IsValid.Should().BeTrue();
        matchedNames.Should().Equal("Alice");
    }

    [Fact]
    public void Schema_BasedOnProfileShadowingBaseOperator_ExposesEveryInheritedOperatorExactlyOnce()
    {
        // Arrange — the resolver advertises the merged operator set in AllowedOperators; the runtime
        // profile must carry exactly the same names, each one only once.
        var assembly = RuntimeLoader.LoadGeneratedAssembly(ShadowingProfileSource);
        var userFilterType = assembly.GetType("Sample.UserFilter")!;
        var instance = Activator.CreateInstance(userFilterType)!;
        var resolvedModel = GeneratorRunner.ExtractFilterClassModels(ShadowingProfileSource).Single();

        // Act
        var schema = userFilterType.GetProperty("Schema")!.GetValue(instance)!;
        var nameProperty = ((System.Collections.IEnumerable)schema.GetType().GetProperty("Properties")!.GetValue(schema)!)
            .Cast<object>().Single();
        var runtimeOperatorNames = ((System.Collections.IEnumerable)nameProperty.GetType()
            .GetProperty("Operators")!.GetValue(nameProperty)!).Cast<string>().ToList();

        // Assert
        runtimeOperatorNames.Should().BeEquivalentTo(resolvedModel.Properties.Single().AllowedOperators);
        runtimeOperatorNames.Should().OnlyHaveUniqueItems();
    }

    // Two enums that share a simple name across namespaces: the generated profile class name, the
    // profile full name and the AddSource hint name all have to stay distinct.
    private const string CollidingEnumNamesSource = """
        using Filtering.Net;
        namespace Sample.Billing
        {
            public enum Status { Draft, Paid }
            public class Invoice { public Status Status { get; set; } }
            [GenerateFilter<Invoice>]
            [Map(nameof(Invoice.Status))]
            public partial class InvoiceFilter { }
        }
        namespace Sample.Shipping
        {
            public enum Status { Packed, Sent }
            public class Parcel { public Status Status { get; set; } }
            [GenerateFilter<Parcel>]
            [Map(nameof(Parcel.Status))]
            public partial class ParcelFilter { }
        }
        """;

    [Fact]
    public void ApplyFilter_TwoSameNamedEnumsInDifferentNamespaces_FilterOnTheirOwnProfiles()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(CollidingEnumNamesSource);
        var invoiceFilterType = assembly.GetType("Sample.Billing.InvoiceFilter")!;
        var invoiceType = assembly.GetType("Sample.Billing.Invoice")!;
        var invoiceStatusType = assembly.GetType("Sample.Billing.Status")!;
        var paidInvoice = Activator.CreateInstance(invoiceType)!;
        invoiceType.GetProperty("Status")!.SetValue(paidInvoice, Enum.Parse(invoiceStatusType, "Paid"));
        var draftInvoice = Activator.CreateInstance(invoiceType)!;
        invoiceType.GetProperty("Status")!.SetValue(draftInvoice, Enum.Parse(invoiceStatusType, "Draft"));
        var query = BuildQueryable(invoiceType, [paidInvoice, draftInvoice]);
        var leaf = new FilterLeaf("Status", "eq", JsonDocument.Parse("\"Paid\"").RootElement);
        var instance = Activator.CreateInstance(invoiceFilterType)!;

        // Act
        var validationResult = (FilterValidationResult)invoiceFilterType.GetMethods()
            .First(m => m.Name == "Validate" && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType == typeof(FilterNode))
            .Invoke(instance, [leaf])!;
        var filteredQuery = invoiceFilterType.GetMethod("ApplyFilter")!.Invoke(instance, [query, (object?)leaf])!;
        var matched = ((System.Collections.IEnumerable)filteredQuery).Cast<object>().ToList();

        // Assert
        assembly.GetType("Sample.Shipping.ParcelFilter").Should().NotBeNull(
            because: "a hint-name collision between the two enum profiles would have failed the whole generator run");
        validationResult.IsValid.Should().BeTrue();
        matched.Should().ContainSingle().Which.Should().BeSameAs(paidInvoice);
    }

    // A typed-value operator whose value type is a record: malformed JSON for it must accumulate as
    // a validation error, never throw out of Validate.
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
        [Map(nameof(User.Email), Profile = typeof(StringWithRegexProfile), Only = new[] { "regex" })]
        public partial class UserFilter
        {
        }
        """;

    [Fact]
    public void ValidateRequest_TypedValuePropertyWithMalformedValue_ReportsTypeErrorWithoutThrowing()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(RegexOperatorSource);
        var filterType = assembly.GetType("Sample.UserFilter")!;
        var resolverCtor = filterType.GetConstructor([typeof(System.Text.Json.Serialization.Metadata.IJsonTypeInfoResolver)])!;
        var filterInstance = resolverCtor.Invoke([new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()]);
        // A JSON number where a RegexFilterValue object is expected — malformed for the custom type.
        var request = new FilterRequest
        {
            Where = new FilterLeaf("Email", "regex", JsonDocument.Parse("42").RootElement),
        };
        var validateMethod = filterType.GetMethods()
            .First(m => m.Name == "Validate"
                        && m.GetParameters().Length == 1
                        && m.GetParameters()[0].ParameterType == typeof(FilterRequest));

        // Act
        var validationResult = (FilterValidationResult)validateMethod.Invoke(filterInstance, [request])!;

        // Assert
        validationResult.IsValid.Should().BeFalse();
        validationResult.Errors.Should().Contain(e => e.Code == FilterValidationCode.InvalidValueType);
    }

    private static object BuildQueryable(Type elementType, IReadOnlyList<object> elements)
    {
        var listType = typeof(List<>).MakeGenericType(elementType);
        var typedList = Activator.CreateInstance(listType)!;
        var addMethod = listType.GetMethod("Add")!;
        foreach (var element in elements) addMethod.Invoke(typedList, [element]);
        return typeof(Queryable).GetMethods()
            .First(m => m.Name == "AsQueryable" && m.IsGenericMethod)
            .MakeGenericMethod(elementType)
            .Invoke(null, [typedList])!;
    }

    private static object CreateNamedUser(Type userType, string name)
    {
        var user = Activator.CreateInstance(userType)!;
        userType.GetProperty("Name")!.SetValue(user, name);
        return user;
    }

    private static object CreateUser(Type userType, string name, int age)
    {
        var user = Activator.CreateInstance(userType)!;
        userType.GetProperty("Name")!.SetValue(user, name);
        userType.GetProperty("Age")!.SetValue(user, age);
        return user;
    }
}
