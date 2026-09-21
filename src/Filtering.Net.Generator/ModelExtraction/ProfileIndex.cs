namespace Filtering.Net.Generator;

// Maps a CLR type's full display name to the profile classes that bind it via [FilterProfile<T>].
// Built once per compilation by ProfileIndexBuilder and carried through the pipeline inside
// GeneratorIndex, so it compares by content: two runs over an unchanged compilation must be equal.
internal sealed class ProfileIndex : IEquatable<ProfileIndex>
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<string>> _entries;

    // Key-ordered snapshot of _entries; the dictionary itself has no defined ordering to compare.
    private readonly List<KeyValuePair<string, IReadOnlyList<string>>> _orderedEntries;

    public ProfileIndex(IReadOnlyDictionary<string, List<string>> entries)
    {
        var snapshot = new Dictionary<string, IReadOnlyList<string>>(entries.Count, StringComparer.Ordinal);
        foreach (var pair in entries)
        {
            snapshot[pair.Key] = pair.Value.AsReadOnly();
        }
        _entries = snapshot;
        _orderedEntries = [.. snapshot.OrderBy(pair => pair.Key, StringComparer.Ordinal)];
    }

    public IReadOnlyList<string> Lookup(string clrTypeFullName) =>
        _entries.TryGetValue(clrTypeFullName, out var profiles) ? profiles : [];

    public bool Equals(ProfileIndex? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_orderedEntries.Count != other._orderedEntries.Count) return false;
        for (var entryIndex = 0; entryIndex < _orderedEntries.Count; entryIndex++)
        {
            var left = _orderedEntries[entryIndex];
            var right = other._orderedEntries[entryIndex];
            if (!string.Equals(left.Key, right.Key, StringComparison.Ordinal)) return false;
            if (left.Value.Count != right.Value.Count) return false;
            for (var profileIndex = 0; profileIndex < left.Value.Count; profileIndex++)
            {
                if (!string.Equals(left.Value[profileIndex], right.Value[profileIndex], StringComparison.Ordinal)) return false;
            }
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is ProfileIndex other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            foreach (var pair in _orderedEntries)
            {
                hash = (hash * 31) + pair.Key.GetHashCode();
                foreach (var profileFullName in pair.Value)
                {
                    hash = (hash * 31) + profileFullName.GetHashCode();
                }
            }
            return hash;
        }
    }
}
