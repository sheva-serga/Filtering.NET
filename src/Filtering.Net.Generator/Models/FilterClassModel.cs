namespace Filtering.Net.Generator;

// HasAnyTypedValueProperty: emitter uses this to decide whether to thread JsonSerializerOptions
// through Apply/Validate — classes with no typed-value properties can skip JSON deserialisation.
internal sealed record FilterClassModel(
    string Namespace,
    string ClassName,
    string FullEntityTypeName,
    int MaxPageSize,
    int DefaultPageSize,
    EquatableList<PropertyMappingModel> Properties,
    EquatableList<InterceptorModel> Interceptors,
    EquatableList<PropertyOverrideModel> Overrides,
    LocationInfo? Location,
    bool HasAnyTypedValueProperty);
