namespace Filtering.Net.Generator;

/// <summary>
/// Emits <c>Validate(FilterNode?)</c> and its supporting <c>ValidateNode</c>/<c>ValidateLeaf</c>
/// dispatchers. The per-property leaf validators live on the per-property nested classes
/// emitted by <see cref="PerPropertyClassEmitter"/>; this emitter just routes into them.
/// </summary>
internal static class ValidateNodeEmitter
{
    public static string Emit(FilterClassModel model) =>
        ScribanRuntime.Render("ValidateNode", BuildView(model));

    internal static ValidateNodeView BuildView(FilterClassModel model)
    {
        var arms = new List<ValidateLeafArmView>();
        foreach (var property in model.Properties)
        {
            arms.Add(new ValidateLeafArmView(
                PropertyIdentifier: EmissionNames.PropertyIdentifier(property.PropertyName),
                PrimaryFieldKey: EmissionNames.UpperFieldKey(property.PropertyName),
                AliasFieldKey: ResolveAlias(property.PropertyName, property.Alias),
                ArmThreadsOptions: property.HasTypedValueOperator));
        }
        foreach (var overrideModel in model.Overrides)
        {
            if (model.Properties.Any(p => p.PropertyName == overrideModel.PropertyName)) continue;
            arms.Add(new ValidateLeafArmView(
                PropertyIdentifier: EmissionNames.PropertyIdentifier(overrideModel.PropertyName),
                PrimaryFieldKey: EmissionNames.UpperFieldKey(overrideModel.PropertyName),
                AliasFieldKey: null,
                ArmThreadsOptions: overrideModel.HasTypedValueOperator));
        }
        var threadsSerializerOptions = model.HasAnyTypedValueProperty;
        return new ValidateNodeView(threadsSerializerOptions, arms);
    }

    private static string? ResolveAlias(string propertyName, string? alias)
    {
        if (string.IsNullOrEmpty(alias)) return null;
        if (string.Equals(alias, propertyName, StringComparison.OrdinalIgnoreCase)) return null;
        return EmissionNames.UpperFieldKey(alias!);
    }
}
