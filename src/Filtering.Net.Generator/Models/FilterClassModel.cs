namespace Filtering.Net.Generator;

// HasAnyTypedValueProperty: the class (or a filter it nests) has an operator whose value is
// deserialized through the JSON resolver, so the emitter adds the resolver-accepting constructor.
internal sealed record FilterClassModel(
    string Namespace,
    string ClassName,
    string FullEntityTypeName,
    int MaxPageSize,
    int DefaultPageSize,
    int MaxNestingDepth,
    int MaxLeafConditions,
    EquatableList<PropertyMappingModel> Properties,
    EquatableList<InterceptorModel> Interceptors,
    EquatableList<PropertyOverrideModel> Overrides,
    LocationInfo? Location,
    bool HasAnyTypedValueProperty,
    EquatableList<NestedMappingModel> NestedMappings);
