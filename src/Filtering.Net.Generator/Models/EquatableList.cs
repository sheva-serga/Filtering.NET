using System.Collections;

namespace Filtering.Net.Generator;

/// <summary>List wrapper with structural equality. Used in generator pipeline models so they cache correctly across incremental runs.</summary>
internal sealed class EquatableList<T> : IReadOnlyList<T>, IEquatable<EquatableList<T>>
{
    private readonly List<T> _items;

    public EquatableList() => _items = [];

    public EquatableList(IEnumerable<T> items) => _items = [.. items];

    public T this[int index] => _items[index];

    public int Count => _items.Count;

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public bool Equals(EquatableList<T>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (_items.Count != other._items.Count) return false;
        for (var index = 0; index < _items.Count; index++)
        {
            if (!EqualityComparer<T>.Default.Equals(_items[index], other._items[index]))
            {
                return false;
            }
        }
        return true;
    }

    public override bool Equals(object? obj) => obj is EquatableList<T> other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            foreach (var item in _items)
            {
                hash = (hash * 31) + (item?.GetHashCode() ?? 0);
            }
            return hash;
        }
    }
}
