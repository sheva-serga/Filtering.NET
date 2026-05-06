namespace Filtering.Net.Generator;

// Maps a CLR type's full display name to the profile classes that bind it via [FilterProfile<T>].
// Built once per generation invocation by ProfileIndexBuilder.
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

    public IReadOnlyList<string> Lookup(string clrTypeFullName) =>
        _entries.TryGetValue(clrTypeFullName, out var profiles) ? profiles : [];

    public IEnumerable<string> RegisteredTypes => _entries.Keys;
}
