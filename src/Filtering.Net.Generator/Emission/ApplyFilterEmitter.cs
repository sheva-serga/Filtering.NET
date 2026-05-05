namespace Filtering.Net.Generator;

/// <summary>
/// Emitter for ApplyFilter. Walks the FilterNode tree, builds typed per-(property, operator)
/// leaf predicates, and composes them with PredicateBuilder.
/// </summary>
/// <remarks>
/// As of the per-property nested-class refactor, the per-property <c>Build</c> methods and
/// per-operator typed leaf methods are emitted by <see cref="PerPropertyClassEmitter"/> rather
/// than directly by this emitter. This emitter only owns the public API surface and the
/// field-name dispatcher (<c>BuildLeaf</c>) that forwards into <c>{Property}.Build(leaf)</c> (or <c>Build(leaf, options)</c> when the property has typed-value operators).
/// </remarks>
internal static class ApplyFilterEmitter
{
    public static string Emit(FilterClassModel model) =>
        ScribanRuntime.Render("ApplyFilter", BuildView(model));

    internal static ApplyFilterView BuildView(FilterClassModel model)
    {
        var entityFullName = "global::" + model.FullEntityTypeName;
        var threadsSerializerOptions = model.HasAnyTypedValueProperty;
        var arms = new List<DispatchArmView>();

        foreach (var property in model.Properties)
        {
            arms.Add(new DispatchArmView(
                PropertyIdentifier: EmissionNames.PropertyIdentifier(property.PropertyName),
                PrimaryFieldKey: EmissionNames.UpperFieldKey(property.PropertyName),
                AliasFieldKey: ResolveAlias(property.PropertyName, property.Alias),
                ArmThreadsOptions: threadsSerializerOptions && property.HasTypedValueOperator));
        }

        foreach (var overrideModel in model.Overrides)
        {
            if (model.Properties.Any(p => p.PropertyName == overrideModel.PropertyName)) continue;
            arms.Add(new DispatchArmView(
                PropertyIdentifier: EmissionNames.PropertyIdentifier(overrideModel.PropertyName),
                PrimaryFieldKey: EmissionNames.UpperFieldKey(overrideModel.PropertyName),
                AliasFieldKey: null,
                ArmThreadsOptions: threadsSerializerOptions && overrideModel.HasTypedValueOperator));
        }

        return new ApplyFilterView(entityFullName, threadsSerializerOptions, arms);
    }

    private static string? ResolveAlias(string propertyName, string? alias)
    {
        if (string.IsNullOrEmpty(alias)) return null;
        if (string.Equals(alias, propertyName, StringComparison.OrdinalIgnoreCase)) return null;
        return EmissionNames.UpperFieldKey(alias!);
    }
}
