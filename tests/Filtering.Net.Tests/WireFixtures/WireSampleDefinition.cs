namespace Filtering.Net.Tests.WireFixtures;

public sealed class WireSampleDepartment
{
    public string? Name { get; set; }
}

public sealed class WireSample
{
    public string Name { get; set; } = string.Empty;
    public string? Nickname { get; set; }
    public int Age { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid ExternalId { get; set; }
    public WireSampleDepartment? Department { get; set; }
    public List<string> Tags { get; set; } = [];
}

internal static class WireSampleDefinition
{
    public static IFilterDefinition<WireSample> Create()
    {
        var nameProfile = StringFilter.Profile.Extend(
            "StringFilterWithILike",
            FilterOperator.Value<string, string>("ilike", (column, value) => column.Contains(value), StringFilter.TryGetValue));
        FilterRule<WireSample, List<string>> tagsRule = new FilterRuleBuilder<WireSample, List<string>>()
            .For(sample => sample.Tags)
            .Operator("isEmpty", tags => tags.Count == 0);

        var schema = new FilterSchemaBuilder<WireSample>(new FilterSettings())
            .Add(FilterProperty.Map<WireSample, string>("name", sample => sample.Name, nameProfile).Sortable().Build())
            .Add(FilterProperty.Map<WireSample, string>("nickname", sample => sample.Nickname!, StringFilter.Profile).Build())
            .Add(FilterProperty.Map<WireSample, int>("age", sample => sample.Age, Int32Filter.Profile).Sortable().Build())
            .Add(FilterProperty.Map<WireSample, bool>("isActive", sample => sample.IsActive, BoolFilter.Profile).Build())
            .Add(FilterProperty.Map<WireSample, DateTimeOffset>("createdAt", sample => sample.CreatedAt, DateTimeOffsetFilter.Profile).Sortable().Build())
            .Add(FilterProperty.Map<WireSample, Guid>("externalId", sample => sample.ExternalId, GuidFilter.Profile).Build())
            .Add(FilterProperty.Map<WireSample, string>("department.name", sample => sample.Department!.Name!, StringFilter.Profile).Build())
            .Add(FilterProperty.MapRule("tags", tagsRule).Build())
            .Build();

        return new FilterDefinition<WireSample>(schema);
    }
}
