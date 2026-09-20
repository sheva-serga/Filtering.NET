namespace Filtering.Net.Generator;

// SchemaEntries are complete ".Add(...)" / ".AddRange(...)" calls, already formatted in C#.
internal sealed record FilterClassView(
    string Namespace,
    bool HasNamespace,
    string ClassName,
    string EntityFullName,
    int DefaultPageSize,
    int MaxPageSize,
    int MaxNestingDepth,
    int MaxLeafConditions,
    bool ThreadsSerializerOptions,
    IReadOnlyList<string> SchemaEntries);
