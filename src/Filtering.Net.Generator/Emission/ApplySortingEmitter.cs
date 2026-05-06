namespace Filtering.Net.Generator;

internal static class ApplySortingEmitter
{
    public static string Emit(FilterClassModel model) =>
        ScribanRuntime.Render("ApplySorting", BuildView(model));

    internal static ApplySortingView BuildView(FilterClassModel model)
    {
        var entityFullName = "global::" + model.FullEntityTypeName;
        var groups = new List<SortDispatchGroupView>();

        foreach (var property in model.Properties.Where(p => p.Sortable))
        {
            var propertyIdentifier = EmissionNames.PropertyIdentifier(property.PropertyName);
            var fieldKeys = new List<string> { EmissionNames.UpperFieldKey(property.PropertyName) };
            if (!string.IsNullOrEmpty(property.Alias)
                && !string.Equals(property.Alias, property.PropertyName, StringComparison.OrdinalIgnoreCase))
            {
                fieldKeys.Add(EmissionNames.UpperFieldKey(property.Alias!));
            }
            groups.Add(new SortDispatchGroupView(propertyIdentifier, fieldKeys));
        }

        return new ApplySortingView(entityFullName, groups.Count > 0, groups);
    }
}
