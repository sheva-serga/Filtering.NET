namespace Filtering.Net.Generator;

internal sealed record ApplySortingView(
    string EntityFullName,
    bool HasSortableFields,
    IReadOnlyList<SortDispatchGroupView> SortDispatchGroups);

internal sealed record SortDispatchGroupView(
    string PropertyIdentifier,
    IReadOnlyList<string> FieldKeys);
