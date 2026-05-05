namespace Filtering.Net.Generator;

/// <summary>Model describing a discovered filter class (a partial class marked with <c>[GenerateFilter&lt;TEntity&gt;]</c>).</summary>
/// <param name="HasAnyTypedValueProperty">True when at least one property in <see cref="Properties"/>
/// has <see cref="PropertyMappingModel.HasTypedValueOperator"/> set, or at least one override in
/// <see cref="Overrides"/> has <see cref="PropertyOverrideModel.HasTypedValueOperator"/> set. The
/// emitter uses this flag to conditionally thread <c>JsonSerializerOptions</c> through the Apply
/// and Validate chains — classes with no typed-value properties need no JSON deserialisation at
/// all and can ignore the options parameter entirely.</param>
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
