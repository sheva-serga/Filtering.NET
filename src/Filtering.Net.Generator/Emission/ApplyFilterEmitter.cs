namespace Filtering.Net.Generator;

// Per-property Build methods and typed leaf methods live in PerPropertyClassEmitter.
// This emitter owns only the field-name dispatcher that forwards into {Property}.Build(leaf).
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
