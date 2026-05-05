namespace Filtering.Net.Generator;

/// <summary>Model describing a single property mapping discovered on a filter class.</summary>
/// <param name="ConfigurationMethodName">Name of the user-declared <c>[Map]</c>-decorated partial method.
/// The generator emits the no-op implementation part for this method so the consumer compilation succeeds.</param>
/// <param name="CustomOperators">Pre-extracted lambda metadata for any custom operators (operators
/// declared by the resolved profile chain that are not part of <see cref="BuiltInProfileCatalog"/>).
/// Empty for built-in profiles.</param>
/// <param name="ConverterModelClrType">When <see cref="ValueConverterFullName"/> is set, the
/// <c>TModel</c> type argument from the converter's <c>ValueConverter&lt;TModel, TProvider&gt;</c>
/// base — this is the CLR type of the value the user works with at the API boundary, and the
/// type the emitter uses for typed leaf-method value parameters and JSON deserialization.
/// Stored as a bare display string (no <c>global::</c> prefix) for symmetry with
/// <see cref="PropertyClrType"/>; <c>PropertyValueShapeResolver</c> does its own qualification.
/// Null when no converter is present or the converter's TModel could not be resolved.</param>
/// <param name="ExtractorProfileFullName">Full name of the profile class whose
/// <c>TryGetValue</c>/<c>TryGetArray</c> helpers the generator calls to lift JSON values into
/// CLR values. Equals <see cref="ProfileFullName"/> for built-in profiles and auto-emitted
/// per-enum profiles; for user-defined custom profiles it walks the
/// <c>[FilterProfile(BasedOn = ...)]</c> chain to a profile that owns the helpers (a built-in
/// or an auto-emitted enum profile). Diagnostics like FN1003 still key off
/// <see cref="ProfileFullName"/> — the extractor name is purely an emission concern.</param>
/// <param name="HasTypedValueOperator">True when at least one of this property's custom operators
/// (declared via <c>[FilterOperator]</c> on the resolved profile) has a non-null
/// <see cref="CustomOperatorModel.ValueClrType"/>. Such operators require the AOT-safe
/// typed-value deserialisation path (<c>JsonSerializer.Deserialize&lt;T&gt;</c>) rather than the
/// <c>TryGetValue</c>/<c>TryGetArray</c> profile-helper extraction. Built-in profile operators
/// and unary custom operators always use the JsonElement-based extractors and leave this flag
/// false.</param>
internal sealed record PropertyMappingModel(
    string PropertyName,
    string PropertyClrType,
    string ProfileFullName,
    string ExtractorProfileFullName,
    EquatableList<string> AllowedOperators,
    string? Alias,
    bool Sortable,
    string DefaultSortDirection,
    string? ValueConverterFullName,
    string? ConverterModelClrType,
    string ConfigurationMethodName,
    EquatableList<CustomOperatorModel> CustomOperators,
    bool HasTypedValueOperator);
