namespace Filtering.Net.Generator;

/// <summary>
/// Emits <c>Validate(IReadOnlyList&lt;SortItem&gt;?)</c>. Iterates the request's sort items and
/// emits a per-sortable-field switch; unknown fields surface as <see cref="FilterValidationCode.NotSortable"/>.
/// </summary>
internal static class ValidateSortEmitter
{
    public static string Emit(FilterClassModel model) =>
        ScribanRuntime.Render("ValidateSort", BuildView(model));

    internal static ValidateSortView BuildView(FilterClassModel model)
    {
        var fields = new List<SortableFieldView>();
        foreach (var property in model.Properties.Where(p => p.Sortable))
        {
            var primary = EmissionNames.UpperFieldKey(property.PropertyName);
            string? alias = null;
            if (!string.IsNullOrEmpty(property.Alias)
                && !string.Equals(property.Alias, property.PropertyName, StringComparison.OrdinalIgnoreCase))
            {
                alias = EmissionNames.UpperFieldKey(property.Alias!);
            }
            fields.Add(new SortableFieldView(primary, alias));
        }
        return new ValidateSortView(fields);
    }
}
