using System.Text.Json;

namespace Filtering.Net.Tests.Engine;

public sealed class Company
{
    public string Country { get; set; } = "";
}

public sealed class Department
{
    public string Name { get; set; } = "";

    public Company Company { get; set; } = new();
}

public sealed class Person
{
    public string Name { get; set; } = "";

    public int Age { get; set; }

    public int? Score { get; set; }

    public short? Rank { get; set; }

    public DateTime? LastSeen { get; set; }

    public Department Department { get; set; } = new();
}

internal static class EngineTestData
{
    public static IQueryable<Person> People() => new[]
    {
        new Person { Name = "Alice", Age = 30, Score = 10, Rank = 1, LastSeen = new DateTime(2026, 1, 1), Department = new Department { Name = "Sales", Company = new Company { Country = "UA" } } },
        new Person { Name = "Bob", Age = 25, Score = null, Rank = null, LastSeen = null, Department = new Department { Name = "Ops", Company = new Company { Country = "PL" } } },
        new Person { Name = "Carol", Age = 41, Score = 20, Rank = 2, LastSeen = new DateTime(2026, 6, 1), Department = new Department { Name = "Sales", Company = new Company { Country = "PL" } } },
    }.AsQueryable();

    public static FilterLeaf Leaf(string field, string operatorName, string valueJson) =>
        new(field, operatorName, JsonDocument.Parse(valueJson).RootElement);

    public static FilterGroup Group(LogicalOp logicalOp, params FilterNode[] children) => new(logicalOp, children);

    public static FilterDefinition<Person> Definition(FilterSettings? settings = null, JsonSerializerOptions? serializerOptions = null, params FilterProperty<Person>[] properties)
    {
        var schemaBuilder = new FilterSchemaBuilder<Person>(settings ?? new FilterSettings(), serializerOptions);
        foreach (var property in properties)
        {
            schemaBuilder.Add(property);
        }
        return new FilterDefinition<Person>(schemaBuilder.Build());
    }

    public static FilterDefinition<Person> StandardDefinition(FilterSettings? settings = null) => Definition(
        settings,
        serializerOptions: null,
        FilterProperty.Map<Person, string>("Name", person => person.Name, StringFilter.Profile).Sortable().Build(),
        FilterProperty.Map<Person, int>("Age", person => person.Age, Int32Filter.Profile).Alias("years").Sortable(SortDir.Desc).Build(),
        FilterProperty.MapNullable<Person, int>("Score", person => person.Score, Int32Filter.Profile).Build(),
        FilterProperty.MapNullable<Person, short>("Rank", person => person.Rank, Int16Filter.Profile).Build(),
        FilterProperty.MapNullable<Person, DateTime>("LastSeen", person => person.LastSeen, DateTimeFilter.Profile).Build());

    public static List<string> Names(this IQueryable<Person> people) => [.. people.Select(person => person.Name)];
}
