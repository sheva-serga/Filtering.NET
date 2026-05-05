namespace Filtering.Net.Generator;

/// <summary>Maps a CLR type's full display name (e.g. <c>System.Int32</c>) to the set of
/// profile classes that bind it via <c>[FilterProfile&lt;T&gt;]</c>. Built once per source-
/// generation invocation by <see cref="ProfileIndexBuilder"/>.</summary>
internal sealed class ProfileIndex
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _entries;

    public ProfileIndex(IReadOnlyDictionary<string, List<string>> entries)
    {
        var snapshot = new Dictionary<string, IReadOnlyList<string>>(entries.Count, StringComparer.Ordinal);
        foreach (var pair in entries)
        {
            snapshot[pair.Key] = pair.Value.AsReadOnly();
        }
        _entries = snapshot;
    }

    /// <summary>Returns the registered profile full names for a CLR type, or an empty list.</summary>
    public IReadOnlyList<string> Lookup(string clrTypeFullName) =>
        _entries.TryGetValue(clrTypeFullName, out var profiles) ? profiles : [];

    /// <summary>The set of CLR type full names that have at least one registered profile.</summary>
    public IEnumerable<string> RegisteredTypes => _entries.Keys;
}
