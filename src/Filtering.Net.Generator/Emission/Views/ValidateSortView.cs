namespace Filtering.Net.Generator;

internal sealed record ValidateSortView(
    IReadOnlyList<SortableFieldView> SortableFields);

internal sealed record SortableFieldView(
    string PrimaryFieldKey,
    string? AliasFieldKey);
