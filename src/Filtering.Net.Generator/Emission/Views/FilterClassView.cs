namespace Filtering.Net.Generator;

internal sealed record FilterClassView(
    string Namespace,
    bool HasNamespace,
    string ClassName,
    string EntityFullName,
    int DefaultPageSize,
    int MaxPageSize,
    bool ThreadsSerializerOptions,
    IReadOnlyList<string> ConfigurationMethodNames,
    string ValidateNodeBody,
    string ValidateSortBody,
    string ValidatePageBody,
    string ApplyFilterBody,
    string ApplySortingBody,
    IReadOnlyList<string> PerPropertyClassBodies);
